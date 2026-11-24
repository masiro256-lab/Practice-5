using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
namespace ProductCatalogApp.Client.Services;

public class ApiAuthenticationHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext;
        var token = context == null ? null : await context.GetTokenAsync("access_token");
        if (request.Headers.Authorization == null && !string.IsNullOrEmpty(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
