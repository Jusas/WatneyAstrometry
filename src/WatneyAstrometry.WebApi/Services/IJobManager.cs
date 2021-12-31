using WatneyAstrometry.WebApi.Models;

namespace WatneyAstrometry.WebApi.Services;

/// <summary>
/// The manager that is used to submit new jobs, cancel them and to get data of the jobs.
/// </summary>
public interface IJobManager
{
    Task<JobModel> PrepareJob(JobFormUnifiedModel jobFormModel);
    Task<JobModel> GetJob(string id);
    Task CancelJob(string id);

}