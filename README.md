# Framework.Core.Logging

ÖdeAL tarafından geliştirilen, ASP.NET Core uygulamaları için kapsamlı loglama çözümü sağlayan NuGet paketi.

## Özellikler

- 🚀 **HTTP Request/Response Logging**: Gelen ve giden HTTP isteklerini otomatik olarak loglar
- 🔗 **HttpClient Logging**: HttpClient ile yapılan dış servis çağrılarını loglar
- 🎯 **Method-Level Logging**: Action Filter ile controller metodlarının çalışma sürelerini ve detaylarını loglar
- 📊 **Correlation ID**: İstekler arası takip için correlation ID desteği
- ⚙️ **Konfigurasyon**: Esnek konfigürasyon seçenekleri
- 🏗️ **Autofac Integration**: Dependency injection desteği

## Kurulum

```bash
dotnet add package Framework.Core.Logging
```

## Kullanım

### 1. Startup.cs'de Servisleri Kaydetme

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Logging servilerini ekle
    services.AddFrameworkLogging(configuration =>
    {
        configuration.EnableHttpLogging = true;
        configuration.EnableMethodLogging = true;
        configuration.LogLevel = LogLevel.Information;
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // HTTP logging middleware'ini ekle
    app.UseHttpLogging();
    
    // Diğer middleware'ler...
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
services.AddFrameworkLogging(config =>
{
    config.EnableHttpLogging = true;          // HTTP logging aktif/pasif
    config.EnableMethodLogging = true;        // Method logging aktif/pasif
    config.LogLevel = LogLevel.Information;   // Minimum log seviyesi
    config.IncludeHeaders = true;             // HTTP headers loglanacak mı
    config.IncludeBody = true;                // Request/Response body loglanacak mı
    config.MaxBodySize = 4096;                // Maksimum body boyutu (byte)
});
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

- .NET 6.0 veya üzeri
- ASP.NET Core 6.0 veya üzeri

## Bağımlılıklar

- Autofac 6.5.0
- Microsoft.Extensions.DependencyInjection 7.0.0
- Microsoft.Extensions.Logging.Configuration 7.0.0
- Microsoft.AspNetCore.Http 2.2.2
- Microsoft.AspNetCore.Mvc.Core 2.2.5
- Microsoft.Extensions.Http 7.0.0
- Newtonsoft.Json 13.0.3
- System.IdentityModel.Tokens.Jwt 7.0.3
- Microsoft.IO.RecyclableMemoryStream 2.3.2

## Lisans

Bu proje ÖdeAL tarafından geliştirilmiştir.

## Katkıda Bulunma

Katkıda bulunmak için lütfen pull request gönderin veya issue açın.

## Sürüm Geçmişi

### v1.1.0
- HTTP Request/Response logging eklendi
- HttpClient logging desteği eklendi
- Method-level logging desteği eklendi
- Correlation ID desteği eklendi

### v1.0.0
- İlk sürüm
- Temel logging altyapısı