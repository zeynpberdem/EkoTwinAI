using EkoTwin.Models;
using Microsoft.EntityFrameworkCore;

namespace EkoTwin.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentDetail> AssessmentDetails => Set<AssessmentDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UUID birincil anahtarlar PostgreSQL tarafında uuidv7() ile üretilir (PostgreSQL 18+).
        // v7 zaman sıralı olduğu için index'i rastgele UUID (v4) gibi dağıtmaz.

        // ---------- Company ----------
        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(c => c.Id).HasDefaultValueSql("uuidv7()");

            // Enum'lar veritabanında metin olarak saklanır: "TextileAndApparel", "IronAndSteel" vb.
            entity.Property(c => c.Sector)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            // Sektöre göre filtreleme ve karşılaştırma için index
            entity.HasIndex(c => c.Sector);

            entity.Property(c => c.CbamProductGroup)
                  .HasConversion<string>()
                  .HasMaxLength(30);

            // Company (1) -> Assessment (N); firma silinirse değerlendirmeleri de silinir.
            entity.HasMany(c => c.Assessments)
                  .WithOne(a => a.Company)
                  .HasForeignKey(a => a.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Question ----------
        modelBuilder.Entity<Question>(entity =>
        {
            entity.Property(q => q.Id).HasDefaultValueSql("uuidv7()");

            // Enum veritabanında metin olarak saklanır: "DigitalInfrastructure" vb.
            entity.Property(q => q.Dimension)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.HasIndex(q => q.DisplayOrder).IsUnique();

            entity.HasData(QuestionSeed.Questions);
        });

        // ---------- Assessment ----------
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.Property(a => a.Id).HasDefaultValueSql("uuidv7()");

            // 0-100 arası, virgülden sonra 2 hane: numeric(5,2)
            entity.Property(a => a.TotalScore).HasPrecision(5, 2);
            entity.Property(a => a.DigitalInfrastructureScore).HasPrecision(5, 2);
            entity.Property(a => a.EnergyEfficiencyScore).HasPrecision(5, 2);
            entity.Property(a => a.CarbonManagementScore).HasPrecision(5, 2);
            entity.Property(a => a.SupplyChainScore).HasPrecision(5, 2);

            // Assessment (1) -> AssessmentDetail (N); değerlendirme silinirse yanıtları da silinir.
            entity.HasMany(a => a.AssessmentDetails)
                  .WithOne(d => d.Assessment)
                  .HasForeignKey(d => d.AssessmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- AssessmentDetail ----------
        modelBuilder.Entity<AssessmentDetail>(entity =>
        {
            entity.Property(d => d.Id).HasDefaultValueSql("uuidv7()");

            // Puan veritabanı seviyesinde de 1-5 ile sınırlandırılır.
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_AssessmentDetails_Score", "\"Score\" BETWEEN 1 AND 5"));

            // Bir değerlendirmede aynı soru iki kez yanıtlanamaz.
            entity.HasIndex(d => new { d.AssessmentId, d.QuestionId }).IsUnique();

            // Question (1) -> AssessmentDetail (N); yanıtı olan soru silinemez (geçmiş korunur).
            entity.HasOne(d => d.Question)
                  .WithMany(q => q.AssessmentDetails)
                  .HasForeignKey(d => d.QuestionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
