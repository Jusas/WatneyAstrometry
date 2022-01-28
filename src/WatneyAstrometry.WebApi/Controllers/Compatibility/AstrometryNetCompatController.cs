// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WatneyAstrometry.Core.Types;
using WatneyAstrometry.WebApi.Controllers.Compatibility.Models;
using WatneyAstrometry.WebApi.Controllers.Watney;
using WatneyAstrometry.WebApi.Models.Domain;
using WatneyAstrometry.WebApi.Services;
using WcsFitsWriter = WatneyAstrometry.Core.Fits.WcsFitsWriter;

namespace WatneyAstrometry.WebApi.Controllers.Compatibility
{
    /// <summary>
    /// Astrometry.net compatibility API.
    /// Allows the use of Watney through the documented nova.astrometry.net interface, see: [http://astrometry.net/doc/net/api.html](http://astrometry.net/doc/net/api.html).
    /// Note that some options will be ignored when they do not fit the Watney option model.
    /// The main 'canonical' Watney API is located under /api/watney.
    /// </summary>
    [Route("api")]
    [ApiController]
    [ApiVersionNeutral]
    [AllowAnonymous]
    public class AstrometryNetCompatController : ControllerBase
    {
        private readonly ILogger<JobsController> _logger;
        private readonly IJobManager _jobManager;
        private readonly WatneyApiConfiguration _apiConfig;
        private readonly IHttpClientFactory _httpClientFactory;

        public AstrometryNetCompatController(ILogger<JobsController> logger, IJobManager jobManager,
            WatneyApiConfiguration apiConfig, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _jobManager = jobManager;
            _apiConfig = apiConfig;
            _httpClientFactory = httpClientFactory;
        }
        

        private (bool authenticated, string user) IsAuthenticated(string apiKey)
        {
            if ("apikey".Equals(_apiConfig.Authentication, StringComparison.InvariantCultureIgnoreCase))
            {
                if (!string.IsNullOrEmpty(apiKey) && _apiConfig.ApiKeys.Values.Contains(apiKey))
                {
                    var user = _apiConfig.ApiKeys.First(x => x.Value == apiKey).Key;
                    return (true, user);
                }
                    
                return (false, null);
            }

            return (true, "");
        }

        /// <summary>
        /// Log in. If authentication is enabled, uses the API key. If authentication is disabled, any api key value will be accepted.
        /// See: http://astrometry.net/doc/net/api.html#session-key
        /// </summary>
        /// <param name="login">
        /// <para>
        /// <code>
        /// request-json={"apikey": "XXXXXX"}
        /// </code>
        /// </para>
        /// <para>
        /// In Swagger, just enter the JSON <code>{"apikey": "XXXXXX"}</code>
        /// </para>
        /// See: http://astrometry.net/doc/net/api.html#session-key
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [Route("login")]
        public async Task<IActionResult> Login([FromForm(Name = "request-json")] string login)
        {
            // Imitates the Astrometry.net login, but is stateless (session == apikey)
            // http://astrometry.net/doc/net/api.html#session-key

            if (string.IsNullOrEmpty(login))
            {
                return Ok(new
                {
                    status = "error",
                    errormessage = "bad apikey"
                });
            }

            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(login);
                var (isAuthenticated, user) = IsAuthenticated(data["apikey"]);
                if (!isAuthenticated)
                {
                    return Ok(new
                    {
                        status = "error",
                        errormessage = "bad apikey"
                    });
                }

                return Ok(new
                {
                    status = "success",
                    message = $"authenticated user: {user}",
                    session = data["apikey"]
                });

            }
            catch (Exception)
            {
                return Ok(new
                {
                    status = "error",
                    errormessage = "bad apikey"
                });
            }
            
            
        }


        private NearbyOptions ConstructNearbyOptions(UploadModel data)
        {
            var options = new NearbyOptions
            {
                Ra = data.CenterRa,
                Dec = data.CenterDec,
                UseFitsHeaders = false,
                SearchRadius = data.SearchRadius
            };

            if (data.FieldRadius.Length == 2)
            {
                options.MaxFieldRadius = data.FieldRadius[0];
                options.MinFieldRadius = data.FieldRadius[1];
                options.IntermediateFieldRadiusSteps = data.IntermediateFieldRadiusSteps;
            }
            else
            {
                options.MaxFieldRadius = data.FieldRadius[0];
                options.MinFieldRadius = data.FieldRadius[0];
            }

            return options;

        }

        /// <summary>
        /// Upload an image file (jpg, png, fits).
        /// See: http://astrometry.net/doc/net/api.html#submitting-a-file
        /// </summary>
        /// <param name="upload"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> FileUpload([FromForm(Name = "request-json")] string upload, IFormFile file)
        {

            FileUploadModel data;
            try
            {
                _logger.LogTrace("Pre-validation input data: " + upload);
                data = JsonConvert.DeserializeObject<FileUploadModel>(upload);
            }
            catch (Exception)
            {
                // Seems to return 500 with invalid payload, so mimic that.
                return new ObjectResult("Server Error (500)")
                {
                    StatusCode = 500
                };
            }

            var (isAuthenticated, user) = IsAuthenticated(data.Session);
            if (!isAuthenticated)
            {
                return Ok(new
                {
                    status = "error",
                    errormessage = $"no session with key {data.Session}"
                });
            }

            if (!data.ValidateModel())
            {
                // Is this the expected return code if we get some invalid data?
                return new ObjectResult("Server Error (500)")
                {
                    StatusCode = 500
                };
            }

            _logger.LogTrace("Post-validation input data: " + JsonConvert.SerializeObject(data));

            try
            {

                // Use blind solve if we don't get coordinates and field radius.
                var mode = "blind";
                if (data.CenterDec != null && data.CenterRa != null && data.ScaleType != null) 
                {
                    mode = "nearby";
                }

                var jobModel = new NewJobInputModel()
                {
                    Image = file,
                    Parameters = new JobParametersModel
                    {
                        Mode = mode,
                        NearbyParameters = mode == "nearby" ? ConstructNearbyOptions(data) : null,
                        BlindParameters = mode == "blind" ? new BlindOptions
                        {
                            MaxRadius = 16, // These are perhaps decent defaults.
                            MinRadius = 0.25
                        } : null,
                        HigherDensityOffset = 1,
                        LowerDensityOffset = 1,
                        Sampling = data.SamplingFactor != null
                            ? (int)data.SamplingFactor.Value
                            : null
                    }
                };

                var createdJob = await _jobManager.PrepareJob(jobModel, data.Metadata);

                return Ok(new
                {
                    status = "success",
                    // A lazy way, but since Astrometry.net uses a number as the ID this should be sufficient.
                    // I'm going to assume returning an int is valid and probably the safest bet that won't
                    // break other software.
                    subid = createdJob.NumericId,
                    hash = string.Join("",
                        SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(file.FileName))
                            .Select(b => $"{b:X}".ToLowerInvariant()))
                });

            }
            catch (Exception e)
            {
                return new ObjectResult("Server Error (500)")
                {
                    StatusCode = 500
                };
            }
            
        }



        /// <summary>
        /// Upload using a URL to an image.
        /// </summary>
        /// <param name="upload"></param>
        /// <returns></returns>
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        [Route("url_upload")]
        public async Task<IActionResult> UrlUpload([FromForm(Name = "request-json")] string upload)
        {

            // URL uploads are always nearby solves.
            // Assume that we get all the necessary information in our parameters.

            UrlUploadModel data;
            try
            {
                data = JsonConvert.DeserializeObject<UrlUploadModel>(upload);
            }
            catch (Exception)
            {
                // Seems to return 500 with invalid payload, so mimic that.
                return new ObjectResult("Server Error (500)")
                {
                    StatusCode = 500
                };
            }
        
            var (isAuthenticated, user) = IsAuthenticated(data.Session);
            if (!isAuthenticated)
            {
                return Ok(new
                {
                    status = "error",
                    errormessage = $"no session with key {data.Session}"
                });
            }

            if (!data.ValidateModel())
            {
                // Is this the expected return code if we get some invalid data?
                return new ObjectResult("Server Error (500)")
                {
                    StatusCode = 500
                };
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(data.Url).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new ObjectResult("Server Error (500)")
                    {
                        StatusCode = 500
                    };
                }

                // Astrometry.net appears to accept any URL with any content, which is somewhat dangerous.
                // If we were to save this to disk we should validate that our data is a valid image
                // (jpg, png or fits) before we use it for anything.
                // Since we try to parse the image here and run star detection and never save the source
                // material to disk we should be ok.
                var webData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var formFile = new HttpFile(webData)
                {
                    FileName = data.Url
                };

                // Use blind solve if we don't get coordinates and field radius.
                var mode = "blind";
                if (data.CenterDec != null && data.CenterRa != null && data.ScaleType != null)
                {
                    mode = "nearby";
                }

                var jobModel = new NewJobInputModel()
                {
                    Image = formFile,
                    Parameters = new JobParametersModel
                    {
                        Mode = mode,
                        NearbyParameters = mode == "nearby" ? ConstructNearbyOptions(data) : null,
                        BlindParameters = mode == "blind" ? new BlindOptions
                        {
                            MaxRadius = 16, // These are perhaps decent defaults.
                            MinRadius = 0.25
                        } : null,
                        HigherDensityOffset = 1,
                        LowerDensityOffset = 1,
                        Sampling = data.SamplingFactor != null 
                            ? (int)data.SamplingFactor.Value 
                            : null
                    }
                };

                var createdJob = await _jobManager.PrepareJob(jobModel, data.Metadata);

                return Ok(new
                {
                    status = "success",
                    // A lazy way, but since Astrometry.net uses a number as the ID this should be sufficient.
                    // I'm going to assume returning an int is valid and probably the safest bet that won't
                    // break other software.
                    subid = createdJob.NumericId,
                    hash = string.Join("",
                        SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(data.Url))
                            .Select(b => $"{b:X}".ToLowerInvariant()))
                });

            }
            catch (Exception e)
            {
                return new ObjectResult("Server Error (500)")
                {
                    StatusCode = 500
                };
            }
            
        }


        /// <summary>
        /// Get the submission information.
        /// See: http://astrometry.net/doc/net/api.html#getting-submission-status
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("submissions/{id}")]
        public async Task<IActionResult> GetSubmission([FromRoute]int id)
        {
            // No authentication required, as far as I can understand.
            // http://astrometry.net/doc/net/api.html#getting-submission-status

            var job = await _jobManager.GetJob(id);

            if (job == null)
                return NotFound("Not found");

            var started = job.Status == JobStatus.Queued 
                ? "None" 
                : job.SolveStarted?.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var finished = job.Status <= JobStatus.Solving 
                ? "None"
                : job.Updated.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

            var jobs = new List<int>();
            if (job.Status >= JobStatus.Solving)
            {
                jobs.Add(job.NumericId);
            }

            var calibrations = new List<int>();
            if (job.Status > JobStatus.Solving)
            {
                calibrations.Add(job.NumericId);
            }

            return Ok(new
            {
                jobs = jobs,
                job_calibrations = calibrations,
                processing_started = started,
                processing_finished = finished,
                user = 0,
                user_images = Array.Empty<string>()
            });

        }

        /// <summary>
        /// Get the submission information.
        /// Method added to comply with Nova also supporting POST.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("submissions/{id}")]
        public async Task<IActionResult> GetSubmissionViaPost([FromRoute] int id)
            => await GetSubmission(id);


        /// <summary>
        /// Get job information.
        /// See: http://astrometry.net/doc/net/api.html#getting-job-status
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("jobs/{id}")]
        public async Task<IActionResult> GetJob([FromRoute] int id)
        {
            // No authentication required, as far as I can understand.
            // http://astrometry.net/doc/net/api.html#getting-job-status

            var job = await _jobManager.GetJob(id);

            if (job == null)
                return NotFound("Not found");
            
            return Ok(new
            {
                status = ResolveStatusString(job.Status)
            });
            
        }

        private string ResolveStatusString(JobStatus status)
        {
            var statusString = "none";
            if (status == JobStatus.Solving)
                statusString = "solving";
            else if (status == JobStatus.Queued)
                statusString = "none";
            else if (status == JobStatus.Success)
                statusString = "success";
            else
                statusString = "failure";
            return statusString;
        }

        /// <summary>
        /// Get job information.
        /// Method added to comply with Nova also supporting POST.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("jobs/{id}")]
        public async Task<IActionResult> GetJobViaPost([FromRoute] int id)
            => await GetJob(id);


        /// <summary>
        /// Get the calibration (result).
        /// See: http://astrometry.net/doc/net/api.html#getting-job-results-calibration
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("jobs/{id}/calibration")]
        public async Task<IActionResult> GetCalibration([FromRoute] int id)
        {
            // No authentication required, as far as I can understand.
            // http://astrometry.net/doc/net/api.html#getting-job-results-calibration

            var job = await _jobManager.GetJob(id);

            if (job == null)
                return NotFound("Not found");

            if(job.Status != JobStatus.Success)
                return NotFound("Calibration data not available");

            return Ok(new
            {
                orientation = job.Solution.Orientation,
                radius = job.Solution.FieldRadius,
                ra = job.Solution.Ra,
                dec = job.Solution.Dec,
                parity = "normal".Equals(job.Solution.Parity, StringComparison.InvariantCultureIgnoreCase)
                    ? -1 
                    : 1,
                width_arcsec = job.Solution.PixScale * job.ImageWidth,
                height_arcsec = job.Solution.PixScale * job.ImageHeight,
                pixscale = job.Solution.PixScale
            });
        }

        /// <summary>
        /// Get the calibration (result).
        /// Method added to comply with Nova also supporting POST.
        /// </summary>
        [HttpPost]
        [Route("jobs/{id}/calibration")]
        public async Task<IActionResult> GetCalibrationViaPost([FromRoute] int id)
            => await GetCalibration(id);


        /// <summary>
        /// Get job information.
        /// See: http://astrometry.net/doc/net/api.html#getting-job-results
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("jobs/{id}/info")]
        public async Task<IActionResult> GetJobInfo([FromRoute] int id)
        {
            // No authentication required, as far as I can understand.
            // http://astrometry.net/doc/net/api.html#getting-job-results

            var job = await _jobManager.GetJob(id);

            if (job == null)
                return NotFound("Not found");
            
            return Ok(new
            {
                objects_in_field = new object[0],
                tags = new string[0],
                machine_tags = new string[0],
                status = ResolveStatusString(job.Status),
                original_filename = job.OriginalFilename,
                calibration = job.Solution != null ? new 
                {
                    // This may seem off; https://github.com/dstndstn/astrometry.net/issues/151
                    // Not sure if I should offset this be 180 degrees or not.
                    orientation = job.Solution.Orientation,
                    radius = job.Solution.FieldRadius,
                    ra = job.Solution.Ra,
                    dec = job.Solution.Dec,
                    // Hoping this is correct - https://github.com/dstndstn/astrometry.net/issues/168
                    parity = "normal".Equals(job.Solution.Parity, StringComparison.InvariantCultureIgnoreCase)
                        ? -1 
                        : 1,
                    width_arcsec = job.Solution.PixScale * job.ImageWidth,
                    height_arcsec = job.Solution.PixScale * job.ImageHeight,
                } : null
            });
        }


        /// <summary>
        /// Get job information.
        /// Method added to comply with Nova also supporting POST.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("jobs/{id}/info")]
        public async Task<IActionResult> GetJobInfoViaPost([FromRoute] int id)
            => await GetJobInfo(id);

        /// <summary>
        /// Returns the WCS file if the job was successful.
        /// See: http://astrometry.net/doc/net/api.html#getting-job-results-results-files
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/wcs_file/{id}")]
        public async Task<IActionResult> GetWcsFile([FromRoute] int id)
        {
            var job = await _jobManager.GetJob(id);

            if (job == null)
                return NotFound("Not found");

            if(job.Solution == null || job.Solution.FitsWcs == null)
                return NotFound("Not found");

            var stream = new MemoryStream();
            var wcsWriter = new WcsFitsWriter(stream);
            wcsWriter.WriteWcsFile(ToCoreSolution(job), job.ImageWidth, job.ImageHeight);
            
            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, "application/fits", "wcs.fits");
        }

        private Solution.FitsHeaderFields ToCoreSolution(JobModel job)
        {
            var w = job.Solution.FitsWcs;
            return new Solution.FitsHeaderFields(w.Cdelt1, w.Cdelt2, w.Crota1, w.Crota2, w.Cd1_1, w.Cd2_1, w.Cd1_2,
                w.Cd2_2, w.Crval1, w.Crval2, w.Crpix1, w.Crpix2);
        }

        /// <summary>
        /// Returns the WCS file if the job was successful.
        /// Method added to comply with Nova also supporting POST.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("/wcs_file/{id}")]
        public async Task<IActionResult> GetWcsFileViaPost([FromRoute] int id)
            => await GetWcsFile(id);



        /// <summary>
        /// Objects in field; always returns an empty list.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("jobs/{id}/objects_in_field")]
        public async Task<IActionResult> GetObjectsInField([FromRoute] int id)
        {
            // Not supported, just return an empty list every time.
            return Ok(new
            {
                objects_in_field = Array.Empty<string>()
            });
        }

        /// <summary>
        /// Objects in field; always returns an empty list.
        /// Method added to comply with Nova also supporting POST.
        /// </summary>
        [HttpPost]
        [Route("jobs/{id}/objects_in_field")]
        public async Task<IActionResult> GetObjectsInFieldViaPost([FromRoute] int id)
            => await GetObjectsInField(id);


        /// <summary>
        /// Annotations; always returns an empty list.
        /// </summary>
        [HttpGet]
        [Route("jobs/{id}/annotations")]
        public async Task<IActionResult> GetAnnotationsInField([FromRoute] int id)
        {
            // Not supported, just return an empty list every time.
            return Ok(new
            {
                annotations = Array.Empty<string>()
            });
        }

        /// <summary>
        /// Annotations; always returns an empty list.
        /// Method added to comply with Nova also supporting POST.
        /// </summary>
        [HttpPost]
        [Route("jobs/{id}/annotations")]
        public async Task<IActionResult> GetAnnotationsInFieldViaPost([FromRoute] int id)
            => await GetAnnotationsInField(id);


        /// <summary>
        /// Tags; always returns an empty list.
        /// </summary>
        [HttpGet]
        [Route("jobs/{id}/tags")]
        public async Task<IActionResult> GetTagsInField([FromRoute] int id)
        {
            // Not supported, just return an empty list every time.
            return Ok(new
            {
                tags = Array.Empty<string>()
            });
        }

        /// <summary>
        /// Tags; always returns an empty list.
        /// Method added to comply with Nova also supporting POST.
        /// </summary>
        [HttpPost]
        [Route("jobs/{id}/tags")]
        public async Task<IActionResult> GetTagsInFieldViaPost([FromRoute] int id)
            => await GetTagsInField(id);


        /// <summary>
        /// Job log; not available, returns a "no log".
        /// </summary>
        [HttpGet]
        [Route("/joblog/{id}")]
        public async Task<IActionResult> GetLogs([FromRoute] int id)
        {
            // Not supported, just return an empty list every time.
            return Ok("no log");
        }

        /// <summary>
        /// Job log; not available, returns a "no log".
        /// Method added to comply with Nova also supporting POST.
        /// </summary>
        [HttpPost]
        [Route("/joblog/{id}")]
        public async Task<IActionResult> GetLogsViaPost([FromRoute] int id)
            => await GetLogs(id);
        
    }
}
