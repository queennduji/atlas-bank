using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AtlasBank.Shared.Middleware;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, lc) => lc
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", ctx.HostingEnvironment.ApplicationName)
            .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

        return builder;
    }

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging();
        return app;
    }
}
