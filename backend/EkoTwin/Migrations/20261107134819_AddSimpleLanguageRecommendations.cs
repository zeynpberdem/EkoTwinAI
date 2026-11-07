using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class AddSimpleLanguageRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SimpleBenefit",
                table: "Recommendations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SimpleCbamNote",
                table: "Recommendations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimpleTitle",
                table: "Recommendations",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AnswerLabel",
                table: "AssessmentRecommendations",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimpleBenefit",
                table: "AssessmentRecommendations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SimpleCbamNote",
                table: "AssessmentRecommendations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimpleReasons",
                table: "AssessmentRecommendations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SimpleTitle",
                table: "AssessmentRecommendations",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000001"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Kayıtlar tek yerde durur, hata ve kayıp azalır.", null, "Muhasebe ve stoğu internetten çalışan bir programa taşıyın" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000002"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Makinelerin neden durduğunu görür, boşa geçen zamanı azaltırsınız.", null, "Önemli makinelerin çalışma ve duruş süresini kaydedin" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000003"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Bilgisayar bozulsa da kayıtlarınız kaybolmaz.", null, "Düzenli yedek alın, virüse karşı önlem kurun" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000004"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Programlar daha doğru ve hızlı kullanılır.", null, "Çalışanlarınıza bilgisayar ve program eğitimi verin" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000005"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "En çok elektrik yakan makineyi görür, tasarrufa oradan başlarsınız.", "Avrupalı alıcınız ürün başına ne kadar elektrik harcadığınızı soracak. Makine sayaçları bu bilgiyi verir.", "Makinelere elektrik harcamasını gösteren sayaç taktırın" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000006"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Az masrafla elektrik ve yakıt faturası düşer.", null, "Lambaları LED'e çevirin, ısıtmayı ve soğutmayı ayarlı hâle getirin" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000007"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Aynı üretimi daha az enerjiyle yaparsınız.", null, "Hava kaçaklarını ve enerji israfını giderin" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000008"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Nerede para kaçırdığınızı ölçülmüş bir raporla görürsünüz.", null, "Tesisinizi bir enerji uzmanına inceletin" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000009"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Müşteri veya banka sorduğunda cevabınız hazır olur. EkoTwin'in hesaplayıcısıyla başlayabilirsiniz.", "Avrupalı alıcınız ürünlerinizin karbon bilgisini isteyecek. Veremezseniz yüksek bir değer varsayılır ve fiyatta geri kalırsınız.", "Firmanızın yıllık karbonunu hesaplayın" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000010"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Müşteri ürün başına karbon sorduğunda işi kaçırmazsınız.", "Avrupa'ya sattığınız ürünlerin karbon bilgisi bu hesaba dayanır.", "Sattığınız ürünlerin karbonunu hesaplatın" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000011"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Neyi ne zamana kadar yapacağınız belli olur; ilerlemeyi EkoTwin'de izlersiniz.", "Ürününüzün karbonu azaldıkça Avrupalı alıcının ödediği karbon bedeli düşer, siz de fiyatta öne geçersiniz.", "Karbonu azaltmak için hedef koyun ve plan yazın" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000012"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Avrupa'ya satışlarınızı riske atmadan önceden hazırlanırsınız.", "Bu kural 2026 başında yürürlüğe girdi. İlk yıllık beyan Eylül 2027'de. Kurallar değişebilir, takipte kalın.", "Avrupa'nın karbon kurallarının sizi nasıl etkilediğini öğrenin" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000013"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Müşterileriniz sorduğunda tedarikçilerinizin bilgisi hazır olur.", null, "Malzeme aldığınız firmalara çevre konusunda birkaç basit soru sorun" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000014"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Yakıt ve taşıma masrafı azalır.", null, "Sevkiyatları birleştirin, araçları dolu gönderin" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000015"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Taşıma süresi ve masrafı azalır, yerel ekonomi güçlenir.", null, "Yakındaki firmalardan daha çok malzeme alın" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000016"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Hammadde masrafı düşebilir, çevreci ürün sunarsınız.", "Demir-çelik ve alüminyumda hurda kullanma oranı ürünün karbonunu doğrudan etkiler.", "Geri dönüştürülmüş malzeme kullanımını artırın" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000017"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Muhasebe, stok ve sipariş birbirine bağlanır, iş takibi kolaylaşır.", null, "Tüm işleri tek programda toplamak için plan yapın" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000018"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Tasarruf tek seferlik kalmaz, her yıl devam eder.", null, "Enerjiyi yönetmek için bir sorumlu ve bir plan belirleyin" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000019"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Avrupa kuralları ve müşteri talepleri sizi hazırlıksız yakalamaz.", "Avrupa'ya bu kurala tabi ürün sattığınız için önce ürün başına karbon bilgisini çıkarmaya odaklanın.", "Karbon konusunda hemen bir yol haritası çıkarın" });

            migrationBuilder.UpdateData(
                table: "Recommendations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8001-000000000020"),
                columns: new[] { "SimpleBenefit", "SimpleCbamNote", "SimpleTitle" },
                values: new object[] { "Tüm alımlarınızda aynı kurala göre karar verirsiniz.", null, "Malzeme alırken çevreyi de gözeten basit kurallar yazın" });

            // Elle eklendi: bu migration'dan önce üretilmiş raporların öneri kayıtlarına sade başlık ve
            // kazanım metni şablondan kopyalanır. Teknik metinler ve CBAM notları olduğu gibi kalır.
            migrationBuilder.Sql(
                """
                UPDATE "AssessmentRecommendations" AS ar
                SET "SimpleTitle" = r."SimpleTitle",
                    "SimpleBenefit" = r."SimpleBenefit"
                FROM "Recommendations" AS r
                WHERE ar."RecommendationId" = r."Id" AND ar."SimpleTitle" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SimpleBenefit",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "SimpleCbamNote",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "SimpleTitle",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "AnswerLabel",
                table: "AssessmentRecommendations");

            migrationBuilder.DropColumn(
                name: "SimpleBenefit",
                table: "AssessmentRecommendations");

            migrationBuilder.DropColumn(
                name: "SimpleCbamNote",
                table: "AssessmentRecommendations");

            migrationBuilder.DropColumn(
                name: "SimpleReasons",
                table: "AssessmentRecommendations");

            migrationBuilder.DropColumn(
                name: "SimpleTitle",
                table: "AssessmentRecommendations");
        }
    }
}
