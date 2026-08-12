# PROGRESS.md — Proje İlerleme Takibi

> Bu dosya **canlı bir durum belgesidir** ve `WORKFLOW.md`'deki fazlara paralel olarak her oturum sonunda güncellenmelidir. Gemini, her yeni oturuma başlarken bu dosyayı okuyarak kaldığı yeri tespit eder (bkz. `WORKFLOW.md` Bölüm 3).
>
> **Güncelleme kuralı:** Bu dosyadaki değişiklikler de (diğer dosya değişiklikleri gibi) kullanıcı onayına tabidir — Gemini bir fazı kendiliğinden "tamamlandı" işaretleyemez; kullanıcının onayı üzerine işaretler.

---

## Son Güncelleme

- **Tarih:** 2026-08-10
- **Güncelleyen:** Gemini Oturumu

---

## Genel Durum Özeti

**Şu an aktif faz:** Faz 7 — Panel Modülleri (Beklemede)

**Sıradaki somut adım:** Faz 7 kapsamında Süper Admin için Kullanıcı/Rol görüntüleme temel sayfasının hazırlanması.

---

## Faz Durum Tablosu

| Faz | Açıklama | Durum | Not |
|---|---|---|---|
| 0 | Proje Kurulumu (Bootstrap) | ✅ Tamamlandı | Veritabanı oluşturuldu, altyapı hazırlandı. |
| 1 | INSPINIA Tema Entegrasyonu | ✅ Tamamlandı | Statik dosyalar eklendi, Razor view iskeletleri oluşturuldu. |
| 2 | Domain Katmanı | ✅ Tamamlandı | Temel POCO sınıfları, arayüzler ve enumlar oluşturuldu. |
| 3 | Data Katmanı (Dapper + Generic Repository) | ✅ Tamamlandı | GenericRepository test edildi ve PRD uyumu onaylandı. |
| 4 | Application Katmanı (Generic Service) | ✅ Tamamlandı | IResourceOwnershipPolicy ve IReorderService entegre edildi, rol bazlı yetkiler doğrulandı. |
| 5 | Infrastructure Katmanı | ✅ Tamamlandı | 6/6 alt-adım tamamlandı: Serilog, CAPTCHA, Rate Limiting, FileStorage, HtmlSanitizer, AuthenticationProvider |
| 6 | Kimlik Doğrulama ve Yetkilendirme | ✅ Tamamlandı | Rate Limiting - Kullanıcı Adı Değiştirerek Atlatma: LoginPolicy (kullanıcı+IP bazlı) tek başına, saldırganın farklı kullanıcı adları deneyerek limiti atlatmasını engellemez; GlobalPolicy (IP bazlı, 100/dakika) bir üst sınır sağlar ama bu gevşek bir eşik. Faz 9 (Güvenlik Sıkılaştırma) sırasında login formu için daha sıkı bir IP-only üst limit (ör. dakikada 20) eklenmesi değerlendirilmeli. |
| 7 | Panel Modülleri | ⬜ Beklemede | |
| 8 | UI (Public) Modülleri | ⬜ Beklemede | |
| 9 | Güvenlik Sıkılaştırma ve Doğrulama | ⬜ Beklemede | |
| 10 | Cilalama (Polish) | ⬜ Beklemede | |

**Durum değerleri:** ⬜ Beklemede · 🔄 Devam Ediyor · ✅ Tamamlandı · ⏸️ Duraklatıldı (sebep notta belirtilir)

---

## Faz 7 — Panel Modülleri Alt Kırılımı

*(Faz 7 birden fazla alt adımdan oluştuğu için ayrıca takip edilir — bkz. `WORKFLOW.md` Faz 7)*

> **Geçici Not (Faz 7 - Adım 1):** Panel dashboard/anasayfa henüz yok, login sonrası varsayılan yönlendirme (RedirectToLocal) rol bazlı geçici yönlendirme (SuperAdmin -> User/Index, Yetkili -> Application/Index) şeklinde ayarlandı. Gerçek dashboard geldiğinde güncellenecek.
> **Not:** `GenericService.CreateAsync` içerisindeki otomatik `SortOrder` ataması (`SortOrder == 0` ise) yalnızca formda `SortOrder` alanı yokken güvenlidir. İleride "listenin başına ekle" gibi bir özellik eklenirse (ör. formdan özel bir sıra girildiğinde veya bilerek 0/1 istenirse) bu mantık gözden geçirilmelidir.
> **Bilinen Eksik (Faz 7 - Adım 4 / Bilerek Ertelenmiş):** `PageController.DownloadAttachment` şu an `[Authorize(Policy = "YetkiliOrAbove")]` koruması altında çalışıyor ancak `Restricted` erişim tipi kontrolü yok — Kısıtlı bir sayfanın ek dosyasına erişim iznine bakılmıyor. Bu kontrol, `ContentPermissions` tablosu ve izin mekanizması Faz 7 Adım 5'te tamamlandıktan sonra Faz 8 UI modülleriyle birlikte uygulanacak.

- [x] Kullanıcı/Rol görüntüleme (temel)
- [x] Uygulama Yönetimi (CRUD + sıralama + erişim tipi)
- [x] Kategori Yönetimi (CRUD + sıralama)
- [x] Sayfa Yönetimi (CRUD + Rich Text + dosya/görsel yükleme + sıralama + erişim tipi)
- [x] Erişim/İzin yönetimi ekranı
- [ ] Log görüntüleme ekranı (Süper Admin)

---

## Faz 8 — UI (Public) Modülleri Alt Kırılımı

- [ ] Ana sayfa (Uygulama listesi)
- [ ] Uygulama detay sayfası
- [ ] Kategori detay sayfası
- [ ] Sayfa detay sayfası
- [ ] Arama
- [ ] Kısıtlı içerik → login yönlendirme
- [ ] Hata logu middleware

---

## Alınan Önemli Kararlar (Log)

*(Proje ilerledikçe burada, ne zaman/hangi kararın verildiği kısaca not edilir — ör. "hangi rate limit eşiği seçildi", "hangi dosya boyutu sınırı belirlendi" gibi.)*

- **Zaman Standardı:** Tüm projede ve veritabanında yerel saat yerine evrensel saat (UTC) kullanılmasına karar verildi (`GETUTCDATE()` ve `DateTime.UtcNow`). (2026-08-05)
- **Hard Delete İptali:** Fiziksel silme (hard delete) işleminin standart CRUD'un bir parçası olmamasına ve generic repo/servislerden kaldırılarak açıkça `SoftDeleteAsync` kullanılmasına karar verildi. (2026-08-05)
- **UserRoles Entity:** Dapper'ın junction tablolara eşlenmesi için `UserRole` ara tablosunun Domain katmanında C# entity'si olarak tutulmasına karar verildi. (2026-08-05)
- Faz 5 sırasında Users tablosunda kaynağı belirsiz, şifresiz/rolsüz 3 test kaydı (test_sa, test_y1, test_y2) bulundu ve temizlendi - hiçbir oturumda bilerek oluşturulmadıkları teyit edildi, güvenlik riski taşımıyorlardı (geçersiz PasswordHash).
- **Yetkili Görünürlük Netleştirmesi:** PRD 6.1'deki 'görür' ifadesi 'tüm içeriği görüntüleyebilir, yalnızca kendi oluşturduğunu yönetebilir (düzenle/sil/sırala)' olarak netleştirildi - başlangıçtaki katı yorum (yalnızca kendi içeriğini görme) kullanıcı deneyimi açısından yetersiz bulundu ve değiştirildi.

---

## Açık Konular / Netleşmemiş Noktalar

*(`PRD.md`'de "ileride netleşecek" olarak bırakılan veya proje sırasında ortaya çıkan, henüz karara bağlanmamış konular burada listelenir.)*

- Kurumun mail tabanlı kimlik doğrulama sisteminin protokolü/altyapısı henüz belirsiz (bkz. `PRD.md` Bölüm 4, Aşama 2) — `InstitutionalAuthProvider` bu netleştiğinde tasarlanacak.
- Geliştirmede kullanılacak kesin .NET sürümü teyit edilmeli (PRD'de "en güncel LTS" olarak bırakıldı).
- **SuperAdmin Seed Data:** `schema.sql` ile veritabanı kurulduğunda sadece varsayılan Roller eklenmiştir. İlk giriş yapacak "SuperAdmin" kullanıcısı henüz sistemde yoktur. Faz 6'da (Kimlik Doğrulama) şifre hash'leme (Password Hashing) altyapısı kurulduktan sonra, ilk SuperAdmin kullanıcısı seed/script ile eklenecektir.
- **AuditLogs Tablosu (Serilog):** Faz 5'te Serilog yapılandırması yapılırken `columnOptionsSection` ile `UserId`, `IPAddress` ve `RequestPath` sütunlarının ayrı sütun olarak tanımlanması gerekiyor. (Varsayılan `autoCreateSqlTable` şeması, sonradan Panel üzerinden kullanıcı bazlı filtrelemeyi desteklemez, standart log sütunları ile tabloyu oluşturur).
- **Yetkili İzinleri Sınırlaması (Faz 7'ye ertelendi):** Yetkili rolündeki bir kullanıcı, `PageAttachment` ve `ContentPermission` gibi `IAuditable` olmayan (Yani `CreatedByUserId` tutmayan) kaynaklarda şu an hiçbir zaman değişiklik yapamıyor (varsayılan red); bu durum Faz 7'de (Panel modülleri) ele alınmalı. Örneğin `PageAttachment` için silme/ekleme izni verilirken üst `Page` entity'sinin sahipliği kontrol edilerek izin verilebilir.
- **Rate Limiting Kullanıcı Adı Kısıtı (Faz 6'ya ertelendi):** Mevcut `LoginPolicy` IP bazlı partition kullanmaktadır. PRD 9.4'te belirtilen "Kullanıcı adı + IP bazlı" kısıtlama, Faz 6'da login formu yazılırken POST body'sinden kullanıcı adı okunarak eklenecektir.
- **PageAttachment İndirme İşlemi (Faz 7'ye ertelendi):** Dosya yükleme sonucu dönen `RelativePath` alanı (ör. `/App_Data/Uploads/Attachments/...`) doğrudan URL/link olarak kullanılamaz (statik sunuma kapalıdır). Faz 7'de bir Controller action'ı (ör. `/Panel/Sayfa/DosyaIndir/{id}`) üzerinden dosyayı App_Data'dan okuyup FileStreamResult olarak dönen güvenli bir mekanizma yazılmalıdır.
- **Departman/Grup Bazlı İzin Yönetimi (Planlanan Ek Özellik):** Kullanıcı bazlı ContentPermissions'a ek olarak, departman/grup bazlı erişim izni verilebilmesi isteniyor (ör. IT departmanı bir Restricted içeriğe toplu erişebilsin). Bu PRD.md'nin orijinal kapsamında YOKTU, sonradan eklenen bir gereksinim. Faz 7-10 (mevcut PRD/WORKFLOW kapsamı) tamamlandıktan SONRA, ayrı bir faz olarak ele alınacak. Gerektirecekleri: yeni Department entity/tablosu, User-Department ilişkisi, ContentPermissions tablosunun GranteeType (User/Department) ayrımı taşıyacak şekilde genişletilmesi, Faz 8'deki erişim kontrolü mantığının hem bireysel hem departman bazlı izni kontrol etmesi, PermissionController/Manage.cshtml'in güncellenmesi.

---

## Bilinen Riskler / Dikkat Edilecekler

*(Faz ilerledikçe ortaya çıkan, unutulmaması gereken teknik notlar buraya eklenir.)*

- **Infrastructure Şablon Kodları:** `Infrastructure/Captcha/AiGeneratedCaptchaProvider.cs` dosyası projeye Faz 0 şablonuyla birlikte önceden gelmiş, şu an atıl durumda. Faz 5'e gelindiğinde bu dosyanın PRD.md Bölüm 5 ile tam uyumlu olup olmadığı sıfırdan gözden geçirilmeli; ayrıca CS0618 (kullanımdan kaldırılmış metot) uyarısı o aşamada çözülmeli.

---

*Bu dosyanın amacı, oturumlar arası bağlamı korumaktır. Boş/eksik bırakılan bir bölüm, o konunun henüz ele alınmadığı anlamına gelir — "tamamlandı" varsayılmaz.*
