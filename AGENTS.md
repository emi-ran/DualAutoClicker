# AGENTS

## Repository overview
- App: Dual AutoClicker (WinUI 3, .NET 8, Windows only)
- UI: XAML in `MainWindow.xaml` and `Controls/` + `Views/`
- Core logic: `Services/`, `Native/`, `Models/`
- Installer automation: `build_installer.py` generates `installer.iss`

## Build / run / test

### Build
- `dotnet build DualAutoClicker.sln -c Debug`
- `dotnet build DualAutoClicker.sln -c Release`

### Run
- `dotnet run --project DualAutoClicker.csproj -c Debug`

### Publish
- `dotnet publish DualAutoClicker.csproj -c Release -r win-x64 --self-contained true`

### Installer (Inno Setup)
- `python build_installer.py`
- Signed installer:
  - `python build_installer.py --cert Cert.pfx --cert-pass "<PASSWORD>"`

### Tests
- There are no automated tests in this repository.
- If tests are added later, prefer `dotnet test <sln/csproj>` for full runs.
- Single test (future): `dotnet test <csproj> --filter FullyQualifiedName~Namespace.Class.Method`

## Lint / format
- No configured linter/formatter found.
- If you add formatting later, prefer `dotnet format` and keep changes minimal.

## Code style guidelines

### C# language usage
- Use C# 10+ with nullable reference types enabled.
- Prefer explicit types when the type is not obvious; `var` is OK for obvious types.
- Use expression-bodied members sparingly; prefer readability.
- Avoid one-letter names except for loop indices.

### Naming
- PascalCase for public types and members.
- camelCase for private fields (prefix underscore) and locals.
- Boolean fields/properties: use `is`/`has`/`can` prefix where natural.
- Event handlers: `<Control>_<Event>` (e.g., `MasterKeyButton_Click`).

### Imports
- Keep `using` directives at the top of the file.
- Use `System.*` first, then external packages, then project namespaces.
- Remove unused `using` directives.

### XAML
- Keep XAML attributes aligned and grouped by function (layout, style, behavior).
- Prefer resource brushes/colors from `Styles/Colors.xaml` and `Styles/Controls.xaml`.
- Keep UI text Turkish unless the existing text is English for a reason.

### UI state updates
- UI changes should be marshaled to UI thread with `DispatcherQueue.TryEnqueue`.
- Keep UI update methods side-effect free beyond UI state.

### Error handling
- Prefer early returns with clear user-facing messages.
- Use `try/catch` around external process calls (`subprocess` in Python, `Process` in C#).
- Use specific exception types where possible.

### Services & settings
- Update settings via `SettingsService` and call `_settingsService.Save()` after changes.
- Keep state sync between UI and `ClickerService` (`MasterStateChanged` event).

### Native integrations
- Be careful with global hooks in `Native/` (`MouseHook`, `KeyboardHook`).
- Always install/uninstall hooks symmetrically and dispose when done.

## Architectural notes
- `ClickerService` owns global input hooks and master state.
- `SettingsPanel` updates UI based on `SettingsService` and `ClickerService` state.
- `WindowPickerDialog` is a dynamic dialog (no XAML file).

## Common pitfalls
- When using `build_installer.py`, it overwrites `installer.iss`.
- Ensure installer signing happens before compiling `installer.iss`.
- Avoid blocking UI thread with long-running tasks.

## Files to update for common changes
- Installer behavior: `build_installer.py` and `installer.iss` (generated).
- UI theme/colors: `Styles/Colors.xaml`, `Styles/Controls.xaml`.
- Master toggle behavior: `Services/ClickerService.cs`, `Controls/SettingsPanel.xaml.cs`.
