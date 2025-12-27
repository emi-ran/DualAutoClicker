# 🖱️ Dual AutoClicker

Bağımsız sol ve sağ tık ayarlarına sahip, yüksek performanslı ve modern tasarımlı Windows autoclicker uygulaması.

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Özellikler

- **Modern UI** - Göz yormayan karanlık tema ve şık tasarım.
- **🎯 Çift Tıklama Desteği** - Sol ve sağ tık için tamamen bağımsız konfigürasyon.
- **⌨️ Esnek Tuş Atama** - Mouse butonları (MB3, MB4, MB5) veya klavye tuşları ile tam uyum.
- **🎚️ Hassas CPS Kontrolü** - 1-100 CPS arasında mikrosaniye düzeyinde doğruluk.
- **🎲 Rastgelelik (Rnd)** - Anti-cheat sistemlerini atlatmak için tık aralıklarına %0-30 arası varyasyon ekler.
- **⏸️ Master Kontrol** - Tek bir tuşla (örn: F8) tüm sistemi anında donduran acil durum anahtarı.
- **🪟 Uygulama Hedefleme** - Tıklayıcının sadece seçtiğiniz uygulama penceresinde çalışmasını sağlar.
- **🚀 Windows Başlangıcı** - Windows açıldığında otomatik olarak arka planda başlar.
- **📌 Akıllı Tray Sistemi** - Küçültüldüğünde tepsisinde çalışır, ikonu makro durumuna göre renk değiştirir (Yeşil: Aktif, Mavi: Beklemede, Gri: Kapalı).

## 📥 Kurulum

### Gereksinimler

- Windows 10 veya 11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Hızlı Başlat

1. [Releases](../../releases) sayfasından son güncel `exe` dosyasını indir.
2. Uygulamayı çalıştır. Ayarlarınız her seferinde otomatik olarak kaydedilir.

## 🎮 Kullanım

1. **Makro Ayarları**:

   - **SOL/SAĞ TIK**: İstediğiniz tarafı etkinleştirin.
   - **SEÇ**: Aktivasyon tuşunu belirleyin (Klavye veya Mouse).
   - **MOD**: Makronun basılı tutunca mı yoksa tıkla-başlat tıkla-durdur (Toggle) şeklinde mi çalışacağını seçin.
   - **CPS & RND**: Tıklama hızını ve rastgelelik oranını belirleyin.

2. **Gelişmiş Ayarlar**:

   - **MASTER KONTROL**: Tüm makroları anında devre dışı bırakmak için bir global kısayol atayın.
   - **UYGULAMA HEDEFLE**: "PENCERE SEÇ" butonu ile makronun sadece o oyunda/programda çalışmasını sağlayın.

3. **Sistem Tepsisi**:
   - Uygulama küçültüldüğünde saatin yanına gider.
   - İkon Rengi **Yeşil** ise makro o an tıklama yapıyordur.
   - İkon Rengi **Mavi** ise makro hazır ama tıklama yapılmıyordur.

## 🛠️ Teknik Detaylar

- **Hassasiyet**: `Stopwatch` ve `PrecisionClicker` motoru ile standart Windows timer limitlerini aşan hassasiyet.
- **Sistem Kaynakları**: Minimum CPU ve RAM kullanımı için optimize edilmiştir.
- **Yayınlama**: Framework-dependent single-file olarak yayınlanmış, taşınabilir ve hafiftir.

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

## ⚠️ Sorumluluk Reddi

Bu yazılım açık kaynak ve eğitim amaçlı bir projedir. Oyunlarda veya diğer platformlarda kullanımından doğabilecek kısıtlamalar veya sorunlardan kullanıcı sorumludur.
