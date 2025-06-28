# 📚 OnlineBookShop

Bu proje bir online kitaplık uygulamasıdır. Kullanıcılar kitapları görüntüleyebilir, beğenebilir, sepete ekleyebilir, puan verebilir ve satın alma işlemleri gerçekleştirebilir. Giriş/kayıt işlemleri JWT ile güvence altına alınmıştır.

## 🚀 Teknolojiler

- Backend: ASP.NET Core (.NET 8)
- Veritabanı: PostgreSQL
- Frontend: HTML, CSS, JavaScript
- Kimlik Doğrulama: JWT
- ORM: Npgsql (native SQL sorgular)
- Dağıtım: (lokal geliştirme)

---

## 🔐 Özellikler

### 👤 Kullanıcı İşlemleri
- Kayıt ol / Giriş yap (JWT token ile)
- Şifre değiştirme
- Profil görüntüleme ve hesap silme
- Profil resmi görüntüleme

### 📘 Kitaplık
- Tüm kitapları listeleme, türe göre filtreleme
- Kitap puanlama (1–5 yıldız)
- Kitap başına ortalama puan görüntüleme
- XSS korunmalı kitap adı ve açıklama gösterimi

### ❤️ Beğeni Sistemi
- Kullanıcı bazlı kitap beğenme ve geri alma
- Beğenilen kitaplar `begeniler.html` sayfasında listelenir

### 🛒 Sepet
- Sepete kitap ekleme ve silme
- Sepeti sadece ilgili kullanıcı görebilir
- Tür filtresi ve toplam ücret gösterimi
- CSRF'den korunmalı ödeme işlemi (sadece frontend)

### 💳 Satın Alma
- Seçilen kitaplar için ödeme yapılır
- `siparisler` tablosuna kayıt düşülür
- Kullanıcının "Siparişlerim" kısmında gösterilir

---

## 📁 Dosya Yapısı

```
/OnlineBookShop
├── Models/
├── Services/
├── Endpoints/
├── Program.cs
├── appsettings.json
/frontend/
├── kitaplik.html
├── begeniler.html
├── sepet.html
├── profil.html
├── index.html (giriş)
├── register.html
```

---

## 🛠️ Gereksinimler

- .NET 8 SDK
- PostgreSQL 13+
- Tarayıcı (Chrome, Edge, Firefox önerilir)

---

## 🧪 Güvenlik Önlemleri

- ✅ SQL Injection: Parametreli sorgular
- ✅ XSS: `innerHTML` yerine `textContent` kullanımı
- ✅ CSRF: Token bazlı oturum ile sahte istek koruması
- ✅ Kimlik Doğrulama: JWT token ile

---

## 📦 API Endpoint Örnekleri

| Endpoint                     | Açıklama                    |
|-----------------------------|-----------------------------|
| POST `/api/register`        | Yeni kullanıcı oluşturur    |
| POST `/api/login`           | JWT token döner             |
| GET `/api/kitaplar`         | Kitap listesi               |
| POST `/api/begeniler`       | Kitap beğenme               |
| DELETE `/api/sepet/...`     | Sepetten kitap silme        |
| POST `/api/satinalma`       | Satın alma işlemi başlatır  |

---

## 👨‍💻 Geliştirici

Feyza Adamhasan

---
