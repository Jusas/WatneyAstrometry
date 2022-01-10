// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Concurrent;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.Core.Image;
using WatneyAstrometry.Core.StarDetection;
using WatneyAstrometry.ImageReaders;
using WatneyAstrometry.WebApi.Exceptions;
using WatneyAstrometry.WebApi.Models;
using WatneyAstrometry.WebApi.Repositories;
using WatneyAstrometry.WebApi.Utils;

namespace WatneyAstrometry.WebApi.Services;

public class JobManager : IJobManager
{
    private readonly IJobRepository _jobRepository;
    private readonly IQueueManager _queueManager;
    private readonly ILogger<JobManager> _logger;

    public JobManager(IJobRepository jobRepository, IQueueManager queueManager, ILogger<JobManager> logger)
    {
        _jobRepository = jobRepository;
        _queueManager = queueManager;
        _logger = logger;
    }

    public async Task<JobModel> PrepareJob(JobFormUnifiedModel jobFormModel, IDictionary<string, object> metadata = null)
    {
        await Task.Yield();

        var jobGeneratedId = Guid.NewGuid().Shortened();
        _logger.LogTrace($"Preparing job {jobGeneratedId}");

        var jobModel = new JobModel
        {
            Id = jobGeneratedId,
            NumericId = jobGeneratedId.GetHashCode(),
            Status = JobStatus.Queued,
            Parameters = jobFormModel.Parameters
        };
        AnalyzeAndExtractStars(jobFormModel.Image, jobModel, metadata);
        
        await _jobRepository.Insert(jobModel).ConfigureAwait(false);
        _queueManager.Enqueue(jobModel.Id);
        return jobModel;
    }
    
    public async Task<JobModel> GetJob(string id)
    {
        return await _jobRepository.Get(id);
    }

    public async Task<JobModel> GetJob(int numericId)
    {
        return await _jobRepository.Get(numericId);
    }

    public async Task CancelJob(string id)
    {
        _logger.LogTrace($"Job cancellation signal received for job {id}");
        _queueManager.Cancel(id);
    }


    private void AnalyzeAndExtractStars(IFormFile file, JobModel model, IDictionary<string, object> metadata)
    {
        try
        {
            _logger.LogTrace($"Job {model.Id}: analyzing and extracting stars");

            IImage image;
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            model.OriginalFilename = file.FileName;

            using var stream = file.OpenReadStream();

            if (fileExtension is ".fit" or ".fits")
            {
                _logger.LogTrace("File appears to be a FITS file");
                try
                {
                    var fitsReader = new DefaultFitsReader();
                    image = fitsReader.FromStream(stream);
                }
                catch (Exception e)
                {
                    throw new FileFormatException($"Could not use FITS file: {e.Message}", e);
                }

                if (model.Parameters.NearbyParameters?.UseFitsHeaders ?? false)
                {
                    try
                    {
                        _logger.LogTrace("Using coordinates and field radius from the FITS headers");
                        var fits = (FitsImage)image;
                        model.Parameters.NearbyParameters.Ra = fits.Metadata.CenterPos.Ra;
                        model.Parameters.NearbyParameters.Dec = fits.Metadata.CenterPos.Dec;
                        model.Parameters.NearbyParameters.FieldRadius = fits.Metadata.ViewSize.DiameterDeg * 0.5;
                    }
                    catch (Exception e)
                    {
                        throw new ImageAnalysisException(
                            "Image did not have the required FITS headers to solve using the coordinates from FITS headers",
                            e);
                    }
                }

            }
            else if (fileExtension is ".png" or ".jpg" or ".jpeg")
            {
                _logger.LogTrace("File appears to be a png/jpg file");
                try
                {
                    var imgReader = new CommonFormatsImageReader();
                    image = imgReader.FromStream(stream);
                }
                catch (Exception e)
                {
                    throw new FileFormatException($"Could not use image file: {e.Message}", e);
                }
            }
            else
            {
                // Should never really get here, because we should have validated the file extension before this.
                throw new FileFormatException($"The file extension '{fileExtension}' is not supported");
            }

            
            model.ImageHeight = image.Metadata.ImageHeight;
            model.ImageWidth = image.Metadata.ImageWidth;
            _logger.LogTrace($"Image dimensions: {model.ImageWidth}x{model.ImageHeight}");

            // Metadata transformations
            if (metadata != null && metadata.ContainsKey("CalculateFieldRadiusFromArcSecsPerPixel") && model.Parameters.Mode == "nearby")
            {
                double appValue = (double)metadata["CalculateFieldRadiusFromArcSecsPerPixel"];
                var radiusArcsecs = Math.Sqrt(model.ImageWidth * model.ImageWidth + model.ImageHeight * model.ImageHeight) * 0.5 * appValue;
                var radiusDeg = radiusArcsecs / 3600.0;
                model.Parameters.NearbyParameters.FieldRadius = radiusDeg;
                _logger.LogTrace($"Calculated image field radius from arcsecperpixel: {radiusDeg:F} deg");
            }

            _logger.LogTrace("Detecting stars");
            var starDetector = new DefaultStarDetector();
            var stars = starDetector.DetectStars(image).ToList();
            model.Stars = stars;
            _logger.LogTrace($"The detector found {stars.Count} stars");

            _logger.LogTrace($"Analysis complete");
        }
        catch (FileFormatException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new ImageAnalysisException($"Image analysis and star detection failed: {e.Message}", e);
        }
        
    }
    
}