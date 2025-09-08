using Framework.Core.Logging.Logging.AppLogger;
using Microsoft.Extensions.Logging;

namespace Framework.Core.Logging.Instrumentation
{
    public class AutoInstrumentationManager
    {
        private readonly IAppLogger _appLogger;
        private readonly ILogger<AutoInstrumentationManager> _logger;
        private readonly AutoInstrumentationOptions _options;
        private bool _initialized = false;

        public AutoInstrumentationManager(
            IAppLogger appLogger,
            ILogger<AutoInstrumentationManager> logger,
            AutoInstrumentationOptions options)
        {
            _appLogger = appLogger;
            _logger = logger;
            _options = options;
        }

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                _logger.LogInformation("Initializing Auto-Instrumentation...");

                // Initialize Database instrumentation
                if (_options.EnableDatabaseInstrumentation)
                {
                    DatabaseInstrumentation.Initialize(_appLogger, _logger, _options.Database);
                    _logger.LogInformation("Database instrumentation initialized");
                }

                // Initialize Redis instrumentation
                if (_options.EnableRedisInstrumentation)
                {
                    RedisInstrumentation.Initialize(_appLogger, _logger, _options.Redis);
                    _logger.LogInformation("Redis instrumentation initialized");
                }

                // Initialize Background Service instrumentation
                if (_options.EnableBackgroundServiceInstrumentation)
                {
                    BackgroundServiceInstrumentation.Initialize(_appLogger, _logger, _options.BackgroundService);
                    _logger.LogInformation("Background Service instrumentation initialized");
                }

                _initialized = true;
                _logger.LogInformation("Auto-Instrumentation initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Auto-Instrumentation");
                throw;
            }
        }

        public bool IsInitialized => _initialized;

        public AutoInstrumentationOptions Options => _options;
    }

    public class AutoInstrumentationOptions
    {
        public bool EnableDatabaseInstrumentation { get; set; } = true;
        public bool EnableRedisInstrumentation { get; set; } = true;
        public bool EnableBackgroundServiceInstrumentation { get; set; } = true;
        public bool EnableHttpClientInstrumentation { get; set; } = true;

        public DatabaseInstrumentationOptions Database { get; set; } = new();
        public RedisInstrumentationOptions Redis { get; set; } = new();
        public BackgroundServiceInstrumentationOptions BackgroundService { get; set; } = new();
    }

    // Helper extensions for easy usage
    public static class AutoInstrumentationExtensions
    {
        // Database extensions
        public static async Task<T> LogDatabaseOperationAsync<T>(
            this object context,
            string operation,
            Func<Task<T>> dbOperation,
            string? commandText = null,
            Dictionary<string, object?>? parameters = null)
        {
            return await DatabaseInstrumentation.LogDbOperationAsync(operation, dbOperation, commandText, parameters);
        }

        public static async Task LogDatabaseOperationAsync(
            this object context,
            string operation,
            Func<Task> dbOperation,
            string? commandText = null,
            Dictionary<string, object?>? parameters = null)
        {
            await DatabaseInstrumentation.LogDbOperationAsync(operation, async () =>
            {
                await dbOperation();
                return 0; // Dummy return for void operations
            }, commandText, parameters);
        }

        // Redis extensions
        public static async Task<T> LogRedisOperationAsync<T>(
            this object redisClient,
            string operation,
            string key,
            Func<Task<T>> redisOperation,
            object? value = null)
        {
            return await RedisInstrumentation.LogRedisOperationAsync(operation, key, redisOperation, value);
        }

        public static async Task LogRedisOperationAsync(
            this object redisClient,
            string operation,
            string key,
            Func<Task> redisOperation,
            object? value = null)
        {
            await RedisInstrumentation.LogRedisOperationAsync(operation, key, redisOperation, value);
        }

        // Background service extensions
        public static async Task LogBackgroundOperationAsync(
            this object service,
            string serviceName,
            string operationName,
            Func<Task> operation,
            CancellationToken cancellationToken = default)
        {
            await BackgroundServiceInstrumentation.LogBackgroundOperationAsync(serviceName, operationName, operation, cancellationToken);
        }

        public static async Task<T> LogBackgroundOperationAsync<T>(
            this object service,
            string serviceName,
            string operationName,
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return await BackgroundServiceInstrumentation.LogBackgroundOperationAsync(serviceName, operationName, operation, cancellationToken);
        }
    }

    // Attribute for marking methods that should be auto-instrumented
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class AutoLogAttribute : Attribute
    {
        public string? OperationName { get; set; }
        public bool LogParameters { get; set; } = true;
        public bool LogReturnValue { get; set; } = false;
        public bool LogExecutionTime { get; set; } = true;

        public AutoLogAttribute() { }

        public AutoLogAttribute(string operationName)
        {
            OperationName = operationName;
        }
    }

    // Usage examples and documentation
    public static class AutoInstrumentationExamples
    {
        // Example: Database operation logging
        public static async Task<User?> GetUserExample(int userId)
        {
            return await DatabaseInstrumentation.LogDbOperationAsync(
                "GetUser",
                async () =>
                {
                    // Your actual database operation here
                    // e.g., return await dbContext.Users.FindAsync(userId);
                    return new User { Id = userId, Name = "Example" };
                },
                commandText: "SELECT * FROM Users WHERE Id = @userId",
                parameters: new Dictionary<string, object?> { ["userId"] = userId }
            );
        }

        // Example: Redis operation logging
        public static async Task<string?> GetCachedDataExample(string key)
        {
            return await RedisInstrumentation.LogRedisOperationAsync(
                "GET",
                key,
                async () =>
                {
                    // Your actual Redis operation here
                    // e.g., return await redis.StringGetAsync(key);
                    return "cached_value";
                }
            );
        }

        // Example: Background service operation logging
        public static async Task ProcessQueueExample()
        {
            await BackgroundServiceInstrumentation.LogBackgroundOperationAsync(
                "QueueProcessor",
                "ProcessMessages",
                async () =>
                {
                    // Your actual background processing here
                    await Task.Delay(1000); // Simulate work
                }
            );
        }

        // Example: Usage with extension methods
        public static async Task ExtensionMethodsExample()
        {
            var dbContext = new object();
            var redisClient = new object();
            var backgroundService = new object();

            // Database operation
            var user = await dbContext.LogDatabaseOperationAsync(
                "GetUser",
                async () => new User { Id = 1, Name = "Example" }
            );

            // Redis operation
            var cachedData = await redisClient.LogRedisOperationAsync(
                "GET",
                "user:1",
                async () => "cached_data"
            );

            // Background operation
            await backgroundService.LogBackgroundOperationAsync(
                "EmailService",
                "SendWelcomeEmail",
                async () => await Task.Delay(500)
            );
        }
    }

    // Simple User class for examples
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}