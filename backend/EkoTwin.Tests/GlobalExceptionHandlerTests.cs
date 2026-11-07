using System.Net.Sockets;
using EkoTwin.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EkoTwin.Tests;

/// <summary>
/// Hata sınıflandırma testleri: hangi hata hangi HTTP koduna dönüşüyor?
/// Özellikle EF Core'un Npgsql hatasını kendi hatasının içine sardığı durum test edilir
/// (gerçek PostgreSQL kapalıyken yakalanan hata bu şekildedir).
/// </summary>
public class GlobalExceptionHandlerTests
{
    [Fact]
    public void DogrudanNpgsqlHatasi_503()
    {
        var (status, _, _) = GlobalExceptionHandler.Classify(new NpgsqlException("bağlantı yok"));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
    }

    [Fact]
    public void EfCoreIcineSarilmisNpgsqlHatasi_503()
    {
        // PostgreSQL kapalıyken EF Core'un fırlattığı hatanın yapısı
        var wrapped = new InvalidOperationException(
            "An exception has been raised that is likely due to a transient failure.",
            new NpgsqlException("Failed to connect", new SocketException((int)SocketError.ConnectionRefused)));

        var (status, title, _) = GlobalExceptionHandler.Classify(wrapped);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal("Veritabanına ulaşılamıyor", title);
    }

    [Fact]
    public void KayitSirasindaBaglantiKopmasi_503()
    {
        var wrapped = new DbUpdateException("kayıt hatası", new NpgsqlException("bağlantı koptu"));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, GlobalExceptionHandler.Classify(wrapped).Status);
    }

    [Fact]
    public void ZamanAsimi_503()
    {
        Assert.Equal(StatusCodes.Status503ServiceUnavailable,
            GlobalExceptionHandler.Classify(new TimeoutException()).Status);
    }

    [Fact]
    public void BozukIstek_400()
    {
        Assert.Equal(StatusCodes.Status400BadRequest,
            GlobalExceptionHandler.Classify(new BadHttpRequestException("bozuk")).Status);
    }

    [Fact]
    public void BaskaHerHata_500()
    {
        var (status, title, _) = GlobalExceptionHandler.Classify(new NullReferenceException());

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("Beklenmeyen bir hata oluştu", title);
    }
}
