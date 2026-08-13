# PROJECT CONTEXT - KILAVUZ PROJESİ

Bu doküman, Kılavuz (Kurumsal Kullanım Kılavuzu Yönetim Sistemi) projesinin mevcut mimarisini, teknik kararlarını ve sistem bileşenlerini özetleyen kapsamlı bir devir ve analiz belgesidir.

## A. PROJE GENEL BİLGİLERİ

- **Proje Adı:** Kılavuz (Kurumsal Kullanım Kılavuzu Yönetim Sistemi)
- **Proje Amacı:** Kurum içi yazılım ve süreçlere ait kullanım kılavuzlarının merkezi olarak yönetilmesi ve yetkiye dayalı olarak kullanıcılara sunulması.
- **Kullanılan Teknoloji:** .NET 8, ASP.NET Core MVC
- **.NET Sürümü:** .NET 8.0
- **ASP.NET Core Sürümü:** 8.0
- **C# Sürümü:** C# 12 (ImplicitUsings ve Nullable özellikleri aktif)
- **Mimari Yaklaşım:** N-Tier / Clean Architecture'a yakın Katmanlı Mimari (Domain, Application, Data, Infrastructure, Web)
- **Proje Türü:** Web Uygulaması (MVC)
- **Veritabanı:** Microsoft SQL Server
- **ORM / Data Access:** Dapper (Micro-ORM)
- **Frontend Teknolojileri:** INSPINIA Admin Theme (Bootstrap 4 tabanlı, HTML5, CSS3, jQuery), CKEditor 5 (Vanilla JS, Native ES Modules)
- **CSS Framework:** Bootstrap 4
- **JavaScript Yaklaşımı:** Panel için INSPINIA (jQuery), CKEditor için ES6 Modules (Vanilla JS/Fetch)
- **Authentication Yöntemi:** Cookie Tabanlı Kimlik Doğrulama (`CookieAuthenticationDefaults.AuthenticationScheme`)
- **Authorization Yöntemi:** Policy-based Role Authorization & Custom Resource Ownership Authorization (`IResourceOwnershipPolicy`)

## B. PROJE DOSYA AĞACI

```
c:\Kılavuz
├── Kilavuz.Web
│   ├── Kilavuz.Web.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Domain/
│   │   ├── Entities/ (Application, Category, Page, User, vb.)
│   │   ├── Enums/ (AccessType, ContentType, UserRoleType)
│   │   └── Interfaces/ (IEntity, IAuditable, vb.)
│   ├── Application/
│   │   ├── Interfaces/ (IGenericService, IPageService, vb.)
│   │   └── Services/ (GenericService, PageService, vb.)
│   ├── Data/
│   │   ├── IDbConnectionFactory.cs / SqlConnectionFactory.cs
│   │   ├── IGenericRepository.cs / GenericRepository.cs
│   │   └── TypeHandlers/ (DapperEnumAsStringHandler)
│   ├── Infrastructure/
│   │   ├── Captcha/ (AiGeneratedCaptchaProvider)
│   │   ├── Logging/ (AuditLogFilter)
│   │   ├── Middleware/ (GlobalExceptionHandler)
│   │   ├── Security/ (LocalTestAuthProvider, HtmlSanitizerService, LoginRateLimiterPolicy)
│   │   └── Storage/ (FileStorageService, FileSignatureChecker)
│   ├── Controllers/ (Public UI rotaları: KilavuzController, HomeController, SearchController, vb.)
│   ├── Areas/
│   │   └── Panel/
│   │       ├── Controllers/ (Auth, Page, Category, vb.)
│   │       └── Views/
│   ├── Views/ (Public UI)
│   └── wwwroot/
│       ├── css/ (site.css, INSPINIA stilleri)
│       ├── js/ (ckeditor/content-file-plugin.js, vb.)
│       └── uploads/ (images, documents, attachments)
├── PRD.md (Ürün Gereksinimleri)
├── WORKFLOW.md (İş Akışı)
├── PROGRESS.md (Geliştirme Günlüğü)
└── GEMINI.md (Kurallar)
```

## C. PROJECT / CSPROJ ANALİZİ

Tüm katmanlar tek bir proje (`Kilavuz.Web.csproj`) içinde klasörlere ayrılmıştır (Logical N-Tier).

- **TargetFramework:** net8.0
- **Nullable:** enable
- **ImplicitUsings:** enable
- **Bağımlılıklar (Packages):**
  - `Dapper` (v2.1.79): Veritabanı işlemleri (Micro-ORM)
  - `HtmlSanitizer` (v9.1.974): XSS koruması için zengin metin temizliği
  - `Microsoft.Data.SqlClient` (v7.0.2): SQL Server driver
  - `Serilog.AspNetCore` (v10.0.0): Loglama altyapısı
  - `Serilog.Sinks.MSSqlServer` (v10.0.0): Veritabanına audit/hata logu yazma
  - `SkiaSharp` (v4.151.0): Gelişmiş görsel oluşturma (CAPTCHA ve Resim işleme)
  - `SkiaSharp.NativeAssets.Linux.NoDependencies`: Linux ortamı (Docker vb.) için Skia desteği

## D. DOMAIN KATMANI

POCO Entity'leri ve arayüzler yer almaktadır.
Önemli Entity'ler:
- `User`: Id, UserName, Email, PasswordHash, IsActive. (Audit loglarda UserName önemli yer tutar).
- `Role`: Id, Name (Admin, Yetkili, Kullanici).
- `UserRole`: User ve Role arasındaki N:N tabloyu Dapper'da yönetmek için oluşturulmuş Entity.
- `Application`: Kılavuzun kök nesnesi. `AccessType` (Public/Restricted) içerir. 
- `Category`: Uygulama altındaki alt kategoriler. ApplicationId'ye bağlıdır.
- `Page`: Sayfalar. İçerisinde zengin metin (`ContentHtml`), kapak görseli (`CoverImagePath`) bulunur. CategoryId'ye bağlıdır.
- `PageAttachment`: Sayfalara ait "Ek Dosyalar".
- `ContentPermission`: Kısıtlı içeriğe erişim atamalarını tutan yapı (UserId, ContentId, ContentType).
- `AuditLog`: Hata ve işlem loglarını barındırır.
- `LoginAttempt`: Login denemelerini ve CAPTCHA durumunu tutar.

**Ortak Arayüzler (Interfaces):**
`IEntity` (Id barındırır), `IAuditable` (CreatedAt, CreatedByUserId vb.), `ISoftDeletable` (IsDeleted), `IOrderable` (SortOrder).

## E. VERİTABANI

SQL Server kullanılmaktadır. (Dapper ile Repository üzerinden erişilir).
- **Tablolar:** Users, Roles, UserRoles, Applications, Categories, Pages, PageAttachments, ContentPermissions, AuditLogs.
- **Soft Delete:** Tablolarda `IsDeleted` sütunu var. Sorgularda `WHERE IsDeleted = 0` filtresi zorunludur.
- **Audit:** `CreatedByUserId`, `CreatedAt`, `UpdatedAt` kullanılıyor.
- **Enums:** `AccessType` (Public, Restricted vb.) Dapper Type Handler sayesinde veritabanına String (VARCHAR) olarak kaydediliyor.

## F. DATA KATMANI

- **Bağlantı Yönetimi:** `IDbConnectionFactory` ve `SqlConnectionFactory` kullanılarak SQL bağlantıları elde edilir.
- **Repository Pattern:** `IGenericRepository<T>` kullanılmıştır. Dapper ile `CRUD`, `SoftDeleteAsync` (IsDeleted = 1 yapan) işlemleri sağlar.
- **Transaction:** Generic yapılardan ziyade, servis/controller tarafında kompleks işlemlerde (ReorderService vb.) ihtiyaç halinde desteklenebilecek şekildedir.

## G. APPLICATION KATMANI

- **GenericService:** `IGenericService<T>`, generic repoyu çağırır. Audit/SoftDelete ve yetki (ResourceOwnershipPolicy) işlemlerini sarmalar.
- **ResourceOwnershipPolicy:** Kuralı işler: `SuperAdmin` her şeyi, `Kullanici` hiçbir şeyi, `Yetkili` sadece `CreatedByUserId == currentUserId` olanları değiştirebilir.
- **PageService / ReorderService:** Sıralama güncelleme vb. spesifik iş mantıkları bu serviste ele alınır.

## H. AUTHENTICATION

- **Yöntem:** Cookie Authentication (`Kilavuz.Auth`).
- **Provider:** `LocalTestAuthProvider` sınıfı kullanılır.
- **Akış:**
  1. Kullanıcı `/Panel/Auth/Login` formunu gönderir.
  2. Rate Limiting çalışır (IP+Username bazlı limit).
  3. `AiGeneratedCaptchaProvider` ile doğrulanır.
  4. `PasswordHasher<User>` ile Hash kontrolü.
  5. `Dapper` üzerinden user'ın Rolleri çekilir.
  6. `ClaimsIdentity` (ClaimTypes.Role vb.) oluşturulur.
  7. `HttpContext.SignInAsync` ile Cookie basılır. `RedirectToLocal` ile hedefe yönlendirilir.

## I. AUTHORIZATION

- **Policy-based:** `[Authorize(Policy = "SuperAdminOnly")]` veya `YetkiliOrAbove`.
- **Panel Yetkileri:** Kategori/Sayfa eklemek, düzenlemek için Controller seviyesinde OwnershipPolicy ile (Örn. `GetCategoryOwnerInfoAsync`) sahibine bakılır.
- **Public Erişim (KilavuzController):** `AccessType.Restricted` bir sayfaya veya kategoriye (ya da uygulamaya) girildiğinde:
  1. Login değilse login'e gönderir.
  2. Logins ise `ContentPermissions` tablosunda `UserId` ve `ContentId` sorgusu (Dapper `COUNT(1)`) yapılır. İzin yoksa `Forbid()` (403) fırlatır.

## J. RATE LIMITING

- **Global Policy:** Tüm isteklere IP bazlı 1 dakika içinde 100 istek limiti.
- **Login Policy:** `LoginRateLimiterPolicy` aracılığıyla.
- **AÇIK (Vulnerability):** Login Rate Limit `partitionKey` olarak IP+Username kullanır (`$"{ip}:{username}"`). Attacker Username'i değiştirerek aynı IP'den her bir Username için 5 istek (dakika) limiti kazanabilir, yani bypass edilebilir. (PROGRESS.md'de kayıtlıdır, düzeltilecektir).

## K. CAPTCHA

- **Provider:** `AiGeneratedCaptchaProvider`. SkiaSharp kullanılarak sunucu tarafında hafızada `SKSurface` ile PNG çizilir (çizgiler, döndürülmüş karakterler, gürültü noktaları).
- **Entegrasyon:** Oluşturulan key Session'a veya MemoryCache'e yazılarak Login POST sırasında doğrulanır. Development ortamında konfigürasyon üzerinden bypass edilebilir.

## L. FILE STORAGE

- **Altyapı:** `FileStorageService` `wwwroot/uploads/` altını yönetir.
- **Üçlü Ayrım:**
  1. **Image Upload (CKEditor):** `.jpg, .jpeg, .png, .gif`. `uploads/images/` altına, SkiaSharp ile işlenerek kaydedilir.
  2. **Content File (CKEditor İçine Gömülü):** `.pdf, .docx, .xlsx, .pptx, .csv`. `uploads/documents/` altına GUID ismi ile kaydedilir. Magic byte (`FileSignatureChecker`) kontrolünden geçer. CSV magic byte kontrolünden bypass edilmiştir (Magic byte yoktur).
  3. **Page Attachment (Ek Dosyalar):** Kılavuz sayfası altındaki dosyalardır. Ayrı endpoint kullanılır, `uploads/attachments/` altına atılır.
- **Güvenlik:** XSS ve Path traversal'a karşı orijinal dosya isimleri GUID ile değiştirilir ve çalıştırılabilir kodları (`.exe, .php, .html`) yüklenmesi engellenir.

## M. CKEDITOR 5

- **Yapı:** CDN üzerinden UMD/ES Modules yöntemiyle `Create.cshtml` ve `Edit.cshtml` içinde init edilir.
- **Eklentiler:** Vanilla CKEditor eklentileri yanında custom olarak `ContentFilePlugin` yazılmıştır.
- **Content File Widget:**
  - *Data Downcast (Canonical HTML):*
    ```html
    <a href="/uploads/documents/guid.pdf" class="content-file" data-file-name="Test.pdf" data-file-type="application/pdf" target="_blank" rel="noopener noreferrer">
        <span class="content-file-icon">📕</span>
        <span class="content-file-info">
            <span class="content-file-name">Test.pdf</span>
            <span class="content-file-type">PDF Dosyası</span>
        </span>
        <span class="content-file-actions">
            <span class="content-file-action">Aç</span>
            <span class="content-file-action">İndir</span>
        </span>
    </a>
    ```
  - *Editing Downcast:* Aynı HTML yapısı Editör içerisinde UI Element ve `toWidget` kullanılarak render edilir. CSS'i `site.css` içindedir.

## N. HTML SANITIZATION

- **Kütüphane:** `Ganss.Xss.HtmlSanitizer`.
- **İzinler:** Belli başlı güvenli HTML tag'leri (`p, b, i, a, span, table vb.`) ve attributeler (`href, src, class, style, data-file-name, vb.`) whitelist mantığıyla geçerlidir. JS, data-url ve iframe varsayılan olarak reddedilir.
- **Image Kuralı:** Görsel (`img src`) sadece `/uploads/images/` altından sunuluyorsa kabul edilir.

## O. PAGE MODÜLÜ

- **PageController (Panel):** Yetkililer (CreatedByUserId'ye dayalı) ve SuperAdminler sayfa yaratabilir, değiştirebilir.
- **LifeCycle:** 
  1. GET Create.
  2. CKEditor ile içerik (ContentHtml) hazırlanır. (Resim veya dosya yüklenirse arkaplanda asenkron çalışır).
  3. POST Create.
  4. Controller'da model validation.
  5. `HtmlSanitizer` ile `ContentHtml` temizlenir.
  6. DB'ye kaydedilir.
  7. Public arayüzde doğrudan gösterilir.

## P. PAGE ATTACHMENT

- Sayfaların altında yer alan geleneksel "Ek Dosyalar" mantığıdır. (Content File Widget'tan farklıdır).
- `PageAttachments` tablosuna kaydedilir. 
- Yüklenen dosyalar `PageController.UploadAttachment` ile karşılanır, listelemede sayfa detayında (Edit) görüntülenir.
- **Risk (TODO):** `PageController.DownloadAttachment` restricted yetki kontrolü henüz yoktur (PROGRESS.md de yazılıdır).

## Q. PUBLIC UI

- **Rotalar:** `/` (Home), `/kilavuz/{appId}`, `/kilavuz/{appId}/{categoryId}`, `/kilavuz/{appId}/{categoryId}/{pageId}`, `/Search`
- Ziyaretçi bir restricted sayfaya tıkladığında, `CheckAccessAsync` (KilavuzController) aracılığıyla cookie'si yoksa login'e, varsa `ContentPermissions` üzerinden iznine bakar. İzin yoksa Forbid, varsa görüntüler.

## R. PANEL UI

- `/Panel/Application`, `/Panel/Category`, `/Panel/Page`...
- Tüm bu yapılar `INSPINIA` template kullanır.
- Dashboard yapısı geçicidir, oturum açan kişi yetkisine göre direkt belli listelere (App/User vb.) yönlendirilir.

## S. MIDDLEWARE PIPELINE

Program.cs deki sıra:
1. `UseExceptionHandler` (Hata sayfasına)
2. `UseHttpsRedirection`
3. `UseStaticFiles` (wwwroot izinleri)
4. `UseRouting`
5. *Custom Login Rate Limiter Middleware (POST parametresini okur)*
6. `UseRateLimiter`
7. `UseAuthentication`
8. `UseAuthorization`
9. `UseSession`
10. `MapControllerRoute` / `MapControllers`

## T. LOGGING / AUDIT

- **Serilog:** `Serilog.Sinks.MSSqlServer` kullanılıyor. 
- **Veritabanı:** Loglar `AuditLogs` tablosuna aktarılır.
- **Filtreleme:** `AuditLogFilter` sınıfı sayesinde IP, RequestPath, Action vs. kaydedilir.
- (Risk: `columnOptionsSection` ile `UserId` gibi alanlar tam oturmamış, PROGRESS.md'de bilinen eksik).

## U. CONFIGURATION

- `appsettings.json` içinde:
  - `Serilog` ayarları (Default: Information)
  - `ConnectionStrings`: (Şifreler/değerler maskeli ***MASKED***)
  - `FileStorage.MaxFileSizeMb` : 10
  - `Security:DisableRateLimitInDev`, `Security:DisableCaptchaInDev` gibi dev bypass anahtarları.

## V. SECURITY AUDIT

| Konu | Durum | Açıklama |
|---|---|---|
| XSS | GÜVENLİ | HtmlSanitizer devrede. View'lerde Html.Raw sadece sanitize edilmiş alanlara basılıyor. |
| CSRF | GÜVENLİ | `[ValidateAntiForgeryToken]` ajax ve form requestlerinde aktif. |
| SQL Injection | GÜVENLİ | Dapper parametreli sorgularla kullanılıyor (`@Param`). |
| Path Traversal | GÜVENLİ | Yüklemelerde dosya isimleri Guid ile değiştiriliyor. |
| Magic Byte / MIME | GÜVENLİ | SkiaSharp ve `FileSignatureChecker` ile inceleniyor. |
| ReturnUrl | RİSKLİ / İYİLEŞTİRİLMELİ | `LocalRedirect` veya `IsLocalUrl` kontrolü olduğu görülmeli (Auth controller incelenmeli, genelde ASP.NET halleder). |
| Rate Limiting | RİSKLİ | Kullanıcı adı değiştirilerek bypass edilebilir durumda (IP:User). |
| BAC / IDOR | GÜVENLİ | Ownership/Policy mimarisi ile kontrol ediliyor. |
| Attachment Download | RİSKLİ | Kısıtlı sayfaların ek dosyaları public kalabilir (Gelecek fazda ele alınacak). |

## W. PROGRESS.MD ANALİZİ

| Konu | PROGRESS.md | Gerçek Kod | Durum |
|---|---|---|---|
| Kullanıcı + IP bazlı limit zaafiyeti | Faz 6'da not edilmiş | Kodda IP:Username | Uyumlu |
| DownloadAttachment Restricted Bypass | Faz 7'de not edilmiş | Kodda DownloadAttachment auth kontrolü yetersiz | Uyumlu |
| UTC Standardı | Uygulanacak denmiş | Domain/Entities Date = DateTime.UtcNow | Uyumlu |
| SuperAdmin Seed | Yok denmiş | DbScripts vb yok | Uyumlu |

## X. PRD / WORKFLOW

- Tamamlanmış: Altyapı, Yetki Mimarisi, Zengin Metin, Dosya Depolama, Public UI Erişimleri.
- Tamamlanacak: Dashboard UI, Rate Limit / Attachment yetki açıkları, Kurumsal Auth entegrasyonu (AD/LDAP), Departman bazlı erişim modeli.

## Y. BİLİNEN TEKNİK BORÇLAR

1. **Login Rate Limiter Bypass** - CRITICAL
2. **PageAttachment Erişim İhlali (Restricted için)** - HIGH
3. **AuditLogs Özel Sütun (UserId/IP) Konfigürasyonu** - LOW
4. **Dashboard Sayfası Eksikliği** - LOW

## Z. GELECEK GELİŞTİRMELER İÇİN BAĞIMLILIK HARİTASI

**Özellik:** Departman Bazlı Erişim
- *Etkilenecekler:* Domain (Department Entity, User-Department ilişkisi), Db Schema, `KilavuzController.CheckAccessAsync`, `ContentPermission` tablosunun mantığı (User mı Group mu?).

**Özellik:** Kısıtlı Dosya İndirme (Attachment/ContentFile)
- *Etkilenecekler:* Uygulama şu an statik dosyaları `/uploads` altından servis ediyor. Public klasöründen çıkartılıp, `[Authorize]` veya Kısıt Kontrolü içeren yeni bir route (`/File/Download/{id}`) inşa edilmeli.

---

# AI DEVELOPMENT MAP

Bu Kılavuz projesi, ASP.NET Core MVC 8.0 ve Dapper kullanılan katmanlı (Clean-like) bir CMS uygulamasıdır. Veritabanı ve ORM hafif, güvenlik kuralları katıdır.

1. **Mimari:** Web (MVC), Application (Servisler), Data (Dapper Repo), Infrastructure (Auth/Storage/Security), Domain (Entity/Enum).
2. **Authentication:** Cookie. `LocalTestAuthProvider` ile Dapper üstünden çalışır.
3. **Authorization:** SuperAdmin (Tam yetki), Yetkili (Self-Owned yetki), Kullanici (Panel kapalı).
4. **File Storage:** Local disk `wwwroot/uploads`. Üç ayrı alt sistem var: Images, Documents (ContentFile), Attachments. Hepsi GUID alır.
5. **CKEditor:** UMD Modules, Vanilla JS. Resim (ajax adaptör) ve Dosya Widget'ı (Custom model, dataDowncast span yapılı) var.
6. **Database:** SQL Server, IsDeleted (Soft Delete) mantığı standarttır. 
7. **Security:** HtmlSanitizer `ContentHtml` üzerinde zorunlu çalışır. Dosyalara magic byte koruması uygulanır.

# DO NOT BREAK

- Mevcut CKEditor (ImageUpload) Altyapısı ve Ayarları
- `HtmlSanitizerService` whitelist yapısı (Bozulursa XSS veya içerik kayıpları oluşur)
- `Content File Widget`'ın `dataDowncast` span kart HTML yapısı (Bozulursa front-end görünümü çöker)
- `IResourceOwnershipPolicy` ve Ownership (Sahiplik) Kuralları
- `Soft Delete` filtreleri (`IsDeleted = 0` Dapper SQL'leri)
- Mevcut public rotalar (`/kilavuz/...`)
- Sırların (secrets) gizliliği (asla repoya/git'e yazılmaması)
- UTC tarih/saat standartları
