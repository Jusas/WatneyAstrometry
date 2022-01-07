// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace WatneyAstrometry.WebApi.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private WatneyApiConfiguration _apiConfiguration;

    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options, ILoggerFactory logger, 
        UrlEncoder encoder, ISystemClock systemClock,
        WatneyApiConfiguration configuration) 
        : base(options, logger, encoder, systemClock)
    {
        _apiConfiguration = configuration;
    }
    

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        bool gotApiKeyFromHeader = Request.Headers.TryGetValue("apikey", out var headerApiKey);
        bool gotApiKeyFromQuery = Request.Query.TryGetValue("apikey", out var queryApiKey);

        if (!gotApiKeyFromHeader && !gotApiKeyFromQuery)
        {
            return AuthenticateResult.NoResult();
        }

        var availableApiKeys = _apiConfiguration.ApiKeys;

        var requestApiKey = gotApiKeyFromHeader ? headerApiKey : queryApiKey;

        if (availableApiKeys.Values.Where(k => !string.IsNullOrEmpty(k)).Any(apiKey => apiKey == requestApiKey))
        {
            var match = availableApiKeys.First(key => key.Value.Equals(requestApiKey));
            var claim = new Claim(ClaimTypes.Name, match.Key);
            var ci = new ClaimsIdentity(new Claim[] { claim }, Options.AuthenticationType);
            var identities = new List<ClaimsIdentity>() { ci };
            var principal = new ClaimsPrincipal(identities);

            return AuthenticateResult.Success(new AuthenticationTicket(principal, Options.Scheme));
        }
        

        return AuthenticateResult.NoResult();

    }

    //protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    //{
    //    Response.StatusCode = 401;
    //    Response.ContentType = ProblemDetailsContentType;
    //    var problemDetails = new UnauthorizedProblemDetails();

    //    await Response.WriteAsync(JsonSerializer.Serialize(problemDetails, DefaultJsonSerializerOptions.Options));
    //}
}