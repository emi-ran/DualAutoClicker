# Changelog

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
