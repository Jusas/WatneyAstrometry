// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WatneyAstrometry.WebApi.Models.Domain;
using WatneyAstrometry.WebApi.Models.Rest;
using WatneyAstrometry.WebApi.Services;


// Notes on versioning:
// When introducing the next API version (differences to API, new endpoints, etc)
// add [ApiVersion("2")] to the controller, and then either:
//
// - Methods that remain the same in both versions remain without [MapToApiVersion] attribute.
// - New methods pertaining to new version get explicit [MapToApiVersion("2")] attribution.
// - Changed v1 -> v2 methods will have the old method attributed with [MapToApiVersion("1")]
//   and new version of the method with [MapToApiVersion("2")]

namespace WatneyAstrometry.WebApi.Controllers.Watney
{
    /// <summary>
    /// Watney canonical API, Jobs controller. Methods for querying and submitting jobs.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1")]
    [Route("api/watney/v{version:apiVersion}/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly ILogger<JobsController> _logger;

        private readonly IJobManager _jobManager;
        private readonly IMapper _mapper;


        public JobsController(ILogger<JobsController> logger, IJobManager jobManager, IMapper mapper)
        {
            _logger = logger;
            _jobManager = jobManager;
            _mapper = mapper;
        }


        /// <summary>
        /// Submit a new solver job.
        /// </summary>
        /// <param name="newJobModel">The new job parameters</param>
        /// <param name="image">The image</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(statusCode: 400, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: 201, type: typeof(RestJobModel))]
        public async Task<IActionResult> Post([FromForm] RestPostNewJobModel newJobModel, IFormFile image)
        {

            _logger.LogTrace("Receiving a new job");
            newJobModel.Image = image;

            var customValidationErrors = newJobModel?.Validate() ?? new Dictionary<string, string[]>();

            if (customValidationErrors.Any())
            {
                _logger.LogTrace("Job input validation failed: " + JsonConvert.SerializeObject(customValidationErrors));
                return ValidationProblem(new ValidationProblemDetails(customValidationErrors));
            }

            var createdJob = await _jobManager.PrepareJob(new NewJobInputModel
            {
                Image = image,
                Parameters = _mapper.Map<JobParametersModel>(newJobModel.Parameters)
            });
            return Ok(_mapper.Map<RestJobModel>(createdJob));

            // https://stackoverflow.com/questions/51614373/multipart-form-data-images-upload-with-json-asp-net-core-api
            
        }
        
        /// <summary>
        /// Get full currently available job data.
        /// </summary>
        /// <param name="id">The job ID</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(statusCode: 200, type: typeof(RestJobModel))]
        [ProducesResponseType(statusCode: 404, type: typeof(ApiNotFoundResponse))]
        public async Task<IActionResult> Get(string id)
        {
            var job = await _jobManager.GetJob(id);
            if (job == null)
                return NotFound(new ApiNotFoundResponse
                {
                    Message = $"Job {id} was not found"
                });
            return Ok(_mapper.Map<RestJobModel>(job));
        }

        /// <summary>
        /// Get the current status of a job.
        /// </summary>
        /// <param name="id">The job ID</param>
        /// <returns></returns>
        [HttpGet("{id}/status")]
        [ProducesResponseType(statusCode: 200, type: typeof(ApiStatusModelResponse))]
        [ProducesResponseType(statusCode: 404, type: typeof(ApiNotFoundResponse))]
        public async Task<IActionResult> GetStatus(string id)
        {
            var job = await _jobManager.GetJob(id);
            if (job == null)
                return NotFound(new ApiNotFoundResponse
                {
                    Message = $"Job {id} was not found"
                });
            return Ok(new ApiStatusModelResponse
            {
                Status = job.Status.ToString()
            });
        }

        /// <summary>
        /// Cancels a job if it has been queued or is currently running.
        /// </summary>
        /// <param name="id">The job ID</param>
        /// <returns></returns>
        [ProducesResponseType(statusCode: 200, type: typeof(CancelJobResponse))]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(string id)
        {
            // TODO response code filter to always produce ApiInternalErrorResponse on 500
            await _jobManager.CancelJob(id);
            return Ok(new CancelJobResponse
            {
                Message = $"Job {id} was signaled cancellation if it was queued or running"
            });
        }
        

    }
}