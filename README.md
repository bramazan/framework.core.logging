# Framework.Core.Logging

ÖdeAL tarafından geliştirilen, ASP.NET Core uygulamaları için kapsamlı loglama çözümü sağlayan NuGet paketi.

## Özellikler

- 🚀 **HTTP Request/Response Logging**: Gelen ve giden HTTP isteklerini otomatik olarak loglar
- 🔗 **HttpClient Logging**: HttpClient ile yapılan dış servis çağrılarını loglar
- 🎯 **Method-Level Logging**: Action Filter ile controller metodlarının çalışma sürelerini ve detaylarını loglar
- 📊 **Correlation ID**: İstekler arası takip için correlation ID desteği
- ⚙️ **Konfigurasyon**: Esnek konfigürasyon seçenekleri
- 🏗️ **Microsoft DI Integration**: .NET Core built-in dependency injection desteği

## Kurulum

```bash
dotnet add package Framework.Core.Logging
```

## Kullanım

### 1. Modern Fluent API (Önerilen) - Tek Satırla Tüm Otomatik Logging

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // TEK SETUP ile her şey otomatik loglanır!
    services.AddFrameworkLogging(builder =>
        builder
            .SetApplicationName("MyApp")
            
            // HTTP Request/Response otomatik logging
            .EnableHttpLogging()
            .LogHeaders()
            .LogBody()
            .SetMaxContentLength(8192)
            .ExcludePaths("/health", "/metrics", "/swagger")
            
            // Global Exception Handling - Hiçbir exception kaçmaz!
            .EnableGlobalExceptionHandling()
            
            // Async Logging - Performance için
            .EnableAsyncLogging()
            
            // Auto-Instrumentation - Her şey otomatik loglanır!
            .EnableAutoInstrumentation()
            .EnableDatabaseInstrumentation()    // SQL queries otomatik
            .EnableRedisInstrumentation()       // Redis operations otomatik
            .EnableBackgroundServiceInstrumentation() // Background jobs otomatik
            
            // Security & Performance
            .EnableSecurityFeatures()           // Enhanced data masking
            .OptimizeMemoryUsage()              // Object pooling
            
            // Method logging
            .EnableMethodLogging()
            .LogMethodParameters()
            .LogExecutionTime()
            
            // Correlation ID
            .WithCorrelationId()
            .SetCorrelationIdHeader("X-Request-Id")
            .SetLogLevel(LogLevel.Information)
    );
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // TEK MIDDLEWARE - Her şey çalışır!
    app.UseFrameworkLogging(); // Global Exception + HTTP Logging + Auto-Instrumentation
    
    // Diğer middleware'ler...
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

### 🎯 SONUÇ: Hiç kod yazmadan bunlar otomatik loglanır:
- ✅ **HTTP Request/Response** (gelen/giden)
- ✅ **Database queries** (Entity Framework)
- ✅ **Redis operations** (GET, SET, DELETE)
- ✅ **Background services** (IHostedService)
- ✅ **Exception'lar** (global handling)
- ✅ **Method calls** (controller + service)
- ✅ **HttpClient calls** (dış API'lar)

### 2. Geleneksel Configuration Yöntemi

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Geleneksel configuration-based yaklaşım
    services.AddFrameworkCoreLogging(Configuration);
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseHttpLogging();
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

### 2. Controller'larda Method Logging

```csharp
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    private readonly IAppLogger _logger;

    public MyController(IAppLogger logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [MethodLogging] // Method seviyesinde loglama
    public async Task<IActionResult> GetData()
    {
        _logger.LogInformation("Getting data...");
        return Ok();
    }
}
```

### 3. HttpClient Logging

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient<MyApiClient>()
            .AddHttpMessageHandler<HttpClientLoggingHandler>();
}
```

### 4. Manual Logging

```csharp
public class MyService
{
    private readonly IAppLogger _logger;

    public MyService(IAppLogger logger)
    {
        _logger = logger;
    }

    public void DoSomething()
    {
        _logger.LogInformation("İşlem başladı");
        
        try
        {
            // İş mantığı
            _logger.LogInformation("İşlem başarılı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem sırasında hata oluştu");
        }
    }
}
```

## Konfigürasyon

### AppLogger Konfigürasyonu

```csharp
### Fluent API Konfigürasyonu

```csharp
services.AddFrameworkLogging(builder =>
{
    // HTTP Logging
    builder.EnableHttpLogging()
           .LogHeaders()
           .LogBody()
           .SetMaxContentLength(8192)
           .ExcludePaths("/health", "/metrics")
           .MaskSensitiveFields("password", "token", "pin")
           .MaskSensitiveHeaders("Authorization", "X-API-Key");

    // Method Logging
    builder.EnableMethodLogging()
           .LogMethodParameters()
           .LogExecutionTime()
           .SetMinimumExecutionTime(100); // 100ms'den uzun işlemleri logla

    // Correlation ID
    builder.WithCorrelationId()
           .SetCorrelationIdHeader("X-Correlation-Id")
           .GenerateCorrelationIdIfMissing()
           .IncludeCorrelationIdInResponse();

    // Genel Ayarlar
    builder.SetApplicationName("MyApplication")
           .EnableConsoleLogging()
           .SetLogLevel(LogLevel.Information);
});
```

### Configuration File Yöntemi

```json
{
  "Logging": {
    "ApplicationName": "MyApp",
    "ConsoleEnabled": true,
    "DebugMode": false,
    "HttpLogging": {
      "LogRequests": true,
      "LogResponses": true,
      "MaxBodySize": 4096,
      "IgnoredPaths": ["/health", "/metrics"]
    }
  }
}
```

```csharp
// Startup.cs'de
services.AddFrameworkCoreLogging(Configuration);
```
```

### HTTP Logging Konfigürasyonu

```csharp
services.Configure<HttpLoggingConfiguration>(options =>
{
    options.LogHeaders = true;
    options.LogBody = true;
    options.MaxContentLength = 4096;
    options.ExcludedPaths = new[] { "/health", "/metrics" };
});
```

## Log Türleri

Framework aşağıdaki log türlerini destekler:

- `LogType.Information`: Genel bilgi logları
- `LogType.Warning`: Uyarı logları
- `LogType.Error`: Hata logları
- `LogType.Debug`: Debug logları
- `LogType.HttpRequest`: HTTP request logları
- `LogType.HttpResponse`: HTTP response logları
- `LogType.MethodEntry`: Method giriş logları
- `LogType.MethodExit`: Method çıkış logları

## Gereksinimler

- .NET 9.0 veya üzeri
- ASP.NET Core 9.0 veya üzeri

## Bağımlılıklar

- Microsoft.Extensions.DependencyInjection 9.0.0
- Microsoft.Extensions.Logging.Configuration 9.0.0
- Microsoft.AspNetCore.Http.Abstractions 2.2.0
- Microsoft.AspNetCore.Mvc.Core 2.2.5
- Microsoft.Extensions.Http 9.0.0
- Newtonsoft.Json 13.0.3
- System.IdentityModel.Tokens.Jwt 8.2.0
- Microsoft.IO.RecyclableMemoryStream 3.0.1

## Lisans

Bu proje ÖdeAL tarafından geliştirilmiştir.

## Katkıda Bulunma

Katkıda bulunmak için lütfen pull request gönderin veya issue açın.

## Gelişmiş Kullanım

### HttpClient Logging

```csharp
services.AddHttpClient<MyApiClient>()
        .AddHttpClientLogging();
```

### Action Filter ile Method Logging

```csharp
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    [HttpGet]
    [MethodLogging] // Otomatik method logging
    public async Task<IActionResult> GetData()
    {
        return Ok();
    }
}
```

### Programmatic Configuration

```csharp
services.AddFrameworkLogging(builder =>
    builder.Configure(options =>
    {
        options.ApplicationName = "CustomApp";
        options.ConsoleEnabled = Environment.IsDevelopment();
        options.HttpLogging.MaxContentLength = 16384;
        options.MethodLogging.MinimumExecutionTimeMs = 500;
    })
);
```

## Sürüm Geçmişi

### v1.5.0 (CURRENT) 🚀
- **MAJOR UPDATE**: Global Exception Handling middleware eklendi
- **YENİ**: Async Logging ile background queue processing
- **YENİ**: Auto-Instrumentation sistemi
  - Database operations otomatik logging (Entity Framework, ADO.NET)
  - Redis operations otomatik logging
  - Background services otomatik logging
- **YENİ**: Memory optimization (Object pooling, batch processing)
- **YENİ**: Enhanced security features (Advanced data masking)
- **YENİ**: UseFrameworkLogging() tek middleware ile tüm features
- **YENİ**: "Add once, log everything" - Zero-code instrumentation
- Performance iyileştirmeleri ve memory efficiency
- Kapsamlı documentation ve usage examples

### v1.4.0
- **YENİ**: Modern Fluent API desteği
- **YENİ**: LoggingOptions ile type-safe configuration
- **YENİ**: IFrameworkLoggingBuilder interface
- Geliştirilmiş developer experience
- Backward compatibility korundu

### v1.3.0
- **BREAKING CHANGE**: Autofac bağımlılığı kaldırıldı
- Tamamen Microsoft.Extensions.DependencyInjection kullanılıyor
- .NET 9.0 desteği
- Nullable reference types uyarıları düzeltildi

### v1.2.0
- .NET 9.0 desteği eklendi

### v1.1.0
- HTTP Request/Response logging eklendi
- HttpClient logging desteği eklendi
- Method-level logging desteği eklendi
- Correlation ID desteği eklendi

### v1.0.0
- İlk sürüm
- Temel logging altyapısı