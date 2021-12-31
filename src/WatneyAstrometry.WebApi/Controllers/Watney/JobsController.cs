using Microsoft.AspNetCore.Mvc;
using WatneyAstrometry.WebApi.Models;
using WatneyAstrometry.WebApi.Services;
using WatneyAstrometry.WebApi.Utils;

namespace WatneyAstrometry.WebApi.Controllers.Watney
{
    [ApiController]
    [Route("api/watney/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly ILogger<JobsController> _logger;

        private readonly IJobManager _jobManager;

        // https://benfoster.io/blog/mvc-to-minimal-apis-aspnet-6/
        public JobsController(ILogger<JobsController> logger, IJobManager jobManager)
        {
            _logger = logger;
            _jobManager = jobManager;
        }


        /// <summary>
        /// Submit a new solver job.
        /// </summary>
        /// <param name="unifiedModel"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost]
        [ProducesResponseType(statusCode: 400, type: typeof(ApiBadRequestErrorResponse))]
        [ProducesResponseType(statusCode: 500, type: typeof(ApiInternalErrorResponse))]
        [ProducesResponseType(statusCode: 201, type: typeof(JobModel))]
        public async Task<IActionResult> Post([FromForm] JobFormUnifiedModel unifiedModel, IFormFile image)
        {
            unifiedModel.Image = image;
            ApiBadRequestErrorResponse badRequestResponse = new ApiBadRequestErrorResponse();
            if (!ModelState.IsValid)
                badRequestResponse = ModelState.ProduceErrorResponse();

            var extendedValidationErrors = unifiedModel?.Validate() ?? Array.Empty<string>();

            if(extendedValidationErrors.Any())
                badRequestResponse.Errors = badRequestResponse.Errors.Concat(extendedValidationErrors).ToArray();

            if(badRequestResponse.Errors.Any())
                return BadRequest(badRequestResponse);

            // TODO timeouts?
            
            var createdJob = await _jobManager.PrepareJob(unifiedModel);
            return Ok(createdJob);

            // https://stackoverflow.com/questions/51614373/multipart-form-data-images-upload-with-json-asp-net-core-api
            
        }
        
        [HttpGet("{id}")]
        [ProducesResponseType(statusCode: 500, type: typeof(ApiInternalErrorResponse))]
        [ProducesResponseType(statusCode: 200, type: typeof(JobModel))]
        [ProducesResponseType(statusCode: 404, type: typeof(ApiNotFoundResponse))]
        public async Task<IActionResult> Get(string id)
        {
            var job = await _jobManager.GetJob(id);
            if (job == null)
                return NotFound(new ApiNotFoundResponse
                {
                    Message = $"Job {id} was not found"
                });
            return Ok(job);
        }

        [HttpGet("{id}/status")]
        [ProducesResponseType(statusCode: 500, type: typeof(ApiInternalErrorResponse))]
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

        [ProducesResponseType(statusCode: 500, type: typeof(ApiInternalErrorResponse))]
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