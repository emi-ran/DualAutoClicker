# 🖱️ Dual AutoClicker

Bağımsız sol ve sağ tık ayarlarına sahip, 6 profil destekli, yüksek performanslı ve modern tasarımlı Windows autoclicker uygulaması.

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)
![Version](https://img.shields.io/badge/Version-3.2.2-orange)

## ✨ Özellikler

- **🎨 Modern UI** - Göz yormayan karanlık tema ve şık tasarım.
- **📁 6 Profil Desteği** - Farklı oyunlar için farklı ayarlar kaydedin, tek tıkla profiller arası geçiş yapın.
- **🎯 Çift Tıklama Desteği** - Sol ve sağ tık için tamamen bağımsız ayarlar.
- **⌨️ Esnek Tuş Atama** - Mouse butonları (MB3, MB4, MB5) veya klavye tuşları ile tam uyum.
- **🎚️ Hassas CPS Kontrolü** - 1-100 CPS arasında mikrosaniye düzeyinde doğruluk.
- **🎲 Rastgelelik (Rnd)** - Tık aralıklarına %0-30 arası varyasyon ekler.
- **⏸️ Master Kontrol** - Tek bir tuşla tüm sistemi anında donduran acil durum anahtarı.
- **🪟 Uygulama Hedefleme** - Tıklayıcının sadece seçtiğiniz uygulama penceresinde çalışmasını sağlar.
- **🚀 Windows Başlangıcı** - Windows açıldığında otomatik olarak arka planda başlar.

## 📥 Kurulum

### Gereksinimler

- Windows 10 veya 11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Inno Setup 6 (installer üretmek için)
- Windows SDK (signtool ile imzalama için)

### Hızlı Başlat

1. [Releases](../../releases) sayfasından `DualAutoClicker-Setup.exe` dosyasını indirin.
2. Kurulumu başlatın ve isteğe bağlı masaüstü kısayolu seçin.
3. Kurulumdan sonra uygulamayı başlatın.

### Geliştirici (Installer)

```powershell
python build_installer.py
```

İmzalı kurulum üretmek için:

```powershell
python build_installer.py --cert Cert.pfx --cert-pass Certpass
```

Bu komut:

- `dotnet publish` ile `publish` klasörünü üretir.
- `installer.iss` dosyasını `.csproj` içinden otomatik oluşturur.
- Inno Setup ile `DualAutoClicker-Setup.exe` kurulum dosyasını üretir.
- Sertifika verildiyse yayınlanan exe ve kurulum dosyasını imzalar.

## 🎮 Kullanım

### Profiller

Uygulamanın üst kısmında 6 profil bulunur. Her profil kendi ayarlarını saklar:

- Profil seçmek için ilgili profile **sol tıklayın**
- Profil adını değiştirmek için **sağ tıklayın**

### Makro Ayarları

- **SOL/SAĞ TIK**: İstediğiniz tarafı etkinleştirin.
- **DEĞİŞTİR**: Aktivasyon tuşunu belirleyin (Klavye veya Mouse).
- **MOD**: Makronun basılı tutunca mı yoksa Aç/Kapat (tıkla-başlat/tıkla-durdur) şeklinde mi çalışacağını seçin.
- **CPS & RND**: Tıklama hızını ve rastgelelik oranını belirleyin.

### Gelişmiş Ayarlar

- **MASTER KONTROL**: Tüm makroları anında devre dışı bırakmak için bir global kısayol atayın.
- **UYGULAMA SEÇ**: Makronun sadece belirli uygulamalarda çalışmasını sağlayın.

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

## ⚠️ Sorumluluk Reddi

Bu yazılım açık kaynak ve eğitim amaçlı bir projedir. Oyunlarda veya diğer platformlarda kullanımından doğabilecek kısıtlamalar veya sorunlardan kullanıcı sorumludur.
