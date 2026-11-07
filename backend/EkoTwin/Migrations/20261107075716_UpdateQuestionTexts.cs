using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestionTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000001"),
                column: "Text",
                value: "İş süreçlerinizde (stok, satış, muhasebe) entegre bir ERP veya CRM yazılımı kullanıyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000002"),
                column: "Text",
                value: "Üretim veya operasyon verileriniz dijital ortamda anlık olarak toplanıyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000003"),
                column: "Text",
                value: "Siber güvenlik önlemleri (düzenli yedekleme, firewall vb.) aktif olarak uygulanıyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000004"),
                column: "Text",
                value: "Çalışanlarınızın dijital yetkinliklerini artırmak için düzenli eğitimler veriliyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000005"),
                column: "Text",
                value: "Tesisinizde enerji tüketimini anlık izleyen sayaç veya otomasyon sistemleri var mı?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000006"),
                column: "Text",
                value: "Aydınlatma ve ısıtma/soğutma sistemleriniz enerji verimli (LED, akıllı termostat vb.) teknolojiye sahip mi?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000007"),
                column: "Text",
                value: "Üretim süreçlerinizde atık ısı geri kazanımı veya enerji tasarrufu sağlayan önlemler alınıyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000008"),
                column: "Text",
                value: "Enerji verimliliğine yönelik periyodik etütler veya denetimler yapılıyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000009"),
                column: "Text",
                value: "Kapsam 1 (doğrudan) ve Kapsam 2 (dolaylı) emisyonlarınızı hesaplıyor ve takip ediyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000010"),
                column: "Text",
                value: "Ürünlerinizin karbon ayak izini (LCA - Yaşam Döngüsü Analizi) ölçümlediniz mi?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000011"),
                column: "Text",
                value: "Karbon emisyonlarını azaltmak için somut bir hedefiniz ve azaltım yol haritanız var mı?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000012"),
                column: "Text",
                value: "AB Yeşil Mutabakatı (CBAM) veya ulusal yeşil mutabakat kriterleri hakkında bilgi sahibi ve uyumlu musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000013"),
                column: "Text",
                value: "Tedarikçilerinizi seçerken sürdürülebilirlik ve çevre kriterlerini dikkate alıyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000014"),
                column: "Text",
                value: "Lojistik ve taşıma operasyonlarınızda karbon salınımını optimize edecek planlamalar yapılıyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000015"),
                column: "Text",
                value: "Hammadde ve malzeme tedarikinde yerel tedarikçilere öncelik veriliyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000016"),
                column: "Text",
                value: "Tedarik zincirinizde geri dönüştürülmüş veya çevre dostu malzeme kullanım oranı nedir?");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000001"),
                column: "Text",
                value: "İş süreçlerinizi (satış, stok, muhasebe vb.) entegre bir yazılım (ERP vb.) ile yönetiyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000002"),
                column: "Text",
                value: "Üretim/operasyon verileriniz sensörler veya otomasyon sistemleriyle dijital olarak toplanıyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000003"),
                column: "Text",
                value: "Verileriniz bulut ortamında güvenli şekilde saklanıyor ve düzenli olarak yedekleniyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000004"),
                column: "Text",
                value: "Karar alma süreçlerinizde veri analitiği veya yapay zekâ araçlarından yararlanıyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000005"),
                column: "Text",
                value: "Enerji tüketiminizi sayaç veya otomasyon sistemleriyle düzenli olarak ölçüp izliyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000006"),
                column: "Text",
                value: "Yüksek verimli ekipman (verimli motor, LED aydınlatma, inverter vb.) kullanıyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000007"),
                column: "Text",
                value: "Yenilenebilir enerji kaynaklarından (GES, yeşil enerji sertifikası vb.) yararlanıyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000008"),
                column: "Text",
                value: "Enerji verimliliği hedefleri belirleyip bunları takip ediyor musunuz (örn. ISO 50001)?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000009"),
                column: "Text",
                value: "Kapsam 1 ve Kapsam 2 sera gazı emisyonlarınızı hesaplıyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000010"),
                column: "Text",
                value: "Karbon azaltım hedefleriniz ve bu hedeflere yönelik bir eylem planınız var mı?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000011"),
                column: "Text",
                value: "AB Sınırda Karbon Düzenleme Mekanizması (SKDM/CBAM) gibi düzenlemelere hazırlık yapıyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000012"),
                column: "Text",
                value: "Atık yönetimi ve geri dönüşüm uygulamalarınızı kayıt altına alıyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000013"),
                column: "Text",
                value: "Tedarikçilerinizi çevresel ve sosyal kriterlere göre değerlendiriyor musunuz?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000014"),
                column: "Text",
                value: "Tedarik zinciri süreçleriniz (sipariş, sevkiyat, stok) dijital platformlar üzerinden izlenebiliyor mu?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000015"),
                column: "Text",
                value: "Lojistik süreçlerinizde karbon ayak izini azaltmaya yönelik uygulamalarınız var mı?");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000016"),
                column: "Text",
                value: "Ürünlerinizde kullanılan hammaddelerin kaynağı ve izlenebilirliği kayıt altında mı?");
        }
    }
}
