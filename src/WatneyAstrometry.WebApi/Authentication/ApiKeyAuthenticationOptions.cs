using Microsoft.AspNetCore.Authentication;

namespace WatneyAstrometry.WebApi.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "apikey";
    public string Scheme => DefaultScheme;
    public string AuthenticationType = DefaultScheme;
}
