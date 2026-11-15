using System.Security.Cryptography;
using System.Text;
using NewsletterService;

public static class FeatureApi
{
    public static IEndpointRouteBuilder MapSubscriberToggle(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/features/subscriber-service", async (
            HttpContext context, IConfiguration config, SubscriberReleaseToggle toggle) =>
        {
            if (!Authorized(context, config)) return Results.Unauthorized();
            return Results.Ok(new { enabled = await toggle.IsEnabledAsync() });
        });
        app.MapPut("/admin/features/subscriber-service", async (
            HttpContext context, IConfiguration config, SubscriberReleaseToggle toggle, ToggleRequest request) =>
        {
            if (!Authorized(context, config)) return Results.Unauthorized();
            try
            {
                await toggle.SetAsync(request.Enabled);
                return Results.Ok(new { enabled = request.Enabled });
            }
            catch (Exception) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
        });
        return app;
    }

    private static bool Authorized(HttpContext context, IConfiguration config)
    {
        var expected = config["FeatureFlags:AdminKey"];
        if (string.IsNullOrWhiteSpace(expected) ||
            !context.Request.Headers.TryGetValue("X-Admin-Key", out var supplied)) return false;
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(supplied.ToString());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}

public sealed record ToggleRequest(bool Enabled);
