using Framework.Core.Logging.ActionFilter;
using Framework.Core.Logging.Builder;
using Framework.Core.Logging.Handler;
using Framework.Core.Logging.Helper;
using Framework.Core.Logging.Logging.AppLogger;
using Framework.Core.Logging.Middleware;
using Framework.Core.Logging.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Framework.Core.Logging.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Framework.Core.Logging servislerini fluent API ile yapılandırır (Önerilen yöntem)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configure">Fluent configuration builder</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddFrameworkLogging(this IServiceCollection services,
            Action<IFrameworkLoggingBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            // HttpContextAccessor'ı ekle (eğer yoksa)
            services.AddHttpContextAccessor();

            // Modern options pattern kullan
            var options = new LoggingOptions();
            var builder = new LoggingBuilder(services, options);
            
            // Fluent API ile konfigürasyon yap
            configure(builder);

            // Options'ı singleton olarak kaydet ve validation ekle
            services.AddSingleton(options);
            services.Configure<LoggingOptions>(opt =>
            {
                opt.ApplicationName = options.ApplicationName;
                opt.ConsoleEnabled = options.ConsoleEnabled;
                opt.DebugMode = options.DebugMode;
                opt.LogLevel = options.LogLevel;
                opt.HttpLogging = options.HttpLogging;
                opt.MethodLogging = options.MethodLogging;
                opt.CorrelationId = options.CorrelationId;
            });

            // Options validation ekle (DataAnnotations validation gerektiğinde aktifleştirilebilir)
            services.AddOptions<LoggingOptions>();

            // Legacy configuration'ı modern options'a map et
            var legacyConfig = CreateLegacyConfigFromOptions(options);
            services.AddSingleton(legacyConfig);

            // Core logging servisleri ekle
            services.AddScoped<ICorrelationIdHelper, CorrelationIdHelper>();
            services.AddSingleton<IAppLogger, AppLogger>();

            // HTTP logging bileşenleri ekle
            services.AddTransient<HttpLoggingMiddleware>();
            services.AddTransient<HttpClientLoggingHandler>();
            services.AddTransient<MethodLoggingActionFilter>();

            return services;
        }

        /// <summary>
        /// Framework.Core.Logging servislerini configuration ile yapılandırır (Geleneksel yöntem)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddFrameworkCoreLogging(this IServiceCollection services, IConfiguration configuration)
        {
            // HttpContextAccessor'ı ekle (eğer yoksa)
            services.AddHttpContextAccessor();

            // Legacy configuration oluştur ve singleton olarak kaydet
            var loggingConfig = PlatformAppLoggerConfiguration.Create(configuration);
            services.AddSingleton(loggingConfig);

            // Modern options'ı legacy config'den oluştur
            var modernOptions = CreateModernOptionsFromLegacy(loggingConfig, configuration);
            services.AddSingleton(modernOptions);
            services.Configure<LoggingOptions>(opt =>
            {
                opt.ApplicationName = modernOptions.ApplicationName;
                opt.ConsoleEnabled = modernOptions.ConsoleEnabled;
                opt.DebugMode = modernOptions.DebugMode;
                opt.LogLevel = modernOptions.LogLevel;
                opt.HttpLogging = modernOptions.HttpLogging;
                opt.MethodLogging = modernOptions.MethodLogging;
                opt.CorrelationId = modernOptions.CorrelationId;
            });

            // Core logging servisleri ekle
            services.AddScoped<ICorrelationIdHelper, CorrelationIdHelper>();
            services.AddSingleton<IAppLogger, AppLogger>();

            // HTTP logging bileşenleri ekle
            services.AddTransient<HttpLoggingMiddleware>();
            services.AddTransient<HttpClientLoggingHandler>();
            services.AddTransient<MethodLoggingActionFilter>();

            return services;
        }

        /// <summary>
        /// Framework.Core.Logging servislerini ekler (Eski metod adı - geriye uyumluluk)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        [Obsolete("AddFrameworkLogging metodu deprecated. Yeni fluent API için AddFrameworkLogging(Action<IFrameworkLoggingBuilder>) veya geleneksel yöntem için AddFrameworkCoreLogging kullanın.", false)]
        public static IServiceCollection AddFrameworkLogging(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddFrameworkCoreLogging(configuration);
        }

        /// <summary>
        /// HttpClient için logging handler'ını ekler
        /// </summary>
        public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder)
        {
            return builder.AddHttpMessageHandler<HttpClientLoggingHandler>();
        }

        #region Helper Methods

        private static PlatformAppLoggerConfiguration CreateLegacyConfigFromOptions(LoggingOptions options)
        {
            return new PlatformAppLoggerConfiguration
            {
                LoggerApplicationName = options.ApplicationName,
                ConsoleEnabled = options.ConsoleEnabled,
                DebugMode = options.DebugMode,
                Enabled = options.ConsoleEnabled,
                HttpLogging = new HttpLoggingConfiguration
                {
                    LogRequests = options.HttpLogging.LogHeaders && options.HttpLogging.LogBody,
                    LogResponses = options.HttpLogging.LogHeaders && options.HttpLogging.LogBody,
                    LogHttpClient = options.HttpLogging.Enabled,
                    LogMethodEntryExit = options.HttpLogging.LogMethodEntryExit,
                    MaxBodySize = options.HttpLogging.MaxBodySize,
                    MaxHeaderSize = options.HttpLogging.MaxContentLength,
                    SensitiveHeaders = options.HttpLogging.SensitiveHeaders?.ToList() ?? new List<string>(),
                    SensitiveFields = options.HttpLogging.SensitiveFields?.ToList() ?? new List<string>(),
                    IgnoredPaths = options.HttpLogging.ExcludedPaths?.ToList() ?? new List<string>()
                }
            };
        }

        private static LoggingOptions CreateModernOptionsFromLegacy(PlatformAppLoggerConfiguration legacyConfig, IConfiguration configuration)
        {
            return new LoggingOptions
            {
                ApplicationName = legacyConfig.LoggerApplicationName,
                ConsoleEnabled = legacyConfig.ConsoleEnabled,
                DebugMode = legacyConfig.DebugMode,
                LogLevel = LogLevel.Information, // Default
                HttpLogging = new HttpLoggingOptions
                {
                    Enabled = legacyConfig.HttpLogging?.LogHttpClient ?? true,
                    LogHeaders = legacyConfig.HttpLogging?.LogRequests ?? true,
                    LogBody = legacyConfig.HttpLogging?.LogRequests ?? true,
                    MaxContentLength = legacyConfig.HttpLogging?.MaxHeaderSize ?? 4096,
                    ExcludedPaths = legacyConfig.HttpLogging?.IgnoredPaths?.ToArray() ?? Array.Empty<string>(),
                    SensitiveFields = legacyConfig.HttpLogging?.SensitiveFields?.ToArray() ?? new[] { "password", "token", "secret", "key" },
                    SensitiveHeaders = legacyConfig.HttpLogging?.SensitiveHeaders?.ToArray() ?? new[] { "Authorization", "Cookie", "Set-Cookie" },
                    LogMethodEntryExit = legacyConfig.HttpLogging?.LogMethodEntryExit ?? true,
                    MaxBodySize = legacyConfig.HttpLogging?.MaxBodySize ?? 64 * 1024
                },
                MethodLogging = new MethodLoggingOptions
                {
                    Enabled = true,
                    LogParameters = true,
                    LogReturnValues = false,
                    LogExecutionTime = true,
                    MinimumExecutionTimeMs = 0
                },
                CorrelationId = new CorrelationIdOptions
                {
                    Enabled = true,
                    HeaderName = "X-Correlation-Id",
                    GenerateIfMissing = true,
                    IncludeInResponse = true
                }
            };
        }

        #endregion
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