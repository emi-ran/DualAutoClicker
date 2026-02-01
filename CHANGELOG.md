# Changelog

## [3.2.0] - 2026-02-01

### Eklenen Ozellikler

#### Klavye Makro Sistemi
- **Anti-detection metin makrosu**: Kullanici tarafindan belirlenen bir metne rastgele "junk" karakterler ekleyerek gonderir
- **Aktivasyon tusu**: Varsayilan "Yok", kullanici istegi tusu atayabilir
- **Modifier tus destegi**: `Alt+3`, `Shift+5`, `Ctrl+Alt+F1` gibi kombinasyonlar desteklenir
- **Son 5 karakter kurali**: Anti-detection icin son kullanilan 5 karakter bir sonraki secimde haric tutulur
- **Ayarlanabilir parametreler**:
  - Temel metin (ornegin: `3/4 vc`)
  - Minimum/maksimum rastgele karakter sayisi (varsayilan: 3-10)
  - Junk karakter seti (varsayilan: `:;*!'.,:"~`)

#### Tus Yutma (Key Suppression)
- Makro aktivasyon tusu basildiginda, tus diger uygulamalara iletilmez
- Ornegin `Shift+,` ile makro calistirildiginda `;` karakteri yazilmaz

### Duzeltmeler

#### INPUT Struct Alignment Hatasi
- 64-bit Windows icin INPUT struct union offset'i 4'ten 8'e duzeltildi
- Bu hata mouse click islemlerinin calismamastna neden oluyordu
- `HARDWAREINPUT` struct'i da eklendi (tam uyumluluk icin)

### Teknik Degisiklikler

#### Yeni Dosyalar
- `Services/KeyboardMacroService.cs` - Rastgele metin uretimi ve son-5 kurali
- `Controls/MacroPanel.xaml` - Makro ayarlari UI
- `Controls/MacroPanel.xaml.cs` - Makro panel kod arkasi

#### Degistirilen Dosyalar
- `Models/ClickerSettings.cs`:
  - `KeyboardMacroSettings` sinifi eklendi
  - `RequireAlt`, `RequireShift`, `RequireCtrl` alanlari
  - `FullKeyName` property (ornegin: "Ctrl+Alt+3")

- `Native/KeyboardHook.cs`:
  - Modifier durum takibi (`IsAltDown`, `IsShiftDown`, `IsCtrlDown`)
  - `IsModifierKey()` static metodu
  - `KeyStateChangedWithSuppress` event (tus yutma destegi)

- `Native/InputSimulator.cs`:
  - INPUT struct 64-bit alignment duzeltmesi
  - `SendText()` metodu (Unicode klavye simulasyonu)
  - `KEYBDINPUT` ve `HARDWAREINPUT` struct'lari

- `Services/ClickerService.cs`:
  - `KeyboardMacroService` entegrasyonu
  - `CheckMacroModifiers()` metodu
  - `OnKeyboardStateChangedWithSuppress()` metodu

- `MainWindow.xaml`:
  - MacroPanel eklendi (clicker panelleri ile settings arasinda)

- `MainWindow.xaml.cs`:
  - MacroPanel ayarlarinin yuklenmesi

### Ornek Kullanim

1. Makro panelinden "Sec" butonuna tiklayin
2. Modifier tuslari basili tutarak (ornegin Alt+Shift) ana tusu basin (ornegin 3)
3. "Alt+Shift+3" olarak kaydedilir
4. Metin kutusuna gondermek istediginiz metni yazin (ornegin: `3/4 vc`)
5. Oyunda Alt+Shift+3 bastiginizda: `3/4 vc .,;::!` gibi rastgele sonekli metin yazilir
