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

    public JobManager(IJobRepository jobRepository, IQueueManager queueManager)
    {
        _jobRepository = jobRepository;
        _queueManager = queueManager;
    }

    public async Task<JobModel> PrepareJob(JobFormUnifiedModel jobFormModel)
    {
        await Task.Yield();

        var jobModel = new JobModel
        {
            Id = Guid.NewGuid().Shortened(),
            Status = JobStatus.Queued,
            Parameters = new JobParametersModel
            {
                BlindParameters = jobFormModel.Parameters.BlindParameters,
                NearbyParameters = jobFormModel.Parameters.NearbyParameters
            }
        };
        AnalyzeAndExtractStars(jobFormModel.Image, jobModel);
        
        await _jobRepository.Insert(jobModel).ConfigureAwait(false);
        _queueManager.Enqueue(jobModel.Id);
        return jobModel;
    }

    public async Task<JobModel> GetJob(string id)
    {
        return await _jobRepository.Get(id);
    }

    public async Task CancelJob(string id)
    {
        _queueManager.Cancel(id);
    }


    private void AnalyzeAndExtractStars(IFormFile file, JobModel model)
    {
        try
        {
            IImage image;
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            using var stream = file.OpenReadStream();

            if (fileExtension is ".fit" or ".fits")
            {
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

            var starDetector = new DefaultStarDetector();
            var stars = starDetector.DetectStars(image).ToList();
            model.Stars = stars;
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