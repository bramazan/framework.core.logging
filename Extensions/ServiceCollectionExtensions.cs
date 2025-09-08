using Framework.Core.Logging.ActionFilter;
using Framework.Core.Logging.Handler;
using Framework.Core.Logging.Helper;
using Framework.Core.Logging.Logging.AppLogger;
using Framework.Core.Logging.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using System;

namespace Framework.Core.Logging.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Framework.Core.Logging HTTP logging servislerini ekler
        /// </summary>
        public static IServiceCollection AddFrameworkCoreLogging(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuration oluştur
            var loggingConfig = PlatformAppLoggerConfiguration.Create(configuration);
            services.AddSingleton(loggingConfig);

            // Core logging servisleri ekle
            services.AddSingleton<ICorrelationIdHelper, CorrelationIdHelper>();
            services.AddSingleton<IAppLogger, AppLogger>();

            // HTTP logging bileşenleri ekle
            services.AddTransient<HttpLoggingMiddleware>();
            services.AddTransient<HttpClientLoggingHandler>();
            services.AddTransient<MethodLoggingActionFilter>();

            return services;
        }

        /// <summary>
        /// HttpClient için logging handler'ını ekler
        /// </summary>
        public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder)
        {
            return builder.AddHttpMessageHandler<HttpClientLoggingHandler>();
        }

    }

    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// HTTP Request/Response logging middleware'ini ekler
        /// </summary>
        public static IApplicationBuilder UseHttpLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<HttpLoggingMiddleware>();
        }

        /// <summary>
        /// Framework.Core.Logging HTTP logging pipeline'ını ekler (middleware + exceptions)
        /// </summary>
        public static IApplicationBuilder UseFrameworkCoreHttpLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<HttpLoggingMiddleware>();
        }
    }
}