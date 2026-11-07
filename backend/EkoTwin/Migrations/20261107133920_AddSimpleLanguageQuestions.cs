using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EkoTwin.Migrations
{
    /// <inheritdoc />
    public partial class AddSimpleLanguageQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HelpText",
                table: "Questions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimpleText",
                table: "Questions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AnswerOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerOptions", x => x.Id);
                    table.CheckConstraint("CK_AnswerOptions_Score", "\"Score\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_AnswerOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AnswerOptions",
                columns: new[] { "Id", "Label", "QuestionId", "Score" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-7000-8003-000000000011"), "Hayır, defter veya kâğıtla tutuyoruz", new Guid("00000000-0000-7000-8000-000000000001"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000012"), "Excel gibi ayrı dosyalarla tutuyoruz", new Guid("00000000-0000-7000-8000-000000000001"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000013"), "Bazı işler için program kullanıyoruz", new Guid("00000000-0000-7000-8000-000000000001"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000014"), "Çoğu işimiz tek programda", new Guid("00000000-0000-7000-8000-000000000001"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000015"), "Her şey tek programda ve birbirine bağlı", new Guid("00000000-0000-7000-8000-000000000001"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000021"), "Hayır, kayıt tutmuyoruz", new Guid("00000000-0000-7000-8000-000000000002"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000022"), "Kâğıda yazıyoruz", new Guid("00000000-0000-7000-8000-000000000002"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000023"), "Gün sonunda bilgisayara giriyoruz", new Guid("00000000-0000-7000-8000-000000000002"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000024"), "Bazı makinelerden bilgi kendiliğinden geliyor", new Guid("00000000-0000-7000-8000-000000000002"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000025"), "Tüm üretimi anlık olarak ekrandan izliyoruz", new Guid("00000000-0000-7000-8000-000000000002"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000031"), "Hiçbir önlem yok", new Guid("00000000-0000-7000-8000-000000000003"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000032"), "Sadece virüs programı var", new Guid("00000000-0000-7000-8000-000000000003"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000033"), "Ara sıra yedek alıyoruz", new Guid("00000000-0000-7000-8000-000000000003"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000034"), "Düzenli yedek alıyoruz, şifre kurallarımız var", new Guid("00000000-0000-7000-8000-000000000003"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000035"), "Düzenli yedek ve koruma var, bundan sorumlu bir kişi var", new Guid("00000000-0000-7000-8000-000000000003"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000041"), "Hiç vermedik", new Guid("00000000-0000-7000-8000-000000000004"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000042"), "Gerektiğinde birbirlerinden öğreniyorlar", new Guid("00000000-0000-7000-8000-000000000004"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000043"), "Yeni program gelince eğitim veriyoruz", new Guid("00000000-0000-7000-8000-000000000004"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000044"), "Yılda bir-iki kez eğitim yapıyoruz", new Guid("00000000-0000-7000-8000-000000000004"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000045"), "Düzenli bir eğitim planımız var", new Guid("00000000-0000-7000-8000-000000000004"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000051"), "Hayır, sadece faturayı ödüyoruz", new Guid("00000000-0000-7000-8000-000000000005"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000052"), "Faturaları aydan aya karşılaştırıyoruz", new Guid("00000000-0000-7000-8000-000000000005"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000053"), "Bazı bölümlerde ayrı sayaç var", new Guid("00000000-0000-7000-8000-000000000005"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000054"), "Önemli makinelerde sayaç var, düzenli bakıyoruz", new Guid("00000000-0000-7000-8000-000000000005"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000055"), "Anlık izliyoruz ve tasarruf hedefi koyuyoruz", new Guid("00000000-0000-7000-8000-000000000005"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000061"), "Hayır, hepsi eski sistem", new Guid("00000000-0000-7000-8000-000000000006"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000062"), "Birkaç lambayı değiştirdik", new Guid("00000000-0000-7000-8000-000000000006"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000063"), "Yaklaşık yarısı tasarruflu", new Guid("00000000-0000-7000-8000-000000000006"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000064"), "Büyük kısmı tasarruflu", new Guid("00000000-0000-7000-8000-000000000006"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000065"), "Tamamı tasarruflu ve kendiliğinden ayarlanıyor", new Guid("00000000-0000-7000-8000-000000000006"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000071"), "Hayır, bu konuya hiç bakmadık", new Guid("00000000-0000-7000-8000-000000000007"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000072"), "Boşta çalışan makineleri kapatıyoruz", new Guid("00000000-0000-7000-8000-000000000007"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000073"), "Kaçakları ve arızaları hemen onarıyoruz", new Guid("00000000-0000-7000-8000-000000000007"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000074"), "Tasarruflu motor veya kompresör aldık", new Guid("00000000-0000-7000-8000-000000000007"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000075"), "Bunlara ek olarak atık ısıyı da geri kazanıyoruz", new Guid("00000000-0000-7000-8000-000000000007"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000081"), "Hayır, hiç düşünmedik", new Guid("00000000-0000-7000-8000-000000000008"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000082"), "Düşünüyoruz ama yaptırmadık", new Guid("00000000-0000-7000-8000-000000000008"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000083"), "Bir kez yaptırdık", new Guid("00000000-0000-7000-8000-000000000008"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000084"), "Yaptırdık ve önerileri uyguladık", new Guid("00000000-0000-7000-8000-000000000008"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000085"), "Birkaç yılda bir düzenli yaptırıyoruz", new Guid("00000000-0000-7000-8000-000000000008"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000091"), "Hayır, hiç hesaplamadık", new Guid("00000000-0000-7000-8000-000000000009"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000092"), "Ne olduğunu biliyoruz ama hesaplamadık", new Guid("00000000-0000-7000-8000-000000000009"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000093"), "Bir kez kabaca hesapladık", new Guid("00000000-0000-7000-8000-000000000009"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000094"), "Her yıl hesaplıyoruz", new Guid("00000000-0000-7000-8000-000000000009"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000095"), "Her yıl hesaplıyor ve uzmana doğrulatıyoruz", new Guid("00000000-0000-7000-8000-000000000009"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000101"), "Hayır", new Guid("00000000-0000-7000-8000-000000000010"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000102"), "Müşterimiz sordu ama cevap veremedik", new Guid("00000000-0000-7000-8000-000000000010"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000103"), "Bir ürün için kabaca hesapladık", new Guid("00000000-0000-7000-8000-000000000010"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000104"), "Ana ürünlerimiz için hesapladık", new Guid("00000000-0000-7000-8000-000000000010"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000105"), "Tüm ürünlerimiz için hesaplı ve belgeli", new Guid("00000000-0000-7000-8000-000000000010"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000111"), "Hayır", new Guid("00000000-0000-7000-8000-000000000011"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000112"), "Düşünüyoruz ama yazılı değil", new Guid("00000000-0000-7000-8000-000000000011"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000113"), "Hedefimiz var, planımız yok", new Guid("00000000-0000-7000-8000-000000000011"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000114"), "Hedefimiz de planımız da var", new Guid("00000000-0000-7000-8000-000000000011"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000115"), "Planı uyguluyoruz, her yıl kontrol ediyoruz", new Guid("00000000-0000-7000-8000-000000000011"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000121"), "Bu kuralları hiç duymadık", new Guid("00000000-0000-7000-8000-000000000012"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000122"), "Duyduk ama ne yapacağımızı bilmiyoruz", new Guid("00000000-0000-7000-8000-000000000012"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000123"), "Bilgi topluyoruz", new Guid("00000000-0000-7000-8000-000000000012"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000124"), "Hazırlanıyoruz, müşterimize bilgi verebiliyoruz", new Guid("00000000-0000-7000-8000-000000000012"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000125"), "Tamamen hazırız", new Guid("00000000-0000-7000-8000-000000000012"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000131"), "Hayır, sadece fiyata bakıyoruz", new Guid("00000000-0000-7000-8000-000000000013"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000132"), "Bazen aklımıza geliyor", new Guid("00000000-0000-7000-8000-000000000013"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000133"), "Önemli tedarikçilerimize soruyoruz", new Guid("00000000-0000-7000-8000-000000000013"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000134"), "Yazılı kurallarımız var", new Guid("00000000-0000-7000-8000-000000000013"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000135"), "Yazılı kurallarımız var ve düzenli kontrol ediyoruz", new Guid("00000000-0000-7000-8000-000000000013"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000141"), "Hayır, plan yapmıyoruz", new Guid("00000000-0000-7000-8000-000000000014"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000142"), "Bazen sevkiyatları birleştiriyoruz", new Guid("00000000-0000-7000-8000-000000000014"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000143"), "Sevkiyatları önceden planlıyoruz", new Guid("00000000-0000-7000-8000-000000000014"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000144"), "Araç doluluğuna ve güzergâha bakıyoruz", new Guid("00000000-0000-7000-8000-000000000014"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000145"), "Bir programla planlıyor ve yakıtı takip ediyoruz", new Guid("00000000-0000-7000-8000-000000000014"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000151"), "Hayır, çoğu uzaktan veya yurt dışından", new Guid("00000000-0000-7000-8000-000000000015"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000152"), "Az bir kısmı yakından", new Guid("00000000-0000-7000-8000-000000000015"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000153"), "Yarı yarıya", new Guid("00000000-0000-7000-8000-000000000015"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000154"), "Çoğunu yakından alıyoruz", new Guid("00000000-0000-7000-8000-000000000015"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000155"), "Yakından alınabilecek her şeyi yakından alıyoruz", new Guid("00000000-0000-7000-8000-000000000015"), 5 },
                    { new Guid("00000000-0000-7000-8003-000000000161"), "Hayır", new Guid("00000000-0000-7000-8000-000000000016"), 1 },
                    { new Guid("00000000-0000-7000-8003-000000000162"), "Çok az", new Guid("00000000-0000-7000-8000-000000000016"), 2 },
                    { new Guid("00000000-0000-7000-8003-000000000163"), "Bazı ürünlerimizde", new Guid("00000000-0000-7000-8000-000000000016"), 3 },
                    { new Guid("00000000-0000-7000-8003-000000000164"), "Ürünlerimizin çoğunda", new Guid("00000000-0000-7000-8000-000000000016"), 4 },
                    { new Guid("00000000-0000-7000-8003-000000000165"), "Mümkün olan her yerde", new Guid("00000000-0000-7000-8000-000000000016"), 5 }
                });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000001"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Defter ve dağınık dosyalar yerine, işlerinizi tek yerde tutan bir program (ön muhasebe programı veya ERP).", "Muhasebe, stok ve satışı bir bilgisayar programında mı takip ediyorsunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000002"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Üretim bilgilerinin kâğıtta değil, bilgisayarda ve zamanında tutulması.", "Ne kadar ürettiğinizi ve makinelerin ne zaman durduğunu kayıt altına alıyor musunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000003"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Düzenli yedek almak, virüs programı ve güçlü şifre kullanmak (siber güvenlik).", "Bilgisayarlarınızı ve kayıtlarınızı virüse ve veri kaybına karşı koruyor musunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000004"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { null, "Çalışanlarınıza bilgisayar ve program kullanımı konusunda eğitim veriyor musunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000005"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Bölüm veya makine bazında ayrı sayaç, harcamayı anlık gösteren ekran.", "Ne kadar elektrik ve yakıt harcadığınızı faturadan başka bir yolla takip ediyor musunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000006"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "LED lamba, hareket sensörü, ayarlı termostat, yalıtım gibi.", "Aydınlatma, ısıtma ve soğutmada tasarruflu ürünler kullanıyor musunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000007"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Basınçlı hava kaçaklarını gidermek, boşta çalışan makineleri kapatmak, fırın ve ocak ısısını tekrar kullanmak.", "Üretimde enerji israfını azaltmak için önlem alıyor musunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000008"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Enerji verimliliği etüdü: uzman nerede israf olduğunu ölçer ve rapor verir.", "Enerji tasarrufu için tesisinizi bir uzmana inceletiniz mi?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000009"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Yaktığınız yakıttan ve kullandığınız elektrikten çıkan karbonun hesabı (karbon ayak izi). EkoTwin'in karbon hesaplayıcısıyla da yapabilirsiniz.", "Firmanızın bir yılda ne kadar karbon saldığını hesapladınız mı?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000010"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Bir ürünün hammaddesinden müşteriye ulaşana kadar çıkan karbon (ürün karbon ayak izi). Avrupalı müşteriler bunu giderek daha çok soruyor.", "Sattığınız ürünlerin karbonunu ayrı ayrı hesapladınız mı?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000011"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Örneğin \"2030'a kadar yüzde 20 azaltacağız\" demek ve bunun için atılacak adımları yazmak.", "Karbonunuzu azaltmak için bir hedefiniz ve planınız var mı?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000012"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "AB'nin Sınırda Karbon Düzenlemesi (SKDM / CBAM). Demir-çelik, alüminyum, çimento gibi ürünlerde Avrupalı alıcı ürünün karbon bilgisini istiyor. Avrupa'ya satışınız yoksa bile müşterileriniz ileride sorabilir.", "Avrupa'ya mal satarken istenen karbon kurallarına ne kadar hazırsınız?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000013"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Fiyat ve kalitenin yanında çevre belgesine (örneğin ISO 14001) ve atıklarını nasıl yönettiklerine bakmak.", "Malzeme aldığınız firmaları seçerken çevreye verdikleri önemi dikkate alıyor musunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000014"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Araçları dolu göndermek, yolu kısaltmak, boş dönüşleri azaltmak.", "Mal taşımayı ve sevkiyatı yakıt tasarrufunu düşünerek planlıyor musunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000015"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Yakından almak hem taşımadan çıkan karbonu hem de bekleme süresini azaltır.", "Malzemeleri mümkün olduğunca yakındaki firmalardan mı alıyorsunuz?" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-7000-8000-000000000016"),
                columns: new[] { "HelpText", "SimpleText" },
                values: new object[] { "Hurda metal, geri dönüştürülmüş plastik veya kâğıt, belgeli ahşap gibi.", "Geri dönüştürülmüş ya da çevre dostu malzeme kullanıyor musunuz?" });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerOptions_QuestionId_Score",
                table: "AnswerOptions",
                columns: new[] { "QuestionId", "Score" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerOptions");

            migrationBuilder.DropColumn(
                name: "HelpText",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SimpleText",
                table: "Questions");
        }
    }
}
