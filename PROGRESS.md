# PROGRESS.md — Proje İlerleme Takibi

> Bu dosya **canlı bir durum belgesidir** ve `WORKFLOW.md`'deki fazlara paralel olarak her oturum sonunda güncellenmelidir. Gemini, her yeni oturuma başlarken bu dosyayı okuyarak kaldığı yeri tespit eder (bkz. `WORKFLOW.md` Bölüm 3).
>
> **Güncelleme kuralı:** Bu dosyadaki değişiklikler de (diğer dosya değişiklikleri gibi) kullanıcı onayına tabidir — Gemini bir fazı kendiliğinden "tamamlandı" işaretleyemez; kullanıcının onayı üzerine işaretler.

---

## Son Güncelleme

- **Tarih:** 2026-08-13
- **Güncelleyen:** Gemini Oturumu

---

## Genel Durum Özeti

**Şu an aktif faz:** Tüm Fazlar Başarıyla Tamamlandı! (Proje Yayına / Teslime Hazır)

**Sıradaki somut adım:** (Varsa) İsteğe bağlı ek özellikler (ör. Departman/Grup Bazlı İzin Yönetimi).

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
| 6 | Kimlik Doğrulama ve Yetkilendirme | ✅ Tamamlandı | Rate Limiting - Kullanıcı Adı Değiştirerek Atlatma: LoginPolicy (kullanıcı+IP bazlı) tek başına, saldırganın farklı kullanıcı adları deneyerek limiti atlatmasını engellemez; GlobalPolicy (IP bazlı, 100/dakika) bir üst sınır sağlar ama bu gevşek bir eşik. Faz 9 (Güvenlik Sıkılaştırma) sırasında login formu için daha sıkı bir IP-only üst limit (ör. dakikada 20) eklenmesi değerlendirildi. |
| 7 | Panel Modülleri | ✅ Tamamlandı | Faz 7 Adım 6 ile Log yönetimi tamamlandı. Tüm 6 madde başarıyla uygulandı. |
| 8 | UI (Public) Modülleri | ✅ Tamamlandı | UI tarafındaki rotalar, sayfalar (Uygulama/Kategori/Sayfa Detay) ve bağımsız yetki kontrolleri (Application + Page seviyesi) başarıyla entegre edildi. |
| 9 | Güvenlik Sıkılaştırma ve Doğrulama | ✅ Tamamlandı | Login (boş ReturnUrl) sonrası genel arayüze yönlendirme, Navbar'a Yönetim Paneli/Kılavuza Dön ve güvenli Çıkış Yap linkleri eklendi. Captcha dev-bypass çift güvenlikli (config + IsDevelopment) olarak tamamlandı. Faz 9 Testleri: CSRF taraması yapıldı, tüm POST metodlarında Anti-Forgery token olduğu doğrulandı. XSS/Html.Raw taraması yapıldı, eksik kural 2.2 güvenlik yorum satırı koda eklendi. Login rate limit (GlobalLimiter bypass zafiyeti) kapatıldı. AuditLogs Serilog custom columns eklendi. |
| 10 | Cilalama (Polish) | ✅ Tamamlandı | Kapsamlı UI cilalama, animasyonlar, breadcrumb iyileştirmeleri, scroll-to-top eklentisi tamamlandı. |

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
- [x] Log görüntüleme ekranı (Süper Admin)

---

## Faz 8 — UI (Public) Modülleri Alt Kırılımı

- [x] Ana sayfa (Uygulama listesi)
- [x] Uygulama detay sayfası
- [x] Kategori detay sayfası
- [x] Sayfa detay sayfası
- [x] Arama
- [x] Kısıtlı içerik → login yönlendirme
- [x] Hata logu middleware

---

## Alınan Önemli Kararlar (Log)

*(Proje ilerledikçe burada, ne zaman/hangi kararın verildiği kısaca not edilir — ör. "hangi rate limit eşiği seçildi", "hangi dosya boyutu sınırı belirlendi" gibi.)*

- **Zaman Standardı:** Tüm projede ve veritabanında yerel saat yerine evrensel saat (UTC) kullanılmasına karar verildi (`GETUTCDATE()` ve `DateTime.UtcNow`). (2026-08-05)
- **Hard Delete İptali:** Fiziksel silme (hard delete) işleminin standart CRUD'un bir parçası olmamasına ve generic repo/servislerden kaldırılarak açıkça `SoftDeleteAsync` kullanılmasına karar verildi. (2026-08-05)
- **UserRoles Entity:** Dapper'ın junction tablolara eşlenmesi için `UserRole` ara tablosunun Domain katmanında C# entity'si olarak tutulmasına karar verildi. (2026-08-05)
- Faz 5 sırasında Users tablosunda kaynağı belirsiz, şifresiz/rolsüz 3 test kaydı (test_sa, test_y1, test_y2) bulundu ve temizlendi - hiçbir oturumda bilerek oluşturulmadıkları teyit edildi, güvenlik riski taşımıyorlardı (geçersiz PasswordHash).
- **Yetkili Görünürlük Netleştirmesi:** PRD 6.1'deki 'görür' ifadesi 'tüm içeriği görüntüleyebilir, yalnızca kendi oluşturduğunu yönetebilir (düzenle/sil/sırala)' olarak netleştirildi - başlangıçtaki katı yorum (yalnızca kendi içeriğini görme) kullanıcı deneyimi açısından yetersiz bulundu ve değiştirildi.
- **PageAttachment İndirme Erişimi:** PageAttachment indirme erişim kontrolü Faz 8'de tamamlandı (commit cc6bbe6, 8d8c051).
- **Rate Limiting Kullanıcı Adı Kısıtı:** LoginRateLimiterPolicy ile Kullanıcı adı + IP bazlı kısıtlama tamamlandı.
- **SuperAdmin Seed Data:** İlk giriş için oluşturulması planlanan `SuperAdmin` yetkili admin hesabı (`admin`) DB'ye eklendi ve manuel DB sorgusu ile doğrulandı.

- **Faz 9 Tamamlamaları:** AuditLogs tablosu için Serilog özel sütun yapılandırması (UserId, IPAddress, RequestPath) başarıyla eklendi, ara katman yazılarak IP adresi ve ID verileri zenginleştirildi. Ayrıca Yetkili rolünün (SuperAdmin dışındakilerin) Uygulamaları sabitlemesi (IsPinned) arayüzden ve sunucu tarafından engellendi. PageAttachment/ContentPermission "Yetkili" erişimi Faz 7-8'de zaten çözülmüştü. (2026-08-14)

---

## Açık Konular / Netleşmemiş Noktalar

*(`PRD.md`'de "ileride netleşecek" olarak bırakılan veya proje sırasında ortaya çıkan, henüz karara bağlanmamış konular burada listelenir.)*

- Kurumun mail tabanlı kimlik doğrulama sisteminin protokolü/altyapısı henüz belirsiz (bkz. `PRD.md` Bölüm 4, Aşama 2) — `InstitutionalAuthProvider` bu netleştiğinde tasarlanacak.
- Geliştirmede kullanılacak kesin .NET sürümü teyit edilmeli (PRD'de "en güncel LTS" olarak bırakıldı).
- **Departman/Grup Bazlı İzin Yönetimi (Planlanan Ek Özellik):** Kullanıcı bazlı ContentPermissions'a ek olarak, departman/grup bazlı erişim izni verilebilmesi isteniyor (ör. IT departmanı bir Restricted içeriğe toplu erişebilsin). Bu PRD.md'nin orijinal kapsamında YOKTU, sonradan eklenen bir gereksinim. Faz 7-10 (mevcut PRD/WORKFLOW kapsamı) tamamlandıktan SONRA, ayrı bir faz olarak ele alınacak. Gerektirecekleri: yeni Department entity/tablosu, User-Department ilişkisi, ContentPermissions tablosunun GranteeType (User/Department) ayrımı taşıyacak şekilde genişletilmesi, Faz 8'deki erişim kontrolü mantığının hem bireysel hem departman bazlı izni kontrol etmesi, PermissionController/Manage.cshtml'in güncellenmesi.

---

## Bilinen Riskler / Dikkat Edilecekler

*(Faz ilerledikçe ortaya çıkan, unutulmaması gereken teknik notlar buraya eklenir.)*

- **Infrastructure Şablon Kodları:** `Infrastructure/Captcha/AiGeneratedCaptchaProvider.cs` dosyası projeye Faz 0 şablonuyla birlikte önceden gelmiş, şu an atıl durumda. Faz 5'e gelindiğinde bu dosyanın PRD.md Bölüm 5 ile tam uyumlu olup olmadığı sıfırdan gözden geçirilmeli; ayrıca CS0618 (kullanımdan kaldırılmış metot) uyarısı o aşamada çözülmeli.

---

*Bu dosyanın amacı, oturumlar arası bağlamı korumaktır. Boş/eksik bırakılan bir bölüm, o konunun henüz ele alınmadığı anlamına gelir — "tamamlandı" varsayılmaz.*
