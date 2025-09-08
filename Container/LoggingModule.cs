using System;
using Autofac;
using Framework.Core.Logging.Helper;
using Framework.Core.Logging.Logging.AppLogger;
using Framework.Core.Logging.Middleware;
using Framework.Core.Logging.Handler;
using Framework.Core.Logging.ActionFilter;
using Microsoft.Extensions.Configuration;

namespace Framework.Core.Logging.Container
{
    public class LoggingModule : Module
    {
        private static IConfiguration _configuration;

        public static void AddLogging(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Core logging configuration
            builder.Register((c, p) => PlatformAppLoggerConfiguration.Create(_configuration)).SingleInstance();
            
            // Core logging services
            builder.RegisterType<CorrelationIdHelper>().As<ICorrelationIdHelper>().InstancePerLifetimeScope();
            builder.RegisterType<AppLogger>().As<IAppLogger>().SingleInstance();

            // HTTP logging components
            builder.RegisterType<HttpLoggingMiddleware>().InstancePerLifetimeScope();
            builder.RegisterType<HttpClientLoggingHandler>().InstancePerLifetimeScope();
            builder.RegisterType<MethodLoggingActionFilter>().InstancePerLifetimeScope();

            base.Load(builder);
        }
    }
}

