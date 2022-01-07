// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Newtonsoft.Json;
using WatneyAstrometry.Core;

namespace WatneyAstrometry.WebApi.Models;

public class JobModel
{
    public string Id { get; set; }

    // Needed to guarantee compatibility with Astrometry.net; it uses numeric IDs.
    // Only needed for the Compatibility API and does not need to be in regular API responses.
    [System.Text.Json.Serialization.JsonIgnore]
    public int NumericId { get; set; }

    public JobParametersModel Parameters { get; set; }

    // This ignore is for HTTP responses. Newtonsoft.Json is used elsewhere.
    [System.Text.Json.Serialization.JsonIgnore] 
    public List<ImageStar> Stars { get; set; }
    public JobStatus Status { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public JobSolutionProperties Solution { get; set; }
    public DateTimeOffset Updated { get; set; }
    public DateTimeOffset? SolveStarted { get; set; }
    public string OriginalFilename { get; set; }
    
}