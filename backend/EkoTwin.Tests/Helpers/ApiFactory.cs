using EkoTwin.Data;
using EkoTwin.Services.Narrative;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EkoTwin.Tests.Helpers;

/// <summary>
/// Entegrasyon testleri için uygulamayı bellekte başlatır (gerçek HTTP hattı: routing, doğrulama,
/// hata yönetimi, CORS, JSON). PostgreSQL yerine InMemory veritabanı kullanılır.
/// <see cref="ConfigureTestServices"/> ile testlere özel servisler (örn. hata fırlatan sahte servis) eklenebilir.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"EkoTwinApiTest-{Guid.NewGuid()}";

    public Action<IServiceCollection>? ConfigureTestServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // PostgreSQL ayarını kaldır, yerine InMemory koy
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_databaseName));

            // Testler gerçek yapay zekâ servisini ASLA çağırmaz (bilgisayarda user-secrets'ta anahtar olsa bile).
            // Varsayılan: ayarlı olmayan sahte yazıcı → kural tabanlı metin. Testler kendi sahtesini verebilir.
            services.RemoveAll<INarrativeWriter>();
            services.AddSingleton<INarrativeWriter>(new FakeNarrativeWriter { IsConfigured = false });

            ConfigureTestServices?.Invoke(services);
        });
    }

    /// <summary>Veritabanını oluşturur (16 soru seed edilir) ve bir HTTP istemcisi döner.</summary>
    public HttpClient CreateClientWithDatabase()
    {
        using (var scope = Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        }
        return CreateClient();
    }
}
