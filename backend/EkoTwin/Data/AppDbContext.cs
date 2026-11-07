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
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<AssessmentRecommendation> AssessmentRecommendations => Set<AssessmentRecommendation>();
    public DbSet<EmissionFactor> EmissionFactors => Set<EmissionFactor>();
    public DbSet<CarbonFootprint> CarbonFootprints => Set<CarbonFootprint>();
    public DbSet<CarbonFootprintItem> CarbonFootprintItems => Set<CarbonFootprintItem>();

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

        // ---------- Recommendation (öneri kütüphanesi) ----------
        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.Property(r => r.Id).HasDefaultValueSql("uuidv7()");

            entity.Property(r => r.TriggerType).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.Dimension).HasConversion<string>().HasMaxLength(50);
            entity.Property(r => r.Priority).HasConversion<string>().HasMaxLength(10);
            entity.Property(r => r.CostLevel).HasConversion<string>().HasMaxLength(10);
            entity.Property(r => r.ImpactLevel).HasConversion<string>().HasMaxLength(10);
            entity.Property(r => r.ImpactType).HasConversion<string>().HasMaxLength(30);

            entity.ToTable(t =>
            {
                // Soru bazlıysa yalnızca QuestionId, boyut bazlıysa yalnızca Dimension dolu olmalı.
                t.HasCheckConstraint("CK_Recommendations_Trigger",
                    "(\"TriggerType\" = 'Question' AND \"QuestionId\" IS NOT NULL AND \"Dimension\" IS NULL) OR " +
                    "(\"TriggerType\" = 'Dimension' AND \"Dimension\" IS NOT NULL AND \"QuestionId\" IS NULL)");
                t.HasCheckConstraint("CK_Recommendations_Duration",
                    "\"DurationMinMonths\" >= 0 AND \"DurationMinMonths\" <= \"DurationMaxMonths\"");
            });

            // Öneri şablonu olan soru silinemez.
            entity.HasOne(r => r.Question)
                  .WithMany()
                  .HasForeignKey(r => r.QuestionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasData(RecommendationSeed.Recommendations);
        });

        // ---------- AssessmentRecommendation (rapordaki önerilerin kaydı) ----------
        modelBuilder.Entity<AssessmentRecommendation>(entity =>
        {
            entity.Property(r => r.Id).HasDefaultValueSql("uuidv7()");

            entity.Property(r => r.TriggerType).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.Dimension).HasConversion<string>().HasMaxLength(50);
            entity.Property(r => r.Priority).HasConversion<string>().HasMaxLength(10);
            entity.Property(r => r.CostLevel).HasConversion<string>().HasMaxLength(10);
            entity.Property(r => r.ImpactLevel).HasConversion<string>().HasMaxLength(10);
            entity.Property(r => r.ImpactType).HasConversion<string>().HasMaxLength(30);

            // Assessment (1) -> AssessmentRecommendation (N); değerlendirme silinirse önerileri de silinir.
            entity.HasOne(r => r.Assessment)
                  .WithMany(a => a.Recommendations)
                  .HasForeignKey(r => r.AssessmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Şablon silinirse kayıt korunur, yalnızca bağlantı boşalır.
            entity.HasOne<Recommendation>()
                  .WithMany()
                  .HasForeignKey(r => r.RecommendationId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Bir değerlendirmede aynı şablon bir kez üretilir.
            entity.HasIndex(r => new { r.AssessmentId, r.RecommendationId }).IsUnique();
        });

        // ---------- EmissionFactor (emisyon faktörleri) ----------
        modelBuilder.Entity<EmissionFactor>(entity =>
        {
            entity.Property(f => f.Id).HasDefaultValueSql("uuidv7()");
            entity.Property(f => f.Source).HasConversion<string>().HasMaxLength(30);
            entity.Property(f => f.Scope).HasConversion<string>().HasMaxLength(10);
            entity.Property(f => f.KgCo2ePerUnit).HasPrecision(12, 4);

            // Her kaynak için tek faktör
            entity.HasIndex(f => f.Source).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("CK_EmissionFactors_Positive", "\"KgCo2ePerUnit\" > 0"));

            entity.HasData(EmissionFactorSeed.Factors);
        });

        // ---------- CarbonFootprint (yıllık karbon ayak izi) ----------
        modelBuilder.Entity<CarbonFootprint>(entity =>
        {
            entity.Property(c => c.Id).HasDefaultValueSql("uuidv7()");

            entity.Property(c => c.Scope1Tonnes).HasPrecision(14, 3);
            entity.Property(c => c.Scope2Tonnes).HasPrecision(14, 3);
            entity.Property(c => c.TotalTonnes).HasPrecision(14, 3);
            entity.Property(c => c.TonnesPerEmployee).HasPrecision(14, 3);

            // Firma başına yıl başına tek kayıt
            entity.HasIndex(c => new { c.CompanyId, c.Year }).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("CK_CarbonFootprints_Year", "\"Year\" BETWEEN 2000 AND 2100"));

            // Company (1) -> CarbonFootprint (N); firma silinirse hesapları da silinir.
            entity.HasOne(c => c.Company)
                  .WithMany(co => co.CarbonFootprints)
                  .HasForeignKey(c => c.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);

            // CarbonFootprint (1) -> Item (N); hesap silinirse satırları da silinir.
            entity.HasMany(c => c.Items)
                  .WithOne(i => i.CarbonFootprint)
                  .HasForeignKey(i => i.CarbonFootprintId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- CarbonFootprintItem ----------
        modelBuilder.Entity<CarbonFootprintItem>(entity =>
        {
            entity.Property(i => i.Id).HasDefaultValueSql("uuidv7()");
            entity.Property(i => i.Source).HasConversion<string>().HasMaxLength(30);
            entity.Property(i => i.Scope).HasConversion<string>().HasMaxLength(10);
            entity.Property(i => i.Quantity).HasPrecision(14, 3);
            entity.Property(i => i.FactorKgCo2ePerUnit).HasPrecision(12, 4);
            entity.Property(i => i.EmissionTonnes).HasPrecision(14, 3);

            // Bir hesapta aynı kaynak bir kez yer alır.
            entity.HasIndex(i => new { i.CarbonFootprintId, i.Source }).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint("CK_CarbonFootprintItems_Quantity", "\"Quantity\" >= 0"));
        });
    }
}
