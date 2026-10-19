# Changelog

## [Unreleased]

### Klavye Makrosu
- Master kontrol kapatıldığında çalışan makro iptal edilir; bekleyen işler de kuyruktan temizlenir. İptal, metin parçaları arasındaki beklemeyi de keser.
- Mouse makro tuşunun bırakılması artık modifier durumundan bağımsız debounce'u sıfırlar.
- Klavye modifier'ları sol/sağ tuş bazında izlenir; bir Ctrl/Shift/Alt tuşunu bırakmak diğer taraf hâlâ basılıyken modifier'ı yanlışlıkla bırakmaz.
- Makro için yutulan aktivasyon tuşunun key-up olayı, modifier'lar değişse veya master kapatılsa bile eşleşmiş şekilde işlenir.

### Performans
- Tıklama iş parçacığının beklemesi durdurma sinyaliyle kesilebilir hale geldi; kapatırken düşük CPS aralıklarının bitmesi beklenmiyor.
- Hassas zamanlamadaki son aktif bekleme aralığı 2 ms'den 1 ms'nin altına düşürüldü; tıklama sırasındaki gereksiz CPU yükü azaltıldı.
- Mouse hook'u artık kullanmadığı hareket, tekerlek ve ana buton olaylarını ayrıştırmadan doğrudan geçiriyor.

### Eklenenler
- GitHub release yayımlandığında Windows x64 uygulamasını ve installer'ı imzalayıp `DualAutoClicker-setup.exe` dosyasını release'e ekleyen CI akışı.
- PFX Base64 ve parola için GitHub Actions secrets; tag/sürüm, sabit installer GUID'i ve imza/zaman damgası kontrolleri.

### Düzeltmeler
- Installer betiği sürümlü Windows SDK klasörlerinde SignTool bulabiliyor ve PFX parolasını `SIGNING_PFX_PASSWORD` ortam değişkeninden okuyabiliyor.
- `--csproj` ile seçilen proje artık publish komutuna da aktarılıyor.

## [3.2.3] - 2026-10-19

### Düzeltmeler

#### Makro Kullanırken Anlık Donma ve Sistem Bipi
- Klavye makrosu artık düşük seviye hook callback'i içinde çalışmıyor; kendi worker thread'inde yürütülüyor. Hook callback sadece karar verip hemen dönüyor, böylece sistem girdi zinciri ve UI thread'i bloklanmıyor.
- Metin enjeksiyonu 4 karakterlik parçalar halinde, aralarda 8 ms bekleyerek yapılıyor. Tek seferde yüzlerce olay gönderilmediği için Windows girdi kuyruğu taşmıyor ve varsayılan bip sesi kesiliyor.
- Enjekte edilen klavye/fare olayları hook callback'inde yok sayılıyor (`LLKHF_INJECTED`, `LLMHF_INJECTED`). Uygulamanın kendi ürettiği girdi callback'e geri dönüp ek yük ve yeniden tetikleme oluşturmuyor.
- `PrecisionClicker.Stop()` artık hook thread'ini bloklamıyor; 100 ms'ye kadar süren `Thread.Join` kaldırıldı. Tıklama thread önceliği `Highest` yerine `AboveNormal`.
- Hedef process adı `Process.GetProcessById` yerine `OpenProcess` + `QueryFullProcessImageName` ile ve PID bazlı 2 saniyelik cache ile çözülüyor. Tıklama başına `Process` nesnesi ayrılmıyor.
- Hook callback'leri try/catch ile korunuyor; bir istisna artık uygulamayı düşürmüyor.
- Hook callback süresi 5 ms'yi aşarsa debug logu düşüyor (tanı için).

#### Modifier Eşleşmesi
- Makro modifier kontrolü tam eşleşmeye çevrildi. `Alt+3` makrosu Ctrl de basılıyken tetiklenmiyor; fazladan modifier hedef uygulamaya sızarak sahte kısayol veya bip üretemiyor.
- Bastırılan tuşlar tek tek izleniyor; bastırılan her keydown'ın keyup'ı da yutuluyor, böylece uygulamada yapışkan Alt/Ctrl kalmıyor.
- Master kapatma, servis durdurma ve yeniden başlatmada bastırma durumu sıfırlanıyor.

#### Sınırlar
- Tek makro çalıştırmasında en fazla 200 karakter enjekte ediliyor; aşan metin kırpılıyor.
- Min/Max rastgele karakter sayısı ve junk karakter listesi güvenli aralıklara kırpılıyor.

#### Kapatma Sırasında Sistem Geneli Fare Takılması
- Uygulama kapatılırken global hook'lar artık ilk iş olarak kaldırılıyor. Hook takılı kaldığı sürece Windows her fare/klavye olayını uygulama thread'inde bekletiyor; bu da kapanıştan sonra birkaç saniye süren fare takılması olarak görünüyordu.
- `App.Shutdown()` artık gerçekten kullanılıyor: pencere kapatma düğmesi, `Closed` olayı ve süreç çıkışı aynı temiz kapanış yolunu izliyor. Önceden `Shutdown()` hiçbir yerden çağrılmıyordu ve kapanışta hook'lar kaldırılmıyordu.
- Kapat düğmesi pencereyi gizlemek yerine uygulamayı kapatıyor. Tray simgesi olmadığı için gizlenen pencereye geri dönüş yolu yoktu.
- `PrecisionClicker.StopAndWait()` eklendi; kapanışta tıklama thread'leri bekleniyor, hook yolunda ise bekleme yok.
- Makro yazımı kapanışta `CancellationToken` ile iptal ediliyor; enjeksiyon döngüsü 8 ms içinde duruyor.
- `SetWindowsHookEx` / `UnhookWindowsHookEx` hataları artık debug loguna yazılıyor.

#### Sistem Yükü
- 1 ms sistem zamanlayıcısı (`timeBeginPeriod(1)`) artık yalnızca tıklama sürerken açık. Önce uygulama açıldığı anda açılıp oturum boyunca kalıyordu; boştaki uygulama sistemden gereksiz zamanlayıcı hassasiyeti istiyordu.
- `PrecisionClicker` bekleme döngüsü her seferinde 1 ms uyuyup uyanmak yerine kalan sürenin çoğunu tek uykuda geçiriyor. Tıklama sırasında saniyede ~1000 gereksiz CPU uyanması ortadan kalktı.

#### Açılış Hızı
- Tek örnek kontrolü WinRT `AppInstance` yerine adlandırılmış mutex ile yapılıyor; açılıştan ~30 ms kazandırıyor.
- Profil düğmelerinin sağ tık menüleri artık açılışta değil, sağ tıklanınca oluşturuluyor. Altı `MenuFlyout`'u önceden kurmak pencere açılışının en pahalı adımlarından biriydi.
- Makro enjeksiyon thread'i ilk makro kullanımında başlatılıyor; uygulama açılışında boşuna thread oluşturulmuyor.
- Yayın (installer) derlemesinde `PublishReadyToRun` açık. Ölçülen kazanç ~50-60 ms; karşılığında yayın boyutu ~160 MB'tan ~203 MB'a çıkıyor. Boyut öncelikliyse bu ayar `DualAutoClicker.csproj` içinden kapatılabilir.

### Teknik Değişiklikler
- `Native/InputSimulator.cs`: parçalı `SendText`, `SendInput` hata kontrolü, process adı cache'i.
- `Native/KeyboardHook.cs`: injected olay filtresi, callback try/catch ve süre ölçümü, `ResetModifierState()`.
- `Native/MouseHook.cs`: injected olay filtresi ve callback try/catch.
- `Native/PrecisionClicker.cs`: run token tabanlı durdurma, bloklamayan `Stop()`.
- `Services/KeyboardMacroService.cs`: kuyruk + worker thread, 30 ms tetikleme gecikmesi, giriş sınırları.
- `Services/ClickerService.cs`: hook içinde enjeksiyon yok, tam modifier eşleşmesi, bastırma seti, kapanışta önce unhook.
- `App.xaml.cs`: tek ve deterministik kapanış yolu; hook'lar süreç kapanmadan önce kaldırılıyor.
- `MainWindow.xaml.cs`: kapat düğmesi uygulamayı kapatıyor, `Closed` olayı temiz kapanışa bağlandı.
- `Native/PrecisionClicker.cs`: `StopAndWait()` ile kapanışta thread'lerin bitmesi garanti.
- `DualAutoClicker.csproj`: SDK 8.0.401 ile derlenebilmesi için `WindowsSdkPackageVersion` sabitlendi; yayında ReadyToRun.

## [3.2.2] - 2026-06-28

### Düzeltmeler

- Çoklu uygulama hedeflemede seçilen process listesi artık doğru eşleştiriliyor.
- Key-binding hook yakalama akışı ortak helper sınıfına taşındı.
- Sessiz hata yakalama blokları debug çıktısı bırakacak şekilde güncellendi.
- Kullanılmayan timer, icon kopyası, event ve window title akışları temizlendi.
- Türkçe UI ve dokümantasyon metinleri hizalandı.

## [3.2.1] - 2026-02-02

### Düzeltmeler

#### Makro Tekrarlı Çalışma Hatası
- Makro aktivasyon tuşu basılı tutulduğunda makronun sürekli tekrar etmesi sorunu giderildi
- Artık makro sadece ilk tuş basılışında bir kez çalışır
- Klavye ve fare aktivasyonları için ayrı durum takibi eklendi (`_macroKeyDown`, `_macroMouseDown`)

#### Modifier Tuş Yutma Eksikliği
- Makro aktivasyonunda modifier tuşlarının (Alt, Shift, Ctrl) diğer uygulamalara iletilmesi sorunu giderildi
- Örneğin `Alt+3` makrosu çalıştırıldığında Alt tuşu artık oyun menüsünü açmıyor
- Modifier durum takibi eklendi (`_suppressAlt`, `_suppressShift`, `_suppressCtrl`)

### Teknik Değişiklikler

#### Değiştirilen Dosyalar
- `Services/ClickerService.cs`:
  - Makro debounce mantığı eklendi (satır 214-233)
  - Modifier tuş bastırma mantığı eklendi (satır 176-212)
  - Yeni durum takip alanları: `_macroKeyDown`, `_macroMouseDown`, `_suppressAlt`, `_suppressShift`, `_suppressCtrl`

## [3.2.0] - 2026-02-01

### Eklenen Özellikler

#### Klavye Makro Sistemi
- **Anti-detection metin makrosu**: Kullanıcı tarafından belirlenen bir metne rastgele "junk" karakterler ekleyerek gönderir
- **Aktivasyon tuşu**: Varsayılan "Yok", kullanıcı istediği tuşu atayabilir
- **Modifier tuş desteği**: `Alt+3`, `Shift+5`, `Ctrl+Alt+F1` gibi kombinasyonlar desteklenir
- **Son 5 karakter kuralı**: Anti-detection için son kullanılan 5 karakter bir sonraki seçimde hariç tutulur
- **Ayarlanabilir parametreler**:
  - Temel metin (örneğin: `3/4 vc`)
  - Minimum/maksimum rastgele karakter sayısı (varsayılan: 3-10)
  - Junk karakter seti (varsayılan: `:;*!'.,:"~`)

#### Tuş Yutma (Key Suppression)
- Makro aktivasyon tuşu basıldığında, tuş diğer uygulamalara iletilmez
- Örneğin `Shift+,` ile makro çalıştırıldığında `;` karakteri yazılmaz

### Düzeltmeler

#### INPUT Struct Alignment Hatası
- 64-bit Windows için INPUT struct union offset'i 4'ten 8'e düzeltildi
- Bu hata mouse click işlemlerinin çalışmamasına neden oluyordu
- `HARDWAREINPUT` struct'ı da eklendi (tam uyumluluk için)

### Teknik Değişiklikler

#### Yeni Dosyalar
- `Services/KeyboardMacroService.cs` - Rastgele metin üretimi ve son-5 kuralı
- `Controls/MacroPanel.xaml` - Makro ayarları UI
- `Controls/MacroPanel.xaml.cs` - Makro panel kod arkası

#### Değiştirilen Dosyalar
- `Models/ClickerSettings.cs`:
  - `KeyboardMacroSettings` sınıfı eklendi
  - `RequireAlt`, `RequireShift`, `RequireCtrl` alanları
  - `FullKeyName` property (örneğin: "Ctrl+Alt+3")

- `Native/KeyboardHook.cs`:
  - Modifier durum takibi (`IsAltDown`, `IsShiftDown`, `IsCtrlDown`)
  - `IsModifierKey()` static metodu
  - `KeyStateChangedWithSuppress` event (tuş yutma desteği)

- `Native/InputSimulator.cs`:
  - INPUT struct 64-bit alignment düzeltmesi
  - `SendText()` metodu (Unicode klavye simülasyonu)
  - `KEYBDINPUT` ve `HARDWAREINPUT` struct'ları

- `Services/ClickerService.cs`:
  - `KeyboardMacroService` entegrasyonu
  - `CheckMacroModifiers()` metodu
  - `OnKeyboardStateChangedWithSuppress()` metodu

- `MainWindow.xaml`:
  - MacroPanel eklendi (clicker panelleri ile settings arasında)

- `MainWindow.xaml.cs`:
  - MacroPanel ayarlarının yüklenmesi

### Örnek Kullanım

1. Makro panelinden "Seç" butonuna tıklayın
2. Modifier tuşları basılı tutarak (örneğin Alt+Shift) ana tuşu basın (örneğin 3)
3. "Alt+Shift+3" olarak kaydedilir
4. Metin kutusuna göndermek istediğiniz metni yazın (örneğin: `3/4 vc`)
5. Oyunda Alt+Shift+3 bastığınızda: `3/4 vc .,;::!` gibi rastgele sonekli metin yazılır
