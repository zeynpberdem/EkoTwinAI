using System.Text.Json.Serialization;
using EkoTwin.Data;
using EkoTwin.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı: PostgreSQL (bağlantı dizesi appsettings.json -> ConnectionStrings:DefaultConnection)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// İş mantığı servisleri (her HTTP isteği için yeni örnek)
builder.Services.AddScoped<IAssessmentService, AssessmentService>();

// Controller'lar; enum'lar JSON'da sayı yerine metin olarak yazılır ("EnergyEfficiency")
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Swagger: /swagger adresinde API test arayüzü
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
