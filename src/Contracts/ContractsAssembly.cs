namespace SmartLogistics.Contracts;

// Bu projedeki event sozlesmeleri icin konvansiyonlar:
//
//   1. GECMIS ZAMAN isim: OrderCreated, CourierAssigned, Delivered.
//      "CreateOrder" bir komuttur ve servisin KENDI icinde kalir;
//      buraya sadece olmus bitmis olaylar girer.
//
//   2. SADECE VERI. Metot, is kurali, davranis yok. Bir event'in isi
//      "su oldu" demek; ne yapilacagina consumer karar verir.
//
//   3. METADATA BURAYA YAZILMAZ. MessageId, SentTime, CorrelationId gibi
//      alanlari MassTransit kendi zarfinda tasiyor; consumer tarafinda
//      ConsumeContext'ten okunur. Event'e elle EventId koymak ayni bilgiyi
//      iki yerde tutmak olurdu.
//
//   4. ORTAK ATA SINIF YOK — bilincli bir tercih. MassTransit topolojiyi
//      tipe gore kurar ve base tip de bir mesaj tipi sayilir: ortak bir
//      IntegrationEvent atasi olsaydi onun icin de bir exchange acilir ve
//      TUM event'ler oraya baglanirdi. Bunu engellemek MassTransit'e ozel
//      bir attribute (ve dolayisiyla bu projeye bir NuGet bagimliligi)
//      gerektirirdi. Sozlesmeleri bagimsiz tutmak daha degerli.
//
//   5. ALAN SILMEK veya YENIDEN ADLANDIRMAK breaking change'dir:
//      consumer'lar eski alani okumaya devam eder. Yeni alan eklemek
//      (nullable olarak) guvenlidir. Katalog: docs/EVENTS.md
//
// Ilk gercek sozlesme Step 07'de gelecek: OrderCreated.

// Testlerin ve DI taramalarinin "bu assembly" demesi icin tip guvenli isaret.
public sealed class ContractsAssembly;
