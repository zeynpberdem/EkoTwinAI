using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace EkoTwin.Infrastructure;

/// <summary>
/// Controller'larda yakalanmayan tüm hataları tek bir yerde yakalar.
/// İstemciye teknik hata sayfası yerine Türkçe, standart bir ProblemDetails (RFC 9457) yanıtı döner;
/// hatanın ayrıntısı loglara yazılır. Ayrıntılar yalnızca geliştirme ortamında yanıta eklenir.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;
    private readonly IProblemDetailsService _problemDetails;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment env,
        IProblemDetailsService problemDetails)
    {
        _logger = logger;
        _env = env;
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        // İstemci isteği iptal ettiyse (sayfayı kapattı vb.) hata sayılmaz.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = 499; // Client Closed Request (yaygın kullanım)
            return true;
        }

        var (status, title, detail) = Classify(exception);

        if (status >= 500)
            _logger.LogError(exception, "İşlenmeyen hata: {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (_env.IsDevelopment())
            problem.Extensions["exception"] = $"{exception.GetType().Name}: {exception.Message}";

        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    /// <summary>Hata türüne göre HTTP durum kodu ve kullanıcıya gösterilecek mesaj.</summary>
    internal static (int Status, string Title, string Detail) Classify(Exception exception)
    {
        // Veritabanına ulaşılamıyor (PostgreSQL kapalı, şifre yanlış, ağ sorunu vb.).
        // EF Core, Npgsql hatasını kendi hatasının (InvalidOperationException, DbUpdateException)
        // İÇİNE sarar; bu yüzden yalnızca dıştaki hataya değil tüm iç hata zincirine bakılır.
        if (IsDatabaseUnavailable(exception))
        {
            return (
                StatusCodes.Status503ServiceUnavailable,
                "Veritabanına ulaşılamıyor",
                "Sistem şu anda veritabanına bağlanamıyor. Lütfen biraz sonra tekrar deneyin.");
        }

        // Bozuk JSON vb. istemci kaynaklı okuma hataları
        if (exception is BadHttpRequestException)
        {
            return (
                StatusCodes.Status400BadRequest,
                "Geçersiz istek",
                "Gönderilen istek okunamadı.");
        }

        return (
            StatusCodes.Status500InternalServerError,
            "Beklenmeyen bir hata oluştu",
            "İşleminiz sırasında beklenmeyen bir hata oluştu. Sorun devam ederse sistem yöneticisine başvurun.");
    }

    private static bool IsDatabaseUnavailable(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            // PostgresException = sunucuya ulaşıldı ama SQL hatası verdi (örn. kısıt ihlali) → bağlantı sorunu DEĞİL
            if (e is PostgresException)
                return false;

            if (e is NpgsqlException or TimeoutException or System.Net.Sockets.SocketException)
                return true;
        }
        return false;
    }
}
