# PROGRESS.md — Proje İlerleme Takibi

> Bu dosya **canlı bir durum belgesidir** ve `WORKFLOW.md`'deki fazlara paralel olarak her oturum sonunda güncellenmelidir. Gemini, her yeni oturuma başlarken bu dosyayı okuyarak kaldığı yeri tespit eder (bkz. `WORKFLOW.md` Bölüm 3).
>
> **Güncelleme kuralı:** Bu dosyadaki değişiklikler de (diğer dosya değişiklikleri gibi) kullanıcı onayına tabidir — Gemini bir fazı kendiliğinden "tamamlandı" işaretleyemez; kullanıcının onayı üzerine işaretler.

---

## Son Güncelleme

- **Tarih:** _(henüz başlanmadı)_
- **Güncelleyen:** _(kullanıcı / Gemini oturumu)_

---

## Genel Durum Özeti

**Şu an aktif faz:** Faz 0 — Proje Kurulumu (henüz başlanmadı)

**Sıradaki somut adım:** _(ör. "Solution ve klasör yapısının oluşturulması için onay bekleniyor")_

---

## Faz Durum Tablosu

| Faz | Açıklama | Durum | Not |
|---|---|---|---|
| 0 | Proje Kurulumu (Bootstrap) | ⬜ Beklemede | |
| 1 | INSPINIA Tema Entegrasyonu | ⬜ Beklemede | |
| 2 | Domain Katmanı | ⬜ Beklemede | |
| 3 | Data Katmanı (Dapper + Generic Repository) | ⬜ Beklemede | |
| 4 | Application Katmanı (Generic Service) | ⬜ Beklemede | |
| 5 | Infrastructure Katmanı | ⬜ Beklemede | |
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

- _(henüz kayıt yok)_

---

## Açık Konular / Netleşmemiş Noktalar

*(`PRD.md`'de "ileride netleşecek" olarak bırakılan veya proje sırasında ortaya çıkan, henüz karara bağlanmamış konular burada listelenir.)*

- Kurumun mail tabanlı kimlik doğrulama sisteminin protokolü/altyapısı henüz belirsiz (bkz. `PRD.md` Bölüm 4, Aşama 2) — `InstitutionalAuthProvider` bu netleştiğinde tasarlanacak.
- Geliştirmede kullanılacak kesin .NET sürümü teyit edilmeli (PRD'de "en güncel LTS" olarak bırakıldı).

---

## Bilinen Riskler / Dikkat Edilecekler

*(Faz ilerledikçe ortaya çıkan, unutulmaması gereken teknik notlar buraya eklenir.)*

- _(henüz kayıt yok)_

---

*Bu dosyanın amacı, oturumlar arası bağlamı korumaktır. Boş/eksik bırakılan bir bölüm, o konunun henüz ele alınmadığı anlamına gelir — "tamamlandı" varsayılmaz.*
