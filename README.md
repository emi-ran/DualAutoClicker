# 🖱️ Dual AutoClicker

Bağımsız sol ve sağ tık ayarlarına sahip, 6 profil destekli, yüksek performanslı ve modern tasarımlı Windows autoclicker uygulaması.

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)
![Version](https://img.shields.io/badge/Version-3.2.4-orange)

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

1. [Releases](../../releases) sayfasından `DualAutoClicker-setup.exe` dosyasını indirin.
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

Yayın derlemesinde `PublishReadyToRun` açıktır: açılış ~50-60 ms hızlanır, yayın boyutu ~203 MB olur. Boyut öncelikliyse `DualAutoClicker.csproj` içinde bu ayarı kapatın.

### Otomatik imzalı release

`.github/workflows/release.yml`, GitHub'da bir release **yayımlandığında** Windows x64 uygulamasını üretir, uygulama EXE'lerini ve installer'ı SHA-256 + RFC 3161 zaman damgası ile imzalar. Sonuç release'e `DualAutoClicker-setup.exe` olarak eklenir. Yalnız tag push etmek veya release taslağı oluşturmak akışı başlatmaz; yayımlamak gerekir. Başarısız akışı Actions'ta yeniden çalıştırabilir veya `Run workflow` ile yayımlanmış tag'i girerek installer'ı yeniden üretebilirsiniz.

Repo **Settings → Secrets and variables → Actions** altında şu değerler gerekir:

| Tür | Ad | İçerik |
| --- | --- | --- |
| Secret | `SIGNING_PFX_BASE64` | Özel anahtarı içeren PFX/P12 dosyasının Base64 kodlanmış içeriği |
| Secret | `SIGNING_PFX_PASSWORD` | PFX dosyasının parolası |
| Variable | `INSTALLER_APP_ID` | Tüm CI release'lerinde aynı kalacak, süslü parantezsiz kurulum GUID'i |

PFX'i diskte ek bir Base64 kopyası oluşturmadan GitHub'a eklemek için repo kökünde:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes((Resolve-Path '.\Emirhan-Cetinkaya.pfx').Path)) |
    gh secret set SIGNING_PFX_BASE64 --repo emi-ran/DualAutoClicker
gh secret set SIGNING_PFX_PASSWORD --repo emi-ran/DualAutoClicker
```

İkinci komut parolayı gizli olarak sorar. Parolayı komuta, kaynak koduna veya `.env` dosyasına yazmak gerekmez. PFX Base64 değeri GitHub'ın secret başına 48 KB sınırına sığmalıdır. `INSTALLER_APP_ID` bir kez oluşturulur; eski bir installer serisini devam ettiriyorsanız onun `AppId` GUID'ini kullanın. Rastgele GUID'i her release'te değiştirmeyin. CI kurulumu için oluşturulan GUID, eski yerel installer'ların rastgele kimlikleriyle kendiliğinden eşleşmez.

Sürüm çıkarırken `.csproj` içindeki `Version`, README sürümü ve CHANGELOG güncellenmeli; tag örneğin `v3.2.5`, proje sürümü `3.2.5` olmalıdır. Ön sürümler için `v3.2.5-rc.1` ve `3.2.5-rc.1` de desteklenir. Tag ile proje sürümü uyuşmazsa akış durur. Workflow'u içeren commit yeni release tag'inde bulunmalıdır.

CI, sertifikayı runner'ın geçici klasöründe kullanır ve iş bitiminde siler. Eksik secret, imzalama veya imza kontrolü hatasında installer yüklenmez. Self-signed sertifikalarla `NotTrusted` durumu kabul edilir; bu, Windows/SmartScreen güveni sağlandığı anlamına gelmez. Runner'a güvenilen kök sertifika yüklenmez. Yerel imzalamada da parola `SIGNING_PFX_PASSWORD` ortam değişkeninden okunabilir; mevcut `--cert-pass` seçeneği desteklenmeye devam eder.

## 🎮 Kullanım

### Profiller

Uygulamanın üst kısmında 6 profil bulunur. Her profil kendi ayarlarını saklar:

- Profil seçmek için ilgili profile **sol tıklayın**
- Profil adını değiştirmek için **sağ tıklayın**

### Tıklama Ayarları

- **SOL/SAĞ TIK**: İstediğiniz tarafı etkinleştirin.
- **DEĞİŞTİR**: Aktivasyon tuşunu belirleyin (Klavye veya Mouse).
- **MOD**: Makronun basılı tutunca mı yoksa Aç/Kapat (tıkla-başlat/tıkla-durdur) şeklinde mi çalışacağını seçin.
- **CPS & RND**: Tıklama hızını ve rastgelelik oranını belirleyin.

### Metin Makrosu

- **Aktivasyon Tuşu**: Klavye veya mouse ile atanır. Tuşu atarken Alt/Ctrl/Shift basılıysa kombinasyon olarak kaydedilir (`Alt+3` gibi).
- **Tam eşleşme**: Makro yalnızca kaydedilen modifier'ların tamamı basılıyken çalışır. `Alt+3` makrosu Ctrl de basılıyken tetiklenmez; böylece fazladan modifier hedef uygulamaya sızmaz.
- **Metin**: Gönderilecek metin. En fazla 200 karakter enjekte edilir.
- **Min / Max / Karakterler**: Metnin sonuna eklenecek rastgele karakter sayısı ve havuzu. Son kullanılan 5 karakter tekrar seçilmez.
- **Yazım hızı**: Metin 4 karakterlik parçalar halinde, aralarda 8 ms bekleyerek yazılır. Bu, sistem girdi kuyruğunun taşmasını ve Windows'un varsayılan bip sesini engeller.

### Gelişmiş Ayarlar

- **MASTER KONTROL**: Tüm makroları anında devre dışı bırakmak için bir global kısayol atayın. Kapatıldığında çalışan metin makrosu iptal edilir ve sıradaki tetiklemeler silinir.
- **UYGULAMA SEÇ**: Makronun sadece belirli uygulamalarda çalışmasını sağlayın.

### Kapatma

- Sağ üstteki kapat düğmesi uygulamayı tamamen kapatır ve global klavye/fare hook'larını serbest bırakır.
- Arka planda çalıştırmaya devam etmek için pencereyi **küçültün**; kapatmak kısayolları da devre dışı bırakır.

Tıklama zamanlayıcısı CPS hassasiyetini korumak için en fazla 1 ms spin-wait yapar. Kapanırken bekleme sinyalle anında kesilir; mevcut tıklama aralığının bitmesi beklenmez.

Mouse hook'u yalnızca orta ve ek fare tuşlarını işler; fare hareketleri, tekerlek ve sol/sağ tıklama olayları ayrıştırılmadan geçirilir.

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

## ⚠️ Sorumluluk Reddi

Bu yazılım açık kaynak ve eğitim amaçlı bir projedir. Oyunlarda veya diğer platformlarda kullanımından doğabilecek kısıtlamalar veya sorunlardan kullanıcı sorumludur.
