namespace WebApi.Authorization;

using Microsoft.Extensions.Options;
using System.Security.Claims;
using WebApi.Helpers;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _appSettings;

    public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
        _next = next;
        _appSettings = appSettings.Value;
    }

    public async Task Invoke(HttpContext context, DataContext dataContext, IJwtUtils jwtUtils)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        // SignalR service passes access_token query parameter rather than Authorization header
        if (token == null)
        {
            if (context.Request.Query["access_token"].ToString() != null)
            {
                token = context.Request.Query["access_token"].ToString();
            }
        }

        var jwtToken = jwtUtils.ValidateJwtToken(token);
        if (jwtToken != null)
        {
            var accountId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
            if (accountId != null)
            {
                // attach account to context on successful jwt validation
                context.Items["Account"] = await dataContext.Accounts.FindAsync(accountId.Value);

                // set up user principal with our custom claims                
                var identity = new ClaimsIdentity(jwtToken.Claims, "JWT");
                var principal = new ClaimsPrincipal(new List<ClaimsIdentity> { identity });
                context.User = principal;
            }
        }
        

        await _next(context);
    }
}