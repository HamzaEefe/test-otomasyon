# Test Otomasyon

ASP.NET Core MVC ile geliştirilmiş, **rol bazlı yetkilendirme** kullanan, **çok dilli (TR/EN)** bir kurumsal görev yönetim sistemi.

## Özellikler

- 🔐 Rol ve yetki tabanlı erişim kontrolü (claims-based authorization)
- 🌳 Hiyerarşik organizasyon şeması (toplanabilir ağaç + arama)
- 📋 Kanban tabanlı görev panosu (Onay Bekleyen / Atandı / Devam Eden / Tamamlanan / Geçmiş)
- ✅ Onay – red akışı, iş bildirme ve takip
- 💬 İç mesajlaşma + e-posta entegrasyonu
- 🌐 Çok dilli arayüz (Türkçe / İngilizce, runtime culture switch)
- 🔑 BCrypt şifre hash, cookie tabanlı kimlik doğrulama
- 🌱 İdempotent veri seed'i (100 demo kullanıcı + departman + rol)

## Kullanılan Teknolojiler

- **Backend:** ASP.NET Core MVC (.NET 8), C#
- **Veritabanı:** SQL Server, Dapper
- **Frontend:** Razor, Bootstrap 5, HTML5 / CSS3, JavaScript
- **Güvenlik:** BCrypt.Net, Cookie Authentication, Claims-based Authorization
- **Lokalizasyon:** .resx + DataAnnotation Localization

## Kurulum

### Gerekli olanlar
- .NET 8 SDK
- SQL Server (LocalDB veya SQL Express yeterli)
- Visual Studio 2022 / Rider / VS Code

### Adımlar

1. Depoyu klonla:
   ```bash
   git clone https://github.com/KULLANICI_ADIN/test-otomasyon.git
   cd test-otomasyon
   ```

2. `appsettings.Development.json` dosyasını oluştur (kök dizinde):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.\\SQLEXPRESS;Database=TestOtomasyonDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
     },
     "Email": {
       "Host": "smtp.gmail.com",
       "Port": 587,
       "UseSsl": true,
       "Username": "your-email@gmail.com",
       "Password": "your-app-password",
       "FromEmail": "your-email@gmail.com",
       "FromName": "Test Otomasyon"
     }
   }
   ```

3. Bağımlılıkları yükle ve çalıştır:
   ```bash
   dotnet restore
   dotnet run
   ```

4. Tarayıcıdan `https://localhost:7xxx` (konsoldaki port) adresine git.

### Varsayılan giriş

- **Kullanıcı adı:** `admin`
- **Şifre:** `Admin123!`

İlk açılışta otomatik olarak 100 demo kullanıcı, 10 departman ve 3 rol oluşturulur. Demo kullanıcı şifreleri: `Test123!`,demo kullanıcı adları için: İsimX --> 
X= soyadın ilk harfi

## Lisans

Eğitim amaçlı staj projesidir.
