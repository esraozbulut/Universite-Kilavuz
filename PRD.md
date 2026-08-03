# PRD — Kurumsal Kullanım Kılavuzu Yönetim Sistemi

**Doküman Tipi:** Product Requirements Document (Ürün Gereksinim Dokümanı)
**Versiyon:** 1.0
**Referans Örnek:** Düzce Üniversitesi Kullanım Kılavuzu (https://kilavuz.duzce.edu.tr)

---

## 1. Proje Özeti

Kurumların (üniversite, kamu kurumu, şirket vb.) kullanıcılarına yönelik teknik/idari kullanım kılavuzlarını (Outlook kurulumu, VPN bağlantısı, yazılım kurulumları vb.) merkezi, hiyerarşik ve yönetilebilir bir yapıda sunan, **ASP.NET Core MVC (C#)** tabanlı web uygulaması. Sistem iki ana bölümden oluşur:

- **Panel (Yönetim Arayüzü):** İçeriklerin (Uygulama/Kategori/Sayfa) oluşturulduğu, düzenlendiği, sıralandığı, yetkilendirildiği ve loglandığı korumalı yönetim alanı.
- **UI (Herkese/Yetkiliye Açık Kılavuz Arayüzü):** Son kullanıcının kılavuzları görüntülediği, bazı içeriklerin login gerektirdiği herkese açık arayüz.

Referans alınan Düzce Üniversitesi kılavuz sayfası, bilgi mimarisinin (Uygulama → Kategori → Sayfa) somut örneğidir: "Outlook Kurulumu" bir **Uygulama**dır; "Android", "Windows", "IOS" bu uygulamaya bağlı **Kategori**lerdir; her kategori altındaki asıl içerik (ör. "IMAP protokolü outlook kullanımı", "POP3 protokolü outlook kullanımı") ise **Sayfa**dır.

---

## 2. Bilgi Mimarisi (İçerik Ağacı)

Sistem 3 seviyeli sabit bir ağaç yapısı üzerine kurulur:

```
Uygulama (Kök / Root)
 └── Kategori (Dal / Branch)
      └── Sayfa (Yaprak / Leaf — asıl içerik burada)
```

| Seviye | Örnek | Açıklama |
|---|---|---|
| **Uygulama** | Outlook Kurulumu | En üst seviye gruplama. Bir "servis/araç" başlığıdır. |
| **Kategori** | Windows, Android, IOS | Uygulamaya bağlı alt gruplama (platform, işletim sistemi, konu başlığı vb. olabilir). |
| **Sayfa** | IMAP Protokolü ile Outlook Kullanımı, POP3 Protokolü ile Outlook Kullanımı | Zengin metin içerikli, dosya/görsel eklenebilen asıl kılavuz içeriğidir. |

**Önemli kurallar:**
- Sayfa yaprak seviyedir; sayfanın altında başka bir seviye yoktur.
- Her seviyede (Uygulama içinde Kategoriler, Kategori içinde Sayfalar) **manuel/sürüklenebilir sıralama** desteklenir (`SortOrder` alanı). Kullanıcı en son eklediği kaydı en üste taşıyabilir ya da istediği gibi yeniden sıralayabilir (Inspinia temasındaki *Nestable List* bileşeni ile sürükle-bırak arayüzü önerilir; sıralama backend'de `SortOrder` (int) güncellemesiyle AJAX üzerinden kalıcı hale getirilir).
- Her seviye **Aktif/Pasif** durumuna sahiptir (yayından kaldırma, silmeden gizleme).
- Her Uygulama ve her Sayfa için ayrı ayrı **erişim/gizlilik** ayarı olabilir (bkz. Bölüm 6.5).

---

## 3. Kullanıcı Rolleri ve Yetki Matrisi

| Rol | Yetkiler |
|---|---|
| **Süper Admin** | Sistemdeki her şeye tam yetkili: tüm Uygulama/Kategori/Sayfa CRUD, kullanıcı ve rol yönetimi, tüm log kayıtlarını görüntüleme, sistem ayarları, erişim kısıtlaması tanımlama. |
| **Yetkili** | Yalnızca **kendisinin oluşturduğu** Uygulama(lar) ve bu Uygulamalara bağlı Kategori/Sayfalar üzerinde CRUD yetkisine sahiptir. Başka Yetkili'nin oluşturduğu Uygulamaları göremez/düzenleyemez (Süper Admin hariç). Log kayıtlarını göremez. |
| **Kullanıcı** | Yalnızca okuma (read-only) yetkisi. Herkese açık içerikleri ve kendisine özel olarak yetki verilmiş kısıtlı içerikleri görüntüleyebilir. Panelde herhangi bir düzenleme ekranına erişemez. |

**Yetkilendirme mantığı (önemli):**
- "Yetkili" rolü **kaynak sahipliğine (ownership)** dayalıdır: her Uygulama kaydında `CreatedByUserId` alanı tutulur; yetkilendirme kontrolünde bu alan referans alınır. Bu kural generic yetkilendirme servisinde (`IAuthorizationService` + custom `IResourceOwnershipPolicy<T>`) merkezi olarak uygulanır, controller'larda tekrar tekrar yazılmaz.
- Bir Yetkili birden fazla Uygulamaya sahip olabilir; ancak bir Uygulamanın sahibi tektir (gerekirse ileride çoklu sahiplik/eş-yetkili desteği eklenebilir — bkz. Bölüm 14).

---

## 4. Kimlik Doğrulama (Authentication) ve Kapsam Notu

Bu proje bir **staj çalışması** kapsamında geliştirilmektedir. Geliştirme/staj ortamında kurumun gerçek/merkezi kullanıcı sistemine erişim yoktur; bu nedenle kimlik doğrulama katmanı **iki aşamalı** olarak ele alınır:

**Aşama 1 — Staj/Geliştirme Ortamı (v1, şu an):**
- Kalıcı, gerçek bir kullanıcı/kişi veritabanı **kurulmayacaktır.** Sistem, geliştirme ve test amacıyla Süper Admin tarafından elle oluşturulmuş **birkaç geçici test kullanıcısı** ile çalışacaktır (`Users` tablosu yalnızca bu test kayıtlarını içerir, gerçek kurum personeli/öğrenci verisi barındırmaz).
- Panel girişi **kullanıcı adı/e-posta + şifre + CAPTCHA** ile yapılır.
- Self-servis kayıt (register) ekranı yoktur; e-posta doğrulama (email confirmation) bu aşamada uygulanmaz.
- Şifreler asla düz metin tutulmaz; ASP.NET Core Identity'nin `PasswordHasher` altyapısı (veya eşdeğeri PBKDF2 tabanlı hashleme) kullanılır.

**Aşama 2 — Kurumsal Entegrasyon (Prod, ileride):**
- Proje canlıya alındığında, kurumun **mail tabanlı** kimlik doğrulama sistemine bağlanacaktır. Bu doküman yazıldığı sırada kurumun kullandığı protokol/altyapı (ör. Active Directory/LDAP, kurumsal SMTP/IMAP doğrulama, ya da başka bir SSO servisi) henüz netleşmemiştir; bu nedenle bu entegrasyonun **teknik detayları bu PRD kapsamı dışındadır** ve entegrasyon zamanı geldiğinde ayrıca netleştirilip dokümante edilecektir.
- Bu geçişin sorunsuz olması için kimlik doğrulama katmanı **baştan soyutlanarak** tasarlanır: `IAuthenticationProvider` arayüzü tanımlanır; Aşama 1'de `LocalTestAuthProvider` (yerel test kullanıcıları) implementasyonu kullanılır, Aşama 2'de bunun yerine kurumun sistemine bağlanan bir `InstitutionalAuthProvider` implementasyonu eklenecektir. Roller (Süper Admin/Yetkili/Kullanıcı), yetkilendirme, oturum yönetimi ve loglama gibi geri kalan tüm katmanlar bu değişiklikten etkilenmeyecek şekilde tasarlanır.
- Oturum yönetimi cookie tabanlı yapılır (`CookieAuthenticationDefaults`), `HttpOnly`, `Secure`, `SameSite=Strict` bayrakları zorunludur — bu davranış her iki aşamada da aynıdır.
- Başarısız giriş denemeleri sayılır; belirli bir eşikten sonra (ör. 5 deneme) hesap geçici olarak kilitlenir (lockout) — bu davranış Rate Limit modülüyle birlikte çalışır.

---

## 5. CAPTCHA Modülü

- Login ekranında, otomatik giriş denemelerine (bot/brute-force) karşı **yapay zeka destekli, dinamik üretilen görsel CAPTCHA** kullanılacaktır.
- Her istek için sunucu tarafında yeni bir CAPTCHA görseli üretilir (bozuk/gürültülü metin, arka plan gürültüsü, karakter döndürme/kaydırma gibi insan-okur/bot-zor teknikleriyle), üretilen doğru değer sunucu tarafında (Session veya kısa ömürlü cache, ör. `IMemoryCache`/Redis) saklanır — **asla client tarafına gömülmez.**
- CAPTCHA doğrulaması sunucu tarafında yapılır; formun geri kalan alanlarıyla birlikte submit edilir.
- Modül, ileride 3. parti bir servisle (ör. görsel üretim API'si) değiştirilebilecek şekilde soyutlanır: `ICaptchaProvider` arayüzü + `AiGeneratedCaptchaProvider` implementasyonu.
- CAPTCHA süresi doldurulmazsa (ör. 2 dakika) otomatik geçersiz sayılır ve yenilenir.

---

## 6. Fonksiyonel Gereksinimler

### 6.1 Panel — Uygulama Yönetimi
- Listeleme (arama, filtre, sayfalama — DataTable/FooTable ile), sıralama (drag&drop), Aktif/Pasif toggle.
- Ekleme/Düzenleme: Ad, Açıklama, İkon/Görsel, Sıra No, Aktiflik, **Erişim Tipi** (Herkese Açık / Kısıtlı).
- Silme: soft delete (kayıt fiziksel silinmez, `IsDeleted` bayrağı ile işaretlenir) — onay için SweetAlert modalı zorunlu.
- Yetkili yalnızca kendi oluşturduğu Uygulamaları görür/yönetir; Süper Admin tümünü görür ve gerekirse sahiplik devredebilir (`Yeniden Ata` işlevi).

### 6.2 Panel — Kategori Yönetimi
- Bir Uygulamaya bağlı olarak oluşturulur (Uygulama seçilmeden Kategori oluşturulamaz).
- Alanlar: Ad, Açıklama, Sıra No, Aktiflik.
- Aynı sıralama ve soft-delete kuralları Kategori için de geçerlidir.
- Kategori, sahibi olduğu Uygulamanın erişim/yetki kurallarını miras alır (üzerinde ekstra kısıtlama tanımlanmaz — sadeleştirme amaçlı; gerekirse Bölüm 14'te opsiyonel genişletme önerilmiştir).

### 6.3 Panel — Sayfa Yönetimi
- Bir Kategoriye bağlı olarak oluşturulur.
- Alanlar: Başlık, **Rich Text içerik** (Summernote editör — Inspinia teması ile birlikte gelir), Sıra No, Aktiflik, **Erişim Tipi** (Herkese Açık / Kısıtlı — kısıtlıysa erişimi olan kullanıcı(lar) çoklu seçim ile atanır), Dosya/Görsel ekleri.
- **Görsel Yükleme:** Rich text editör içine sürükle-bırak/yapıştır ile görsel ekleme; ayrıca sayfa geneline kapak görseli.
- **Dosya Yükleme:** Sayfa altında indirilebilir ek dosyalar (pdf, docx, zip vb.) listesi; her dosya için ad, boyut, yükleyen kullanıcı, yükleme tarihi.
- Yükleme kısıtları: izinli MIME/uzantı beyaz listesi, maksimum dosya boyutu (konfigüre edilebilir, ör. 20 MB), dosya adı GUID ile yeniden adlandırılır (path traversal / çakışma önleme), içerik-tipi doğrulaması (uzantı + gerçek dosya imzası/magic number kontrolü).
- Rich text içerik veritabanına kaydedilmeden önce **HTML sanitize** edilir (XSS önleme — ör. HtmlSanitizer kütüphanesi).
- Sürüm geçmişi (nice-to-have, bkz. Bölüm 14).

### 6.4 Panel — Sıralama (Genel Kural)
- Uygulama listesi, bir Uygulamaya bağlı Kategori listesi, bir Kategoriye bağlı Sayfa listesi — üçü de bağımsız sıralanabilir.
- "Yeni eklenen en üstte görünsün" davranışı **varsayılan değildir**, kullanıcı tercihi olarak sunulur: yeni kayıt eklenirken "Listenin başına ekle" / "Listenin sonuna ekle" seçeneği + sonrasında sürükle-bırak ile serbest yeniden sıralama.
- Sıralama işlemi generic bir `IOrderable` arayüzü ve generic `ReorderService<T>` ile tüm 3 seviyede aynı koda dayanır (bkz. Bölüm 8.3).

### 6.5 Erişim Kontrolü / Gizlilik
- Her **Uygulama** ve her **Sayfa** için `AccessType`: `Public` (herkese açık, login gerektirmez) veya `Restricted` (yalnızca izinli kullanıcılar).
- `Restricted` bir kayda erişim, ilişkisel bir izin tablosu (`ContentPermissions`: `ContentType`, `ContentId`, `UserId`) üzerinden generic biçimde tanımlanır — hem Uygulama hem Sayfa seviyesinde aynı mekanizma kullanılır.
- Login olmayan bir kullanıcı kısıtlı içeriğe erişmeye çalışırsa login sayfasına yönlendirilir (dönüş URL'si `ReturnUrl` ile korunur).
- Login olan ama yetkisi olmayan `Kullanıcı` rolündeki biri kısıtlı içeriğe erişmeye çalışırsa **403 Erişim Reddedildi** sayfası gösterilir (bilgi sızdırmamak için içerik var/yok bilgisi verilmez).

### 6.6 UI (Public Kılavuz Arayüzü)
- Ana sayfa: Aktif Uygulamaların (kullanıcının erişim yetkisine göre filtrelenmiş) listesi, arama kutusu.
- Uygulama sayfası: bu uygulamaya bağlı Kategoriler.
- Kategori sayfası: bu kategoriye bağlı Sayfalar (sıralı).
- Sayfa detayı: Rich text içerik, ekli dosyalar (indirme linkleri), ekli görseller.
- Arama: Uygulama/Kategori/Sayfa başlık ve içeriğinde tam metin arama (MSSQL `LIKE` veya Full-Text Search — trafik/ölçek büyürse Full-Text Search önerilir).
- Duyarlı (responsive) tasarım — Inspinia temasının responsive grid yapısı korunur.

### 6.7 Bildirim ve Onay Deneyimi
- **SweetAlert2:** Silme, pasifleştirme, toplu işlem gibi **geri dönüşü kritik olan her onay adımında** kullanılır ("Emin misiniz?" modalı, Evet/Vazgeç butonları).
- **Toastr.js:** Her işlem sonucu bildiriminde kullanılır — "Kaydedildi", "Güncellendi", "Dosya yüklendi", "Onaylandı", "Hata oluştu" gibi kısa ömürlü, ekranın köşesinde beliren bildirimler (success/info/warning/error varyantları).
- Kural: Kritik/yıkıcı işlemler önce SweetAlert ile onaylanır → işlem backend'e gönderilir → sonuç Toastr ile bildirilir.

---

## 7. Loglama (Logging) Gereksinimleri

| Katman | Ne Loglanır | Nerede Tutulur | Kim Görebilir |
|---|---|---|---|
| **UI (Public)** | Yalnızca **hata (exception/error) logları** | Veritabanı (`ErrorLogs` tablosu) | — (kullanıcıya gösterilmez; sadece iç kullanım/gelecekte panel entegrasyonu için) |
| **Panel** | **Her şey**: login/logout, başarısız giriş denemeleri, CRUD işlemleri (kim, ne zaman, hangi kayıt, eski/yeni değer), yetkisiz erişim denemeleri, dosya yükleme/silme, sıralama değişiklikleri, rol/kullanıcı yönetimi işlemleri | Veritabanı (Serilog **MSSQL Sink**) | Yalnızca **Süper Admin** |

**Teknik detaylar:**
- Loglama kütüphanesi: **Serilog**. Panel tarafında `Serilog.Sinks.MSSqlServer` sink'i ile yapılandırma yapılır; ek olarak dosya/konsol sink'i (geliştirme ortamı için) opsiyoneldir.
- Serilog **enrichers** ile her log kaydına otomatik olarak şu bilgiler eklenir: `UserId`, `UserName`, `Role`, `IPAddress`, `UserAgent`, `RequestPath`, `CorrelationId`.
- Panel tarafında audit log, generic bir `IAuditLogger` servisi + action filter/middleware üzerinden merkezi olarak yakalanır; her controller'da manuel log çağrısı yazmak yerine, generic CRUD servisleri (`GenericService<T>`) işlemi otomatik loglar.
- Log kayıtları **hiçbir zaman** düzenlenemez/silinemez (yalnızca ekleme — append-only); Süper Admin panelinde filtrelenebilir/arşivlenebilir görüntüleme sunulur (tarih aralığı, kullanıcı, işlem tipi filtreleri).
- **Önemli kısıt (kullanıcı talebi):** *"Bu bağlamda hiçbir şahıs için oluşturulan kayıt veritabanında tutulmayacak"* — bu ifade, log kayıtlarının kişisel/hassas veri (ör. şifre, kişisel iletişim bilgisi) içermeyeceği; yalnızca işlemsel/denetim amaçlı (kim, ne zaman, ne yaptı) meta verinin tutulacağı şeklinde uygulanacaktır. Log kayıtlarında asla şifre, CAPTCHA çözümü gibi hassas veriler saklanmaz.

---

## 8. Teknik Mimari

### 8.1 Genel Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Framework | ASP.NET Core MVC — projede geliştirme başladığında yayınlanmış **en güncel LTS/stabil .NET sürümü** (bu doküman yazıldığında referans: .NET 10) |
| Dil | C# (son dil sürümü — nullable reference types, record, pattern matching aktif) |
| Veritabanı | Microsoft SQL Server (2025 veya kurulu olan en güncel sürüm) |
| Veri Erişimi | Dapper (Micro-ORM) — **tüm veri erişim metotları generic yapıda** |
| Kimlik Doğrulama | Cookie tabanlı Authentication + rol bazlı (Role-based) / policy bazlı (Policy-based) Authorization |
| Frontend Şablonu | INSPINIA Admin Theme — `HTML5_Full_Version` (Bootstrap 4.x, jQuery) — Razor view'lara entegre edilecek statik varlıklar |
| Rich Text Editör | Summernote (Inspinia paketiyle birlikte gelir) |
| Bildirim/Onay | SweetAlert2 (onay diyalogları), Toastr.js (bildirimler) |
| Loglama | Serilog + `Serilog.Sinks.MSSqlServer` |
| Rate Limiting | ASP.NET Core yerleşik `Microsoft.AspNetCore.RateLimiting` middleware |
| CAPTCHA | Özel/AI-destekli görsel CAPTCHA üretim servisi |

### 8.2 Katmanlı Mimari

```
/src
 ├── Kilavuz.Domain          → Entity/Model sınıfları, Enum'lar, sabitler
 ├── Kilavuz.Data             → Dapper bağlantı yönetimi, generic Repository katmanı
 ├── Kilavuz.Application      → Generic Service katmanı, iş kuralları, DTO/ViewModel eşleme
 ├── Kilavuz.Infrastructure   → Serilog konfigürasyonu, CAPTCHA servisi, Dosya depolama servisi, Rate limit ayarları
 ├── Kilavuz.Web.Panel        → Yönetim paneli MVC alanı/projesi (Area: "Panel")
 ├── Kilavuz.Web.UI           → Herkese açık kılavuz arayüzü MVC alanı/projesi (Area: "UI" veya kök)
 └── Kilavuz.Shared           → Ortak yardımcı sınıflar (extension'lar, sabitler)
```

> Panel ve UI, tek bir ASP.NET Core Web uygulaması içinde **Area** (`Areas/Panel`, `Areas/UI`) olarak ayrılabilir ya da ileri düzey izolasyon isteniyorsa iki ayrı proje + ortak paylaşılan katmanlar (Domain/Data/Application) şeklinde kurgulanabilir. Başlangıç için **tek proje + Area yapısı** önerilir (basitlik, ortak session/auth yönetimi).

### 8.3 Generic Yapı (Kritik Gereksinim)

Kullanıcının özellikle vurguladığı üzere **tüm CRUD ve veri erişim metotları generic olmalıdır.** Önerilen yaklaşım:

```csharp
// Data Katmanı
public interface IGenericRepository<T> where T : class, IEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync(object? filter = null);
    Task<int> InsertAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> SoftDeleteAsync(int id, int deletedByUserId);
    Task<bool> ReorderAsync(int id, int newSortOrder);
}

// Application Katmanı
public interface IGenericService<T> where T : class, IEntity
{
    Task<ServiceResult<T>> GetByIdAsync(int id);
    Task<ServiceResult<IEnumerable<T>>> GetAllAsync();
    Task<ServiceResult<int>> CreateAsync(T entity, int currentUserId);
    Task<ServiceResult<bool>> UpdateAsync(T entity, int currentUserId);
    Task<ServiceResult<bool>> DeleteAsync(int id, int currentUserId);
    // ↑ Bu katman: yetki kontrolü + audit log + validation'ı merkezi olarak sarmalar
}
```

- `Uygulama`, `Kategori`, `Sayfa`, `Kullanici` gibi tüm ana entity'ler bu generic altyapıdan (repository + service) türetilir; özel iş kuralı gereken yerlerde generic servis **inherit edilerek** (ör. `SayfaService : GenericService<Sayfa>, ISayfaService`) genişletilir, generic metotlar tekrar yazılmaz.
- Dapper sorguları generic repository içinde `typeof(T).Name` tabanlı tablo adı eşlemesi (attribute ile: `[Table("Sayfalar")]`) kullanılarak parametrik/yeniden kullanılabilir şekilde kurulur; SQL enjeksiyonuna karşı **her zaman parametreli sorgu** kullanılır, asla string concatenation yapılmaz.
- Sayfalama, filtreleme, sıralama gibi ortak davranışlar generic `QueryOptions` (ör. `PageIndex`, `PageSize`, `SortBy`, `SearchTerm`) parametresiyle tüm listeleme metotlarında ortaklaştırılır.

### 8.4 Taslak Veritabanı Şeması (Ana Tablolar)

| Tablo | Önemli Alanlar |
|---|---|
| `Applications` (Uygulamalar) | Id, Name, Description, IconPath, SortOrder, IsActive, AccessType, CreatedByUserId, CreatedAt, UpdatedAt, IsDeleted |
| `Categories` (Kategoriler) | Id, ApplicationId (FK), Name, Description, SortOrder, IsActive, CreatedByUserId, CreatedAt, UpdatedAt, IsDeleted |
| `Pages` (Sayfalar) | Id, CategoryId (FK), Title, ContentHtml, CoverImagePath, SortOrder, IsActive, AccessType, CreatedByUserId, CreatedAt, UpdatedAt, IsDeleted |
| `PageAttachments` (Sayfa Ekleri) | Id, PageId (FK), FileName, StoredFileName, FileSize, ContentType, UploadedByUserId, UploadedAt |
| `ContentPermissions` (Erişim İzinleri) | Id, ContentType (Application/Page), ContentId, UserId, GrantedByUserId, GrantedAt |
| `Users` (Kullanıcılar) | Id, UserName, Email, PasswordHash, IsActive, CreatedAt |
| `Roles` / `UserRoles` | SuperAdmin / Yetkili / Kullanıcı rol tanımları ve atamaları |
| `AuditLogs` (Panel Logları — Serilog MSSQL Sink hedefi) | Id, TimeStamp, Level, Message, UserId, UserName, IPAddress, RequestPath, Properties(JSON) |
| `ErrorLogs` (UI Hata Logları) | Id, TimeStamp, Message, StackTrace, RequestPath, IPAddress |
| `LoginAttempts` (Rate limit / brute-force takibi) | Id, UserNameAttempted, IPAddress, IsSuccess, AttemptedAt |

> Not: Şema; geliştirme sürecinde normalize edilecek nihai bir taslaktır, uygulama başlamadan önce detaylı ER diyagramı ile netleştirilmelidir.

---

## 9. Güvenlik Gereksinimleri (Kapsamlı)

1. **Kimlik Doğrulama:** Cookie tabanlı, `HttpOnly` + `Secure` + `SameSite=Strict`; oturum süresi sınırlı ve sliding expiration.
2. **Yetkilendirme:** Policy-based authorization; her Panel action'ı ilgili role/ownership policy'siyle korunur (`[Authorize(Policy = "...")]`).
3. **CAPTCHA:** Login ve (gerekirse) kritik formlarda bot/otomasyon koruması (Bölüm 5).
4. **Rate Limiting:** ASP.NET Core `RateLimiting` middleware ile:
   - Login endpoint'inde IP + kullanıcı adı bazlı sabit pencere (fixed window) veya sliding window limiti (ör. 5 istek/dakika).
   - Genel API/form submit endpoint'lerinde global limit (DoS/otomasyon önleme).
   - Limit aşımında `429 Too Many Requests` + kullanıcıya Toastr ile bilgilendirme.
5. **Girdi Doğrulama:** Sunucu tarafı model validation (Data Annotations / FluentValidation) — istemci tarafı validation asla tek başına güvenilir kabul edilmez.
6. **XSS Koruması:** Rich text içerik kayıt öncesi HTML sanitize edilir; Razor view'larda varsayılan HTML encode korunur (yalnızca sanitize edilmiş içerik `Html.Raw` ile basılır).
7. **CSRF Koruması:** Tüm POST formlarında Anti-Forgery Token (`@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]`) zorunlu.
8. **SQL Injection Koruması:** Dapper'da her zaman parametreli sorgu; dinamik SQL string concatenation yasak.
9. **Dosya Yükleme Güvenliği:** Uzantı + MIME + magic number üçlü kontrolü, boyut sınırı, yeniden adlandırma (GUID), yüklenen dosyaların doğrudan çalıştırılabilir konumda barınmaması (statik dosya sunumu, execute izni yok), görsellerin yeniden encode edilmesi (opsiyonel, gömülü zararlı payload riskini azaltmak için).
10. **Güvenlik Başlıkları:** `Content-Security-Policy`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Strict-Transport-Security` (HTTPS zorunlu, HTTP→HTTPS redirect).
11. **Şifreleme:** Şifreler hash'lenerek saklanır (tuzlu/salted hash); hassas konfigürasyon (bağlantı dizeleri, secret key'ler) `appsettings.json` yerine User Secrets / Environment Variables / Azure Key Vault benzeri bir mekanizmada tutulur.
12. **Denetim (Audit):** Bölüm 7'de detaylandırılan kapsamlı Panel loglaması.
13. **Erişim Kısıtlı İçerik:** Bölüm 6.5'te detaylandırılan izin modeli; yetkisiz erişim denemeleri de loglanır.
14. **Oturum Sonlandırma:** Idle timeout (Inspinia temasında hazır "Idle Timer" bileşeni kullanılabilir) — belirli süre işlem yapılmazsa oturum otomatik sonlandırılır/uyarı gösterilir.

---

## 10. Arayüz (UI) ve Tema Entegrasyonu — INSPINIA

- Kaynak: `Chuibility/inspinia` GitHub deposu → **`HTML5_Full_Version`** klasörü (Bootstrap 4.x, HTML5/CSS3, jQuery tabanlı statik tema).
- Statik varlıklar (`css`, `js`, `fonts`, `img`) `wwwroot` altına taşınır; sayfa şablonları Razor `_Layout.cshtml` + `_PanelLayout.cshtml` (Panel için) ve `_PublicLayout.cshtml` (UI için) olarak ikiye ayrılır.
- Panel tarafında temanın hazır bileşenlerinden şunlar doğrudan kullanılacaktır:
  - **Summernote** → Rich text editör (Sayfa içerik girişi).
  - **Dropzone.js** → Dosya/görsel yükleme modülü (sürükle-bırak).
  - **DataTables / FooTable** → Liste ekranları (arama, sayfalama, sıralama sütunları).
  - **Nestable List** → Sıralanabilir ağaç/liste görünümü (Kategori/Sayfa sıralama).
  - **SweetAlert** → Onay diyalogları.
  - **Toastr** → Bildirimler.
  - **Login / Lock Screen sayfaları** → Panel giriş ekranı temel alınır, CAPTCHA alanı eklenir.
- UI (public) tarafı, temanın daha sade sayfa şablonlarından (ör. Blog/Article view) esinlenerek Uygulama/Kategori/Sayfa listeleme ve detay görünümlerine uyarlanır.

---

## 11. Fonksiyonel Olmayan Gereksinimler (Non-Functional)

- **Tarayıcı Desteği:** Güncel Chrome, Edge, Firefox, Safari (son 2 major sürüm).
- **Duyarlılık (Responsive):** Masaüstü, tablet, mobil.
- **Performans:** Liste ekranlarında sayfalama zorunlu (büyük veri setlerinde tam liste çekilmez); sık erişilen public içerikler için opsiyonel önbellekleme (`IMemoryCache`/Redis — ileri aşama).
- **Erişilebilirlik:** Temel WCAG uyumluluğu (form etiketleri, kontrast, klavye navigasyonu) hedeflenir.
- **Sürdürülebilirlik:** Generic mimari sayesinde yeni bir içerik tipi/entity eklenmesi minimum kod tekrarı gerektirir.
- **Yedekleme:** MSSQL düzenli yedekleme politikası (altyapı/DevOps kapsamında, bu PRD'nin dışında ama önerilir).

---

## 12. Kapsam Dışı (Out of Scope — v1)

- Kurumun gerçek/merkezi mail tabanlı kimlik doğrulama sistemine entegrasyon (protokol/altyapı henüz belirsiz; ayrı bir aşamada ele alınacak — bkz. Bölüm 4, Aşama 2).
- Self-servis kullanıcı kaydı ve e-posta ile hesap doğrulama (email confirmation).
- Şifremi unuttum / e-posta ile şifre sıfırlama akışı (v1'de Süper Admin manuel sıfırlar).
- Çoklu dil desteği (i18n) — tema altyapısında mevcut ama v1 kapsamında aktif edilmeyecek.
- Mobil native uygulama.
- Üçüncü parti SSO/AD entegrasyonu.

---

## 13. Kabul Kriterleri (Örnek — Özet)

- [ ] Süper Admin, Yetkili ve Kullanıcı rolleriyle giriş yapabiliyor ve yetkilerine göre farklı ekranlar görüyor.
- [ ] Bir Yetkili yalnızca kendi oluşturduğu Uygulama ve altındaki Kategori/Sayfaları düzenleyebiliyor, başka Yetkili'nin içeriğine erişemiyor.
- [ ] Uygulama/Kategori/Sayfa listelerinde sürükle-bırak ile sıralama yapılıp kalıcı olarak kaydediliyor.
- [ ] Sayfa oluştururken Rich Text editör ile içerik girilebiliyor, görsel/dosya eklenebiliyor.
- [ ] `Restricted` olarak işaretlenmiş bir Sayfa, yetkisi olmayan/login olmayan kullanıcıya gösterilmiyor.
- [ ] Login ekranında CAPTCHA doğru çözülmeden giriş yapılamıyor.
- [ ] Ardışık başarısız login denemelerinde Rate Limit devreye giriyor (429 dönüyor / hesap geçici kilitleniyor).
- [ ] Panelde yapılan her CRUD işlemi Serilog aracılığıyla MSSQL'e yazılıyor ve yalnızca Süper Admin bu logları görüntüleyebiliyor.
- [ ] UI tarafında yalnızca hata (exception) logları veritabanına yazılıyor.
- [ ] Silme gibi kritik işlemlerde SweetAlert onayı isteniyor; her işlem sonrası Toastr bildirimi gösteriliyor.
- [ ] Tüm veri erişim ve servis metotları generic altyapı (`IGenericRepository<T>`, `IGenericService<T>`) üzerinden çalışıyor.

---

## 14. İleri Aşama / Opsiyonel Geliştirme Önerileri (v2+)

- Sayfa içerik sürüm geçmişi (kim, ne zaman neyi değiştirdi + eski sürüme dönebilme).
- Uygulama/Kategori bazında **birden fazla Yetkili** ataması (tekil sahiplik yerine ekip modeli).
- Tam metin arama için MSSQL Full-Text Search veya harici arama motoru (ör. Elasticsearch).
- Public tarafta kullanıcı geri bildirimi ("Bu sayfa faydalı oldu mu?").
- Bildirim merkezine e-posta entegrasyonu (Yetkili'ye onay/red bildirimleri).
- Çoklu dil (i18n) desteğinin aktifleştirilmesi.
- Panel logları için gelişmiş dashboard/analitik görünüm.
- **Kurumsal kimlik doğrulama entegrasyonu:** `IAuthenticationProvider` soyutlaması üzerinden kurumun mail tabanlı sistemine (protokol/altyapı belirlendiğinde) bağlanacak gerçek `InstitutionalAuthProvider` implementasyonunun geliştirilmesi; bu aşamada `Users` tablosunun kurumsal veriyle nasıl senkronize/eşleştirileceği (ör. mail adresi üzerinden otomatik kullanıcı oluşturma) ayrıca tasarlanmalıdır.

---

## 15. Sözlük

| Terim | Anlamı |
|---|---|
| Uygulama | İçerik ağacının kök seviyesi (ör. Outlook Kurulumu) |
| Kategori | Uygulamaya bağlı orta seviye gruplama (ör. Windows) |
| Sayfa | Kategoriye bağlı, asıl içeriğin bulunduğu yaprak seviye |
| Panel | Yönetim arayüzü (giriş gerektirir) |
| UI | Son kullanıcı kılavuz arayüzü (kısmen herkese açık) |
| Restricted | Belirli kullanıcılarla sınırlı erişim tipi |
| Generic Repository/Service | Tüm entity tipleri için ortak, tip-parametreli veri erişim/iş mantığı katmanı |
