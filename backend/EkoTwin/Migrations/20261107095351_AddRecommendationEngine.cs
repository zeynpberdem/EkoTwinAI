using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    TriggerType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Dimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CostLevel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ImpactLevel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ImpactType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DurationMinMonths = table.Column<int>(type: "integer", nullable: false),
                    DurationMaxMonths = table.Column<int>(type: "integer", nullable: false),
                    CbamRelevant = table.Column<bool>(type: "boolean", nullable: false),
                    CbamNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                    table.CheckConstraint("CK_Recommendations_Duration", "\"DurationMinMonths\" >= 0 AND \"DurationMinMonths\" <= \"DurationMaxMonths\"");
                    table.CheckConstraint("CK_Recommendations_Trigger", "(\"TriggerType\" = 'Question' AND \"QuestionId\" IS NOT NULL AND \"Dimension\" IS NULL) OR (\"TriggerType\" = 'Dimension' AND \"Dimension\" IS NOT NULL AND \"QuestionId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_Recommendations_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggerType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Dimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    IsTopPriority = table.Column<bool>(type: "boolean", nullable: false),
                    UrgencyScore = table.Column<int>(type: "integer", nullable: false),
                    QuestionNumber = table.Column<int>(type: "integer", nullable: true),
                    AnswerScore = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CbamNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CostLevel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ImpactLevel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ImpactType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DurationMinMonths = table.Column<int>(type: "integer", nullable: false),
                    DurationMaxMonths = table.Column<int>(type: "integer", nullable: false),
                    Reasons = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentRecommendations_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentRecommendations_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Recommendations",
                columns: new[] { "Id", "CbamNote", "CbamRelevant", "CostLevel", "Dimension", "DurationMaxMonths", "DurationMinMonths", "ImpactLevel", "ImpactType", "IsActive", "Priority", "QuestionId", "Rationale", "Source", "Title", "TriggerType" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-7000-8001-000000000001"), null, false, "Low", null, 2, 1, "High", "QuickWin", true, "High", new Guid("00000000-0000-7000-8000-000000000001"), "Satış, stok ve faturaların kâğıt veya dağınık Excel dosyaları yerine tek bir sistemde tutulması veri giriş hatalarını ve stok kayıplarını azaltır, güncel duruma her an erişim sağlar. İleride entegre ERP'ye geçişin ilk adımıdır.", null, "Bulut tabanlı ön muhasebe ve stok yazılımına geçiş", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000002"), null, false, "Medium", null, 4, 2, "High", "LongTermEfficiency", true, "Medium", new Guid("00000000-0000-7000-8000-000000000002"), "Makine duruşları, çevrim süreleri ve firelerin sensör veya basit veri toplama terminalleriyle anlık izlenmesi üretimdeki darboğazları görünür kılar. Tüm tesis yerine en kritik 1-2 makineyle başlanabilir.", null, "Kritik makinelerde dijital üretim takibi", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000003"), null, false, "Low", null, 1, 1, "High", "QuickWin", true, "High", new Guid("00000000-0000-7000-8000-000000000003"), "Düzenli ve en az bir kopyası farklı ortamda tutulan yedekleme, güncel güvenlik duvarı ve çok faktörlü kimlik doğrulama; fidye yazılımı gibi saldırılarda veri ve üretim kaybını önler. Kişisel veri işleyen firmalar için KVKK yükümlülükleri açısından da gereklidir.", null, "Temel siber güvenlik ve yedekleme paketi", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000004"), null, false, "Low", null, 3, 1, "Medium", "LongTermEfficiency", true, "Medium", new Guid("00000000-0000-7000-8000-000000000004"), "Yazılım ve otomasyon yatırımlarının verim getirmesi, çalışanların bu araçları benimsemesine bağlıdır. Kısa, uygulamalı eğitimlerle başlanabilir; kamu ve meslek odası destekli eğitim programları araştırılmalıdır.", null, "Çalışanlar için dijital yetkinlik eğitim programı", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000005"), "CBAM beyanında ürün başına elektrik tüketimi gerekir; hat veya makine bazında ölçüm bu veriyi sağlar.", true, "Low", null, 2, 1, "High", "QuickWin", true, "High", new Guid("00000000-0000-7000-8000-000000000005"), "Boşta çalışan makineleri, kaçak akımları ve pik saat tüketimini görünür kılar; enerji faturalarında ölçülebilir düşüş sağlar. Diğer tüm enerji verimliliği adımları için gereken verinin temelidir.", null, "IoT tabanlı anlık enerji izleme ve sayaç entegrasyonu", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000006"), null, false, "Low", null, 2, 1, "Medium", "QuickWin", true, "Medium", new Guid("00000000-0000-7000-8000-000000000006"), "LED aydınlatma, hareket sensörleri ve akıllı termostatlar düşük riskli ve hızlı uygulanabilen yatırımlardır; enerji tüketimini üretimi etkilemeden azaltır.", null, "Aydınlatma ve iklimlendirmede verimli ekipmana geçiş", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000007"), null, false, "Medium", null, 6, 3, "High", "LongTermEfficiency", true, "Medium", new Guid("00000000-0000-7000-8000-000000000007"), "Basınçlı hava kaçaklarının giderilmesi ile yüksek verimli motor ve hız kontrol cihazı (VFD) kullanımı düşük maliyetli başlangıçlardır. Döküm, fırın ve kurutma süreçlerinde atık ısının ön ısıtmada kullanılması ileri aşama bir adımdır.", null, "Basınçlı hava, motor ve atık ısı verimlilik iyileştirmeleri", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000008"), null, false, "Medium", null, 3, 1, "Medium", "LongTermEfficiency", true, "Medium", new Guid("00000000-0000-7000-8000-000000000008"), "Yetkili bir enerji verimliliği danışmanlık firmasına yaptırılan etüt, tasarruf fırsatlarını yatırım geri dönüş süreleriyle birlikte önceliklendirir. Verimlilik projelerine yönelik destek başvurularında da temel belge olarak kullanılır.", null, "Enerji verimliliği etüdü yaptırılması", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000009"), "AB'li müşterileriniz CBAM beyanı için ürünlerinizin gömülü emisyon verisini isteyecektir. Veri sunulamazsa yüksek varsayılan değerler uygulanır ve rekabet gücünüz düşer.", true, "Low", null, 3, 1, "High", "StrategicCompliance", true, "High", new Guid("00000000-0000-7000-8000-000000000009"), "Yakıt, doğalgaz ve elektrik faturalarından yıllık sera gazı emisyonunuzu hesaplamak karbon yönetiminin ilk adımıdır. Resmi emisyon katsayıları ve hesaplama araçlarıyla düşük maliyetle yapılabilir.", null, "Kurumsal karbon ayak izi hesabı (Kapsam 1 ve 2)", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000010"), "CBAM kapsamındaki ürünlerde ürün başına gömülü emisyon hesabı bu çalışmanın temelini oluşturur.", true, "Medium", null, 6, 3, "Medium", "StrategicCompliance", true, "Medium", new Guid("00000000-0000-7000-8000-000000000010"), "En çok satan ürününüzün yaşam döngüsü boyunca oluşturduğu emisyonu ölçmek; yeşil ürün beyanı, ihale ve müşteri taleplerine cevap verebilmenizi sağlar. Uzmanlık gerektirir; kurumsal hesap yapıldıktan sonra tek bir üründen başlanması önerilir.", null, "Ürün bazlı karbon ayak izi (LCA) çalışması", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000011"), "Ürünlerinizin gömülü emisyonu azaldıkça AB'li müşterinizin CBAM maliyeti, dolayısıyla fiyat rekabetiniz doğrudan iyileşir.", true, "Low", null, 3, 1, "Medium", "StrategicCompliance", true, "Medium", new Guid("00000000-0000-7000-8000-000000000011"), "Ölçülen emisyonlara dayanan, yıllara yayılmış somut hedefler (enerji verimliliği, yenilenebilir elektrik, yakıt değişimi gibi) yatırımları önceliklendirir ve müşterilere taahhüt olarak sunulabilir.", null, "Karbon azaltım hedefi ve yol haritası belirlenmesi", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000012"), "CBAM kesin dönemi 1 Ocak 2026'da başladı; 2026 ithalatları için ilk yıllık beyan 30 Eylül 2027'dedir. Mevzuat değişebileceği için güncel metin takip edilmelidir.", true, "Low", null, 2, 1, "High", "StrategicCompliance", true, "High", new Guid("00000000-0000-7000-8000-000000000012"), "Ürünlerinizin AB düzenlemelerinden nasıl etkilendiğini, müşterilerinizin hangi veriyi hangi formatta isteyeceğini ve Türkiye'deki iklim mevzuatındaki gelişmeleri düzenli takip etmek; sürpriz maliyet ve müşteri kaybı riskini azaltır.", null, "AB Yeşil Mutabakatı ve SKDM (CBAM) uyum değerlendirmesi", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000013"), null, false, "Low", null, 4, 2, "Medium", "LongTermEfficiency", true, "Medium", new Guid("00000000-0000-7000-8000-000000000013"), "Tedarikçi seçiminde fiyat ve kalitenin yanına çevre ve sürdürülebilirlik kriterlerinin eklenmesi, tedarik zincirinden kaynaklanan dolaylı emisyonları (Kapsam 3) düşürür ve yeşil kriter arayan müşteri ve ihalelere erişimi kolaylaştırır.", null, "Yeşil tedarikçi değerlendirme kriterlerinin oluşturulması", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000014"), null, false, "Low", null, 3, 1, "Medium", "QuickWin", true, "Medium", new Guid("00000000-0000-7000-8000-000000000014"), "Sevkiyatların birleştirilmesi, boş dönüşlerin azaltılması ve rota planlama araçlarının kullanılması yakıt maliyetini ve taşımacılık kaynaklı emisyonları birlikte düşürür.", null, "Lojistik planlama ve rota optimizasyonu", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000015"), null, false, "Low", null, 6, 3, "Low", "LongTermEfficiency", true, "Low", new Guid("00000000-0000-7000-8000-000000000015"), "Aynı il veya OSB'deki tedarikçilere öncelik vermek taşıma mesafesini ve teslim sürelerini kısaltır, tedarik kesintisi riskini azaltır. Karar verilirken fiyat ve kaliteyle birlikte değerlendirilmelidir.", null, "Yerel tedarikçi ağının geliştirilmesi", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000016"), "Demir-çelik ve alüminyumda hurda (ikincil hammadde) oranı ürünün gömülü emisyonunu doğrudan etkiler.", true, "Medium", null, 6, 3, "Medium", "LongTermEfficiency", true, "Medium", new Guid("00000000-0000-7000-8000-000000000016"), "İkincil hammadde ve geri dönüştürülmüş ambalaj kullanımı hammadde maliyetini ve ürünün karbon ayak izini düşürebilir. Kalite gereksinimleri korunarak kademeli olarak artırılmalıdır.", null, "Geri dönüştürülmüş ve çevre dostu malzeme kullanımının artırılması", "Question" },
                    { new Guid("00000000-0000-7000-8001-000000000017"), null, false, "Medium", "DigitalInfrastructure", 6, 3, "High", "LongTermEfficiency", true, "High", null, "Dijital altyapınız temel seviyede. Süreçleri aşamalı olarak tek bir entegre sistemde birleştiren bir plan (önce muhasebe ve stok, ardından üretim ve müşteri yönetimi) hazırlanmalı; kamu dijitalleşme destekleri araştırılmalıdır.", null, "Dijital dönüşüm yol haritası ve entegre ERP/CRM planı", "Dimension" },
                    { new Guid("00000000-0000-7000-8001-000000000018"), null, false, "Medium", "EnergyEfficiency", 6, 3, "High", "LongTermEfficiency", true, "High", null, "Enerji verimliliğiniz düşük seviyede. Bir enerji sorumlusu atanması, tüketimin düzenli izlenmesi ve yıllık iyileştirme hedeflerinden oluşan bir yönetim sistemi, tek seferlik önlemlerin kalıcı tasarrufa dönüşmesini sağlar.", null, "Enerji yönetim sistemi kurulması (ISO 50001 yaklaşımı)", "Dimension" },
                    { new Guid("00000000-0000-7000-8001-000000000019"), "AB'ye CBAM kapsamında ürün ihraç ettiğiniz için bu yol haritası öncelikli olarak ürün başına gömülü emisyon verisini üretmeye odaklanmalıdır.", true, "Medium", "CarbonManagement", 3, 1, "High", "StrategicCompliance", true, "High", null, "Karbon yönetiminde temel adımlar eksik. Önce kurumsal emisyon hesabı, ardından azaltım hedefleri ve ürün bazlı hesap şeklinde aşamalı bir plan hazırlanmalı; iklim düzenlemeleri ve müşteri talepleri bu alanı giderek zorunlu hale getirmektedir.", null, "Karbon yönetimi için acil yol haritası", "Dimension" },
                    { new Guid("00000000-0000-7000-8001-000000000020"), null, false, "Low", "SupplyChain", 6, 3, "Medium", "LongTermEfficiency", true, "Medium", null, "Tedarik zinciri boyutunuz zayıf. Tedarikçi seçimi, lojistik planlama ve malzeme tercihlerini kapsayan yazılı bir sürdürülebilir tedarik politikası, müşterilerin artan tedarik zinciri denetimlerine hazırlık sağlar.", null, "Sürdürülebilir tedarik zinciri politikası", "Dimension" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentRecommendations_AssessmentId_RecommendationId",
                table: "AssessmentRecommendations",
                columns: new[] { "AssessmentId", "RecommendationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentRecommendations_RecommendationId",
                table: "AssessmentRecommendations",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_QuestionId",
                table: "Recommendations",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentRecommendations");

            migrationBuilder.DropTable(
                name: "Recommendations");
        }
    }
}
