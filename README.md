# Kurumsal Kullanım Kılavuzu Yönetim Sistemi

Bu proje, kurum içi kullanıcıların çeşitli sistemler ve süreçler hakkında bilgilendirilmesini sağlamak amacıyla geliştirilmiş kapsamlı bir "Kullanım Kılavuzu" ve belgelendirme portalıdır. Proje, hem son kullanıcılar için modern ve şık bir arayüz (Public UI) hem de yöneticiler için güçlü bir Yönetim Paneli (Panel) sunmaktadır.

## 🚀 Proje Hakkında

- **Geliştirme Ortamı:** .NET 8 (ASP.NET Core MVC)
- **Tema:** INSPINIA (HTML5 Full Version) Bootstrap tabanlı
- **Veritabanı:** MSSQL (Dapper Micro-ORM)
- **Loglama:** Serilog (Veritabanı Sink)

---

## 🛠️ Kurulum ve Çalıştırma

Projeyi kendi bilgisayarınızda ayağa kaldırmak için aşağıdaki adımları izleyin:

### 1. Veritabanı Kurulumu
Proje, Entity Framework Core Migrations kullanmamaktadır. Bunun yerine saf (raw) SQL kullanılmaktadır.
- SQL Server üzerinde `KilavuzDb` (veya istediğiniz isimde) boş bir veritabanı oluşturun.
- Proje kök dizinindeki `schema.sql` dosyasını açın ve oluşturduğunuz veritabanında çalıştırarak tabloların ve ilk verilerin (Seed) oluşmasını sağlayın.

### 2. Konfigürasyon Ayarları (appsettings.json)
Güvenlik gereği veritabanı bağlantı dizesi kaynak kodda yer almaz. Development ortamında `appsettings.Development.json` veya User Secrets (tavsiye edilen) kullanarak bağlantı dizesini tanımlayın:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=KilavuzDb;User Id=...;Password=...;TrustServerCertificate=True"
  }
}
```

### 3. Projeyi Çalıştırma
Terminal veya komut satırından `Kilavuz.Web` dizinine gidin ve projeyi derleyip çalıştırın:
```bash
dotnet build
dotnet run
```
Proje varsayılan olarak `http://localhost:5193` veya `https://localhost:7193` üzerinde ayağa kalkacaktır.

---

## 👥 Test Kullanıcıları

`schema.sql` çalıştırıldığında sisteme erişim testleri yapabilmeniz için varsayılan bazı kullanıcılar tanımlanmıştır. 

> **ÖNEMLİ:** Gerçek şifreler güvenlik kuralları gereği burada yer almamaktadır. Lütfen test hesaplarına giriş için **"varsayılan test şifresi (ayrıca paylaşılacak)"** bilgisini proje yöneticisinden edininiz.

- **SuperAdmin Hesabı:** Kullanıcı Adı: `admin` (Tüm yetkilere sahiptir)
- **Yetkili Hesabı:** Kullanıcı Adı: `yetkili_1` (Yalnızca kendi oluşturduğu içerikleri yönetebilir)
- **Standart Kullanıcı:** Kullanıcı Adı: `user_1` (Yalnızca kısıtlı kılavuzları okuma yetkisi vardır, panele giremez)

---

## 🧩 Mimari ve Tasarım Desenleri

Projede temiz kod (Clean Code) prensipleri benimsenmiştir:
- **Generic Repository Pattern:** Tüm entity'ler için ortak olan CRUD (Create, Read, Update, Delete) işlemleri tek bir `GenericRepository<T>` sınıfından yönetilmekte ve SQL sorguları dinamik (Dapper) üretilmektedir.
- **Generic Service Pattern:** Veritabanı işlemleri doğrudan Controller katmanından çağrılmaz. Araya giren `GenericService<T>` katmanı ile Soft Delete (silinmiş gibi işaretleme), Sıralama (Orderable) ve Sahip (Ownership) kontrolleri yapılır.
- **ServiceResult Wrapper:** Tüm servis dönüşleri standart bir `ServiceResult<T>` sınıfı üzerinden başarılı/başarısız durum, mesaj ve veri iletilerek yapılır.
- **Dapper Type Handlers:** Enum değerlerinin (Örn: `AccessType.Public`) veritabanına integer yerine "Public" (string) olarak kaydedilmesi ve okunması için özel tip işleyiciler yazılmıştır.

---

## 🔒 Güvenlik Önlemleri

Sistem, OWASP standartları ve kurum gereksinimleri gözetilerek güvenlik testlerinden geçirilmiştir:
1. **Kimlik Doğrulama (Authentication):** Cookie bazlı güvenli (HttpOnly, Secure, SameSite) oturum yönetimi.
2. **Kapsamlı Yetkilendirme (Authorization):** Controller ve Action seviyesinde `[Authorize(Policy="...")]` koruması ve arka planda çalışan "Sahiplik" (Ownership) mantığı (Yetkililerin başka yetkililere ait içerikleri silememesi).
3. **Kısıtlı İçerik (Restricted) Koruması:** `ContentPermissions` tablosu sayesinde özel yetki verilmemiş kullanıcılar, "Kısıtlı" kılavuzları ve bunlara ait ek dosyaları (Attachments) indiremez. (Sunucu tarafı doğrulamalı).
4. **Rate Limiting & CAPTCHA:** Kaba kuvvet (Brute-force) saldırılarına karşı IP+Username bazlı Rate Limit ve arka arkaya hatalı girişlerde devreye giren dinamik (AiGenerated/Math) CAPTCHA mekanizması.
5. **Güvenli Dosya Yükleme:** Sadece belirli uzantılara (MIME type ve File Signature - Magic Number kontrolleri ile) izin verilir. Yüklenen dosyaların adları sistemde Guid ile değiştirilerek depolanır.
6. **XSS & Html Sanitization:** Panel'deki zengin metin (Rich Text) editörden gelen HTML verisi, zararlı betiklere karşı merkezi bir `HtmlSanitizerService` üzerinden temizlenerek (sanitize) veritabanına işlenir ve `Html.Raw` sadece bu güvenli veride kullanılır.
7. **SQL Injection Koruması:** Tüm Dapper sorguları string interpolation yerine `@parametre` yaklaşımı kullanılarak injection riskine karşı kapatılmıştır. Dynamic sıralama parametreleri ise önceden belirlenmiş (whitelist) sütun adlarıyla eşleşmeden sorguya eklenmez.

---
*Bu proje Faz 10 - Cilalama adımı itibarıyla stajyer/geliştirici tarafından teslim edilmek üzere tamamlanmıştır.*
