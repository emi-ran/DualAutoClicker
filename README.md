# 🖱️ Dual AutoClicker

Bağımsız sol ve sağ tık ayarlarına sahip, yüksek performanslı Windows autoclicker uygulaması.

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Özellikler

- **🎯 Çift Tıklama Desteği** - Sol ve sağ tık için bağımsız ayarlar
- **⌨️ Esnek Tuş Atama** - Mouse butonları (MB3, MB4, MB5) veya klavye tuşları
- **🎚️ Ayarlanabilir CPS** - 1-100 tık/saniye arasında hassas kontrol
- **🔄 İki Mod** - Basılı tut veya Toggle
- **💾 Ayar Kaydetme** - Ayarlar otomatik olarak kaydedilir
- **📌 Sistem Tepsisi** - Küçültüldüğünde tepsiye gider

## 📥 Kurulum

### Gereksinimler

- Windows 10/11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### İndirme

1. [Releases](../../releases) sayfasından son sürümü indir
2. `DualAutoClicker.exe` dosyasını çalıştır

### Kaynak Koddan Derleme

```bash
git clone https://github.com/kullanici/DualAutoClicker.git
cd DualAutoClicker
dotnet publish -c Release -r win-x64 -o ./publish
```

## 🎮 Kullanım

1. Uygulamayı başlat
2. Sol/Sağ tık panellerinden:
   - **Aktif** - Tıklayıcıyı aç/kapat
   - **Seç** - Aktivasyon tuşunu belirle
   - **Mod** - Basılı tut veya Toggle seç
   - **CPS** - Saniyedeki tıklama sayısını ayarla
3. Aktivasyon tuşuna bas ve tıklamaya başla!

### Tuş Atama

- **Seç** butonuna tıkla
- İstediğin tuşa veya mouse butonuna bas
- **ESC** ile iptal et

## ⚙️ Ayarlar

Ayarlar otomatik olarak şu konumda saklanır:

```
%LOCALAPPDATA%\DualAutoClicker\settings.json
```

## 🛠️ Geliştirme

```bash
# Geliştirme modunda çalıştır
dotnet run

# Release build
dotnet build -c Release

# Tek dosya olarak yayınla
dotnet publish -c Release -r win-x64 -o ./publish
```

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

## ⚠️ Sorumluluk Reddi

Bu yazılım eğitim amaçlıdır. Oyunlarda veya diğer uygulamalarda haksız avantaj sağlamak amacıyla kullanılması kullanıcının sorumluluğundadır.
