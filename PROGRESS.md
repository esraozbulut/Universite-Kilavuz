# PROGRESS.md — Proje İlerleme Takibi

> Bu dosya **canlı bir durum belgesidir** ve `WORKFLOW.md`'deki fazlara paralel olarak her oturum sonunda güncellenmelidir. Gemini, her yeni oturuma başlarken bu dosyayı okuyarak kaldığı yeri tespit eder (bkz. `WORKFLOW.md` Bölüm 3).
>
> **Güncelleme kuralı:** Bu dosyadaki değişiklikler de (diğer dosya değişiklikleri gibi) kullanıcı onayına tabidir — Gemini bir fazı kendiliğinden "tamamlandı" işaretleyemez; kullanıcının onayı üzerine işaretler.

---

## Son Güncelleme

- **Tarih:** 2026-08-05
- **Güncelleyen:** Gemini Oturumu

---

## Genel Durum Özeti

**Şu an aktif faz:** Faz 5 — Infrastructure Katmanı (Devam Ediyor)

**Sıradaki somut adım:** Faz 5 kapsamında Serilog/hata yönetimi alt-adımı tamamlandı. Sırada CAPTCHA gözden geçirmesi, Rate Limiting testi, FileStorage, HtmlSanitizer ve AuthenticationProvider adımları bulunmaktadır.

---

## Faz Durum Tablosu

| Faz | Açıklama | Durum | Not |
|---|---|---|---|
| 0 | Proje Kurulumu (Bootstrap) | ✅ Tamamlandı | Veritabanı oluşturuldu, altyapı hazırlandı. |
| 1 | INSPINIA Tema Entegrasyonu | ✅ Tamamlandı | Statik dosyalar eklendi, Razor view iskeletleri oluşturuldu. |
| 2 | Domain Katmanı | ✅ Tamamlandı | Temel POCO sınıfları, arayüzler ve enumlar oluşturuldu. |
| 3 | Data Katmanı (Dapper + Generic Repository) | ✅ Tamamlandı | GenericRepository test edildi ve PRD uyumu onaylandı. |
| 4 | Application Katmanı (Generic Service) | ✅ Tamamlandı | IResourceOwnershipPolicy ve IReorderService entegre edildi, rol bazlı yetkiler doğrulandı. |
| 5 | Infrastructure Katmanı | 🔄 Devam Ediyor | Serilog/Hata Yönetimi alt-adımı tamamlandı. Captcha, Rate Limit, vb. devam ediyor. |
| 6 | Kimlik Doğrulama ve Yetkilendirme | ⬜ Beklemede | |
| 7 | Panel Modülleri | ⬜ Beklemede | |
| 8 | UI (Public) Modülleri | ⬜ Beklemede | |
| 9 | Güvenlik Sıkılaştırma ve Doğrulama | ⬜ Beklemede | |
| 10 | Cilalama (Polish) | ⬜ Beklemede | |

**Durum değerleri:** ⬜ Beklemede · 🔄 Devam Ediyor · ✅ Tamamlandı · ⏸️ Duraklatıldı (sebep notta belirtilir)

---

## Faz 7 — Panel Modülleri Alt Kırılımı

*(Faz 7 birden fazla alt adımdan oluştuğu için ayrıca takip edilir — bkz. `WORKFLOW.md` Faz 7)*

- [ ] Kullanıcı/Rol görüntüleme (temel)
- [ ] Uygulama Yönetimi (CRUD + sıralama + erişim tipi)
- [ ] Kategori Yönetimi (CRUD + sıralama)
- [ ] Sayfa Yönetimi (CRUD + Rich Text + dosya/görsel yükleme + sıralama + erişim tipi)
- [ ] Erişim/İzin yönetimi ekranı
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

---

## Açık Konular / Netleşmemiş Noktalar

*(`PRD.md`'de "ileride netleşecek" olarak bırakılan veya proje sırasında ortaya çıkan, henüz karara bağlanmamış konular burada listelenir.)*

- Kurumun mail tabanlı kimlik doğrulama sisteminin protokolü/altyapısı henüz belirsiz (bkz. `PRD.md` Bölüm 4, Aşama 2) — `InstitutionalAuthProvider` bu netleştiğinde tasarlanacak.
- Geliştirmede kullanılacak kesin .NET sürümü teyit edilmeli (PRD'de "en güncel LTS" olarak bırakıldı).
- **SuperAdmin Seed Data:** `schema.sql` ile veritabanı kurulduğunda sadece varsayılan Roller eklenmiştir. İlk giriş yapacak "SuperAdmin" kullanıcısı henüz sistemde yoktur. Faz 6'da (Kimlik Doğrulama) şifre hash'leme (Password Hashing) altyapısı kurulduktan sonra, ilk SuperAdmin kullanıcısı seed/script ile eklenecektir.
- **AuditLogs Tablosu (Serilog):** Faz 5'te Serilog yapılandırması yapılırken `columnOptionsSection` ile `UserId`, `IPAddress` ve `RequestPath` sütunlarının ayrı sütun olarak tanımlanması gerekiyor. (Varsayılan `autoCreateSqlTable` şeması, sonradan Panel üzerinden kullanıcı bazlı filtrelemeyi desteklemez, standart log sütunları ile tabloyu oluşturur).
- **Yetkili İzinleri Sınırlaması (Faz 7'ye ertelendi):** Yetkili rolündeki bir kullanıcı, `PageAttachment` ve `ContentPermission` gibi `IAuditable` olmayan (Yani `CreatedByUserId` tutmayan) kaynaklarda şu an hiçbir zaman değişiklik yapamıyor (varsayılan red); bu durum Faz 7'de (Panel modülleri) ele alınmalı. Örneğin `PageAttachment` için silme/ekleme izni verilirken üst `Page` entity'sinin sahipliği kontrol edilerek izin verilebilir.

---

## Bilinen Riskler / Dikkat Edilecekler

*(Faz ilerledikçe ortaya çıkan, unutulmaması gereken teknik notlar buraya eklenir.)*

- **Infrastructure Şablon Kodları:** `Infrastructure/Captcha/AiGeneratedCaptchaProvider.cs` dosyası projeye Faz 0 şablonuyla birlikte önceden gelmiş, şu an atıl durumda. Faz 5'e gelindiğinde bu dosyanın PRD.md Bölüm 5 ile tam uyumlu olup olmadığı sıfırdan gözden geçirilmeli; ayrıca CS0618 (kullanımdan kaldırılmış metot) uyarısı o aşamada çözülmeli.

---

*Bu dosyanın amacı, oturumlar arası bağlamı korumaktır. Boş/eksik bırakılan bir bölüm, o konunun henüz ele alınmadığı anlamına gelir — "tamamlandı" varsayılmaz.*
