# WORKFLOW.md — Geliştirme Süreci ve Fazlar

> Bu dosya, projenin **hangi sırayla** inşa edileceğini tanımlar. `PRD.md` neyin yapılacağını, `GEMINI.md` nasıl davranılacağını (özellikle güvenlik/onay), bu dosya ise **hangi adımın önce, hangisinin sonra** geleceğini tanımlar. Bu üç dosya birlikte okunmalıdır.
>
> Anlık durum (şu an hangi fazdayız, sıradaki adım ne) bu dosyada **değil**, `PROGRESS.md` dosyasında tutulur. Bu dosya statiktir, nadiren değişir.

---

## 0. Temel Kural: Faz Atlama Yasağı

- Fazlar **bağımlılık sırasına göre** dizilmiştir: bir faz, kendinden önceki fazın temel çıktıları tamamlanmadan başlatılamaz (ör. Data katmanı yokken Panel controller'ı yazılmaz).
- Bir sonraki faza geçmeden önce, mevcut fazın çıktıları kullanıcı tarafından **gözden geçirilip onaylanmış** olmalıdır. Gemini kendiliğinden "madem burada kaldık, şunu da yapayım" diyerek bir sonraki faza sızmaz.
- Bir fazın **tamamının** bitmesi şart değildir — kullanıcı bir fazı kısmen tamamlayıp bilinçli olarak sonraki faza geçmeyi tercih edebilir. Ama bu her zaman kullanıcının kararıdır, Gemini'nin inisiyatifi değildir.

---

## 1. Fazlar

### Faz 0 — Proje Kurulumu (Bootstrap)
- Solution ve proje yapısının oluşturulması (`Domain`, `Data`, `Application`, `Infrastructure`, `Web` — bkz. `PRD.md` Bölüm 8.2).
- Gerekli NuGet paketlerinin belirlenmesi (kurulumu onay gerektirir — bkz. `GEMINI.md` 1.6): Dapper, Serilog + `Serilog.Sinks.MSSqlServer`, HTML sanitizer kütüphanesi vb.
- MSSQL bağlantısının kurulması, User Secrets ile bağlantı dizesinin tanımlanması.
- `.gitignore` oluşturulması (`bin/`, `obj/`, `appsettings.Development.json`, secret dosyaları).
- Git deposunun başlatılması (ilk commit dahil her adım onay gerektirir).

**Çıktı:** Boş ama çalışan, derlenebilen bir ASP.NET Core MVC iskeleti.

---

### Faz 1 — INSPINIA Tema Entegrasyonu
- `HTML5_Full_Version` içindeki statik varlıkların (`css`, `js`, `fonts`, `img`) `wwwroot` altına taşınması.
- `_PanelLayout.cshtml` ve `_PublicLayout.cshtml` iskeletlerinin oluşturulması (henüz veri bağlanmadan, yalnızca temanın doğru render edildiğinin doğrulanması).
- Ortak partial view'ların (navbar, sidebar, footer) ayrıştırılması.

**Çıktı:** Tema doğru yüklenen, boş içerikli Panel ve Public sayfa iskeletleri.

---

### Faz 2 — Domain Katmanı
- Entity sınıfları: `Application`, `Category`, `Page`, `User`, `Role`, `ContentPermission`, `PageAttachment`, `AuditLog`, `ErrorLog`, `LoginAttempt` (bkz. `PRD.md` Bölüm 8.4).
- Ortak arayüzler: `IEntity`, `IOrderable` (`SortOrder`), `ISoftDeletable` (`IsDeleted`), `IAuditable` (`CreatedByUserId`, `CreatedAt`, `UpdatedAt`).
- Enum'lar: `AccessType` (Public/Restricted), `ContentType` (Application/Page), `UserRoleType` (SuperAdmin/Yetkili/Kullanici).

**Çıktı:** Veritabanından bağımsız, saf domain modelleri.

---

### Faz 3 — Data Katmanı (Dapper + Generic Repository)
- Dapper bağlantı fabrikası (`IDbConnectionFactory`).
- `IGenericRepository<T>` arayüzü ve implementasyonu (bkz. `PRD.md` Bölüm 8.3) — **tüm entity'ler için ortak.**
- Tablo adı eşleme mekanizması (attribute tabanlı).
- Bu fazda **henüz gerçek migration/tablo oluşturma yapılmaz** — yalnızca kod yazılır; migration/DB script'i çalıştırma Faz 3 sonunda ayrı bir onay adımıdır (bkz. `GEMINI.md` 1.3).

**Çıktı:** Herhangi bir entity için çalışan generic CRUD altyapısı (henüz UI'a bağlı değil, birim test/manuel test ile doğrulanabilir).

---

### Faz 4 — Application Katmanı (Generic Service)
- `IGenericService<T>` arayüzü ve implementasyonu — validation, ownership kontrolü ve audit log çağrısını sarmalayan katman.
- `ServiceResult<T>` (başarı/hata/mesaj) wrapper yapısı.
- `ReorderService<T>` (sıralama işlemleri için).
- `IResourceOwnershipPolicy<T>` (Yetkili'nin yalnızca kendi içeriğine erişimi için merkezi kural).

**Çıktı:** İş kurallarını içeren, henüz controller'a bağlanmamış servis katmanı.

---

### Faz 5 — Infrastructure Katmanı
- Serilog konfigürasyonu (MSSQL sink + enricher'lar).
- `ICaptchaProvider` + `AiGeneratedCaptchaProvider`.
- Rate Limiting middleware konfigürasyonu.
- `IFileStorageService` (dosya/görsel yükleme, uzantı/MIME/magic number kontrolleri).
- `IHtmlSanitizerService` (rich text içerik temizleme).
- `IAuthenticationProvider` arayüzü + `LocalTestAuthProvider` implementasyonu (bkz. `PRD.md` Bölüm 4).

**Çıktı:** Tüm çapraz kesen (cross-cutting) servisler hazır, henüz uçtan uca bağlanmamış.

---

### Faz 6 — Kimlik Doğrulama ve Yetkilendirme
- Cookie authentication kurulumu.
- Login/Logout akışı + CAPTCHA entegrasyonu (canlı, çalışan ilk kez bu fazda).
- Rol bazlı policy tanımları (`SuperAdminOnly`, `YetkiliOrAbove`, vb.).
- Test kullanıcılarının oluşturulması (yalnızca kullanıcı onayıyla, `LocalTestAuthProvider` üzerinden — gerçek/kalıcı kullanıcı verisi değildir).

**Çıktı:** Panel'e login olunabilen, rol bazlı erişimin çalıştığı ilk uçtan uca akış.

---

### Faz 7 — Panel Modülleri (Sırayla)
1. Kullanıcı/Rol görüntüleme (temel, yalnızca Süper Admin) — henüz tam yönetim ekranı şart değil.
2. **Uygulama** Yönetimi (CRUD + sıralama + erişim tipi).
3. **Kategori** Yönetimi (CRUD + sıralama).
4. **Sayfa** Yönetimi (CRUD + Rich Text + dosya/görsel yükleme + sıralama + erişim tipi).
5. Erişim/İzin yönetimi ekranı (`ContentPermissions` — kısıtlı içerik için kullanıcı ataması).
6. Log görüntüleme ekranı (yalnızca Süper Admin).
7. Her modülde SweetAlert (onay) ve Toastr (bildirim) entegrasyonu — modülle birlikte eklenir, sona bırakılmaz.

> Not: Uygulama → Kategori → Sayfa sırası zorunludur; bir üst seviye çalışmadan alt seviyenin CRUD ekranı anlamlı test edilemez.

**Çıktı:** Panelin tüm temel yönetim işlevleri çalışır durumda.

---

### Faz 8 — UI (Public) Modülleri
1. Ana sayfa (Uygulama listesi, erişim yetkisine göre filtrelenmiş).
2. Uygulama detay sayfası (Kategori listesi).
3. Kategori detay sayfası (Sayfa listesi).
4. Sayfa detay sayfası (içerik + ekler).
5. Arama.
6. Kısıtlı içerik → login yönlendirme akışı.
7. Hata logu middleware'i (yalnızca UI hataları DB'ye yazılır).

**Çıktı:** Son kullanıcının kılavuzları görüntüleyebildiği, erişim kurallarının uçtan uca çalıştığı public site.

---

### Faz 9 — Güvenlik Sıkılaştırma ve Doğrulama
- Güvenlik başlıklarının eklenmesi (CSP, X-Frame-Options vb.).
- HTTPS zorunluluğunun doğrulanması.
- CSRF/XSS/SQL Injection senaryolarının manuel test edilmesi.
- Rate Limit ve CAPTCHA'nın gerçek senaryoda (art arda hatalı giriş vb.) test edilmesi.
- Dosya yükleme uçlarının kötü niyetli dosya ile test edilmesi (yanlış uzantı, sahte MIME vb.).
- Log kayıtlarının (Panel/UI ayrımı, hassas veri sızmadığının) doğrulanması.

**Çıktı:** `PRD.md` Bölüm 9'daki tüm maddelerin fiilen doğrulanmış olması.

---

### Faz 10 — Cilalama (Polish)
- Responsive kontrol (mobil/tablet).
- Hata sayfaları (403/404/500) — Inspinia temasının hazır error page'leri uyarlanır.
- Performans gözden geçirmesi (sayfalama, gereksiz sorgular).
- Proje `README.md` dokümantasyonu.

---

## 2. Görev Verme Kuralları (Kullanıcı için Rehber)

Gemini'ye verilen görevlerin boyutu, hem onay sürecinin (`GEMINI.md`) sağlıklı işlemesi hem de hataların erken yakalanması için önemlidir:

- **Tercih edilen görev boyutu:** Tek bir entity'nin tek bir katmanı (ör. "Sayfa entity'si için repository katmanını yaz") veya tek bir uçtan uca dar akış (ör. "Uygulama CRUD'unun yalnızca listeleme ekranı"). Bir defada "tüm Paneli yap" gibi geniş görevler **verilmemelidir** — hem onay adımları takip edilemez hale gelir hem de hata ayıklama zorlaşır.
- Bir faz içindeki adımlar dahi tek seferde istenmeyebilir; kullanıcı isterse bir fazı kendi belirlediği alt adımlara bölebilir.
- Her görev öncesi, hangi faza ait olduğu (kullanıcı tarafından veya Gemini tarafından `PROGRESS.md`'ye bakılarak) netleştirilir.

---

## 3. Oturum Başlangıcı Protokolü

Her yeni Gemini oturumu (yeni sohbet/terminal oturumu) başladığında:

1. Gemini önce `PROGRESS.md` dosyasını okuyarak **hangi fazda kalındığını ve sıradaki adımın ne olduğunu** kontrol eder.
2. Kaldığı yeri kısaca kullanıcıya özetler ("Son durumda X fazındayız, Y adımı tamamlanmış görünüyor, sıradaki adım Z. Devam edelim mi?").
3. Kullanıcı onayı olmadan kaldığı yerden otomatik devam etmez — özetten sonra kullanıcının yönlendirmesini bekler.

---

## 4. Faz Sonu Kontrol Listesi (Her Faz İçin Ortak)

Bir faz "tamamlandı" olarak `PROGRESS.md`'ye işaretlenmeden önce şunlar sağlanmalıdır:

- [ ] Kod derleniyor (kullanıcı onayıyla build edilip doğrulanmış).
- [ ] İlgili fazda `GEMINI.md` Bölüm 2'deki (Güvenlik) ilgili maddeler gözden geçirilmiş.
- [ ] Görev kapsamı dışında istenmeyen değişiklik yapılmamış.
- [ ] Kullanıcı, faz çıktısını gözden geçirip onaylamış.
- [ ] `PROGRESS.md` güncellenmiş (durum + sıradaki adım).

---

*Bu dosya sabit bir referans niteliğindedir; fazların içeriği projede yeni bir ihtiyaç ortaya çıktıkça (kullanıcı onayıyla) güncellenebilir. Anlık ilerleme için her zaman `PROGRESS.md`'ye bakılmalıdır.*
