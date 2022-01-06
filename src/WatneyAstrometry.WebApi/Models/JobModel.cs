using Newtonsoft.Json;
using WatneyAstrometry.Core;

namespace WatneyAstrometry.WebApi.Models;

public class JobModel
{
    public string Id { get; set; }
    public JobParametersModel Parameters { get; set; }

    // This ignore is for HTTP responses. Newtonsoft.Json is used elsewhere.
    [System.Text.Json.Serialization.JsonIgnore] 
    public List<ImageStar> Stars { get; set; }
    public JobStatus Status { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public JobSolutionProperties Solution { get; set; }
    public DateTimeOffset Updated { get; set; }

}