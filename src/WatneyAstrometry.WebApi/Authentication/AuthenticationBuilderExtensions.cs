// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Authentication;

namespace WatneyAstrometry.WebApi.Authentication;

internal static class AuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddApiKeySupport(this AuthenticationBuilder authenticationBuilder, Action<ApiKeyAuthenticationOptions> options)
    {
        return authenticationBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, options);
    }
}