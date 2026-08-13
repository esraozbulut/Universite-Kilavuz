# PROJECT FILE INDEX

Bu doküman, projeyi devralacak yapay zeka ajanlarına rehberlik etmesi amacıyla kritik kaynak dosyalarının (Source Files) görevlerini ve önem derecelerini listeler.

| Dosya | Katman | Görev | Önem |
|---|---|---|---|
| `Kilavuz.Web.csproj` | Root | Proje bağımlılıkları ve genel konfigürasyon (.NET 8, Nullable). | HIGH |
| `Program.cs` | Web | DI (IoC) servis kayıtları, Middleware sıralaması, Rate Limit ve Auth ayarları. | CRITICAL |
| `appsettings.json` | Web | Serilog, veritabanı bağlantı dizesi, dosya boyut sınırları (şifreler maskelidir). | HIGH |
| `Domain/Entities/Page.cs` | Domain | Sayfa ana modeli. İçerik ve kapak resmi burada tutulur. | CRITICAL |
| `Domain/Entities/ContentPermission.cs` | Domain | Kısıtlı sayfalara erişim haklarını barındıran yetki tutucu yapı. | HIGH |
| `Application/Interfaces/IResourceOwnershipPolicy.cs` | Application | "Sadece kendi oluşturduğunu yönetebilir" kuralının soyutlaması. | CRITICAL |
| `Application/ResourceOwnershipPolicy.cs` | Application | Ownership kuralının SuperAdmin/Yetkili/Kullanici seviyesinde Dapper üzerinden doğrulanması. | CRITICAL |
| `Data/GenericRepository.cs` | Data | Tüm Entity'ler için ortak Dapper CRUD işlemleri (Soft Delete dahil). | CRITICAL |
| `Data/SqlConnectionFactory.cs` | Data | Veritabanı bağlantısı üretim merkezi. | HIGH |
| `Infrastructure/Security/HtmlSanitizerService.cs` | Infrastructure | XSS koruması için CKEditor çıktısının whitelist üzerinden temizlenmesi. | CRITICAL |
| `Infrastructure/Security/LocalTestAuthProvider.cs` | Infrastructure | Cookie login öncesi password hash, Role çekme ve credential doğrulama. | HIGH |
| `Infrastructure/Security/LoginRateLimiterPolicy.cs` | Infrastructure | Login Endpointi için IP+Username bazlı Rate Limiting uygulanması. | HIGH |
| `Infrastructure/Storage/FileStorageService.cs` | Infrastructure | Resim, Döküman ve Eklentilerin diske (wwwroot) güvenle kaydedilmesi. | HIGH |
| `Infrastructure/Storage/FileSignatureChecker.cs` | Infrastructure | Yüklenen dosyaların Magic Byte (MIME vb.) kontrolü. | HIGH |
| `Infrastructure/Captcha/AiGeneratedCaptchaProvider.cs` | Infrastructure | SkiaSharp ile sunucu bazlı CAPTCHA resim üretimi. | MEDIUM |
| `Controllers/KilavuzController.cs` | Web (Public UI) | Son kullanıcıya sayfaların sunulması ve (Restricted) erişim (CheckAccessAsync) kontrolü. | CRITICAL |
| `Areas/Panel/Controllers/PageController.cs` | Web (Panel) | Zengin içerik (Page) CRUD işlemleri, CKEditor upload karşılama noktaları. | CRITICAL |
| `Areas/Panel/Controllers/AuthController.cs` | Web (Panel) | Login/Logout süreçlerinin Cookie mekanizması ile işlenmesi. | HIGH |
| `Areas/Panel/Views/Page/Create.cshtml` | Web (Panel UI) | Yeni sayfa ekleme ekranı; CKEditor initialization scriptleri burada başlar. | HIGH |
| `Areas/Panel/Views/Page/Edit.cshtml` | Web (Panel UI) | Sayfa düzenleme ekranı; Attachment (Ek Dosyalar) modülü burada listelenir. | HIGH |
| `wwwroot/js/ckeditor/content-file-plugin.js` | Web (Static) | CKEditor için yazılmış özel "Sayfa İçi Dosya Widget" eklentisi (Downcast/Schema). | CRITICAL |
| `wwwroot/js/ckeditor/content-file-upload-adapter.js` | Web (Static) | CSRF ve Yetki uyumlu özel dosya yükleyici adapter. | HIGH |
| `wwwroot/css/site.css` | Web (Static) | Frontend dosya kartı tasarım kodları (.content-file). | MEDIUM |
