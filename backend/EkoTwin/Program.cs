using System.Text.Json.Serialization;
using EkoTwin.Data;
using EkoTwin.Infrastructure;
using EkoTwin.Services;
using EkoTwin.Services.Narrative;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Veritabanı ----------
// Bağlantı dizesi: geliştirmede user-secrets, canlıda ortam değişkeni (ConnectionStrings__DefaultConnection)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- İş mantığı servisleri (her HTTP isteği için yeni örnek) ----------
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ICarbonFootprintService, CarbonFootprintService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IBenchmarkService, BenchmarkService>();

// ---------- Yapay zekâ rapor mektubu (A7) ----------
// Anahtar ve model user-secrets'tan okunur (Gemini:ApiKey, Gemini:Model). Tanımlı değilse
// sistem kural tabanlı metinle çalışmaya devam eder.
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddHttpClient<INarrativeWriter, GeminiNarrativeWriter>();
builder.Services.AddScoped<INarrativeService, NarrativeService>();

// Saat: testlerde sabit tarih verilebilsin diye doğrudan DateTime.UtcNow yerine TimeProvider kullanılır.
builder.Services.AddSingleton(TimeProvider.System);

// ---------- Controller'lar ----------
// Enum'lar JSON'da sayı yerine metin olarak yazılır ("EnergyEfficiency")
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- Hata yönetimi ----------
// Yakalanmayan hatalar Türkçe ProblemDetails yanıtına çevrilir (bkz. GlobalExceptionHandler)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------- CORS ----------
// Frontend farklı bir adreste (port) çalışacağı için izin verilen adresler appsettings'ten okunur.
const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// ---------- Sağlık kontrolü ----------
// /health: uygulama ve veritabanı ayakta mı?
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(); // gövdesiz 404/405 vb. yanıtlara da ProblemDetails ekler

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteAsync
});

app.Run();

/// <summary>Entegrasyon testlerinin (WebApplicationFactory) uygulamayı başlatabilmesi için.</summary>
public partial class Program;
