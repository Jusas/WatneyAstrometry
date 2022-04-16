// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using WatneyAstrometry.WebApi.Controllers.Compatibility;

namespace WatneyAstrometry.WebApi.Controllers;

internal class ControllerProvider : ControllerFeatureProvider
{
    private readonly WatneyApiConfiguration _config;

    public ControllerProvider(WatneyApiConfiguration config)
    {
        _config = config;
    }

    protected override bool IsController(TypeInfo typeInfo)
    {
        var isController = base.IsController(typeInfo);

        if (isController && typeInfo.Name.Equals(nameof(AstrometryNetCompatController)))
        {
            return _config.EnableCompatibilityApi;
        }

        return isController;
    }
}