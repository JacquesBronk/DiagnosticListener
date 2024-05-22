using Diagnostics.Lib.Domain;
using Diagnostics.Lib.Infra;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Diagnostics.Lib;

public static class DependencyInjection
{
    public static IServiceCollection AddDiagnostics(this IServiceCollection services)
    {
        services.AddFeatureManagement();
        services.AddTransient(typeof(DiagnosticWrapper<>));
        services.AddHostedService<DiagnosticListenerService>();
        return services;
    }
}