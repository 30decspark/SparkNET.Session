using Microsoft.Extensions.DependencyInjection;

namespace SparkNET.Session
{
    public static class DI
    {
        public static IServiceCollection AddSparkSession(this IServiceCollection services, Action<SessionOption>? action = null)
        {
            SessionOption option = new();
            action?.Invoke(option);
            SparkSession.Initialize(option.File);
            services.AddSingleton(option);
            services.AddScoped<SparkSession>();
            return services;
        }
    }
}
