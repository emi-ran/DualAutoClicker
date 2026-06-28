# Polish Durum Raporu

Bu dosya, yapılan polish çalışmasının güncel durumunu özetler.

## Tamamlananlar

- [x] Çoklu uygulama hedefleme düzeltildi
  Konum: `Controls/SettingsPanel.xaml.cs`, `Services/ClickerService.cs`, `Native/InputSimulator.cs`
  Sonuç: Seçilen process adları virgülle ayrılmış listeden okunuyor ve foreground process adı liste içinde aranıyor.

- [x] Sessiz hata yakalama blokları görünür hale getirildi
  Konum: `Services/SettingsService.cs`, `Services/StartupService.cs`, `MainWindow.xaml.cs`, `Native/WindowEnumerator.cs`, `Native/InputSimulator.cs`
  Sonuç: Hatalar artık `Debug.WriteLine(...)` ile izlenebilir.

- [x] Tekrar eden key-binding akışı ortaklaştırıldı
  Konum: `Controls/KeyBindingCapture.cs`, `Controls/ClickerPanel.xaml.cs`, `Controls/MacroPanel.xaml.cs`, `Controls/SettingsPanel.xaml.cs`
  Sonuç: Mouse/klavye hook kurma ve kapatma akışı tek helper sınıfından yönetiliyor.

- [x] Key-binding lifecycle netleştirildi
  Konum: `Controls/KeyBindingCapture.cs`, `Controls/ClickerPanel.xaml.cs`, `Controls/MacroPanel.xaml.cs`, `Controls/SettingsPanel.xaml.cs`
  Sonuç: `Unloaded` sırasında helper dispose edilmiyor, sadece hook yakalama durduruluyor. Aynı control instance tekrar yüklenirse capture yeniden başlatılabilir.

- [x] Türkçe UI ve dokümantasyon metinleri hizalandı
  Konum: `Controls/ClickerPanel.xaml`, `README.md`, `CHANGELOG.md`
  Sonuç: Kullanıcıya görünen `Toggle` metni `Aç/Kapat` oldu; kalan Türkçe metinler yerel karakterlerle temizlendi.

- [x] README sürümü güncellendi
  Konum: `README.md`
  Sonuç: Badge sürümü proje sürümüyle `3.2.1` olarak hizalandı.

- [x] Kullanılmayan icon kopyası kaldırıldı
  Konum: `Resources/icon.ico`
  Sonuç: Uygulama ve installer tek icon kaynağı olarak `Assets/icon.ico` kullanıyor.

- [x] Ölü kod temizlendi
  Konum: `Native/MultimediaTimer.cs`, `MainWindow.xaml.cs`, `Services/ClickerService.cs`, `Native/InputSimulator.cs`
  Sonuç: Kullanılmayan timer sınıfı, kullanılmayan hook alanları, kullanılmayan click eventleri ve kullanılmayan P/Invoke kaldırıldı.

- [x] Window target modeli sadeleştirildi
  Konum: `Models/ClickerSettings.cs`, `Services/ClickerService.cs`, `Native/InputSimulator.cs`
  Sonuç: UI tarafından beslenmeyen `WindowTitle` akışı kaldırıldı.

## Doğrulama

- [x] Debug build başarılı: `dotnet build "DualAutoClicker.sln" -c Debug`
- [x] Release build başarılı: `dotnet build "DualAutoClicker.sln" -c Release`

## Kalan Notlar

- [ ] Manuel smoke test önerilir: key binding, makro modifier yakalama, master tuş atama, çoklu uygulama hedefleme.
