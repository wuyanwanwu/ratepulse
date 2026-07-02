# RatePulse

RatePulse is a lightweight exchange-rate widget project.

Current scaffold:

- Windows 11 floating desktop window: `src/RatePulse.Windows`
- Framework: .NET 8 WPF
- SDK location on this machine: `E:\DevTools\dotnet`
- Default watchlist pairs: USD/CNY, USD/EUR, USD/JPY, USD/HKD, USD/GBP, USD/AUD, USD/CAD, USD/CHF, USD/SGD, USD/TRY

Windows v1 features:

- Always-on-top floating widget window
- USD bridge converter for one source amount and one target currency, defaulting to CNY -> USD
- Editable currency dropdowns with common choices and manual three-letter fallback
- USD-based watchlist cards with click-to-open 15-day history chart popups
- English/Chinese UI selection with Chinese-English currency labels
- Editable USD watchlist and refresh interval
- Local settings saved under `%LOCALAPPDATA%\RatePulse`
- Local rate and chart cache for startup/offline display
- Minimize-to-tray and tray restore/exit menu

## Run the Windows widget

Published exe:

```text
D:\program\ratepulse\release\win-x64\RatePulse.Windows.exe
```

Double-click:

```text
D:\program\ratepulse\Start-RatePulse.bat
```

Create or update the published exe:

```text
D:\program\ratepulse\Publish-RatePulse.bat
```

Command line:

```powershell
E:\DevTools\dotnet\dotnet.exe run --project D:\program\ratepulse\src\RatePulse.Windows\RatePulse.Windows.csproj
```

## Build

```powershell
E:\DevTools\dotnet\dotnet.exe build D:\program\ratepulse\RatePulse.sln
```

## Notes

Latest rates and the USD bridge converter use `open.er-api.com` so the app can work without an API key during early development. The 15-day watchlist chart uses `api.frankfurter.dev` because it provides date-range history. For production-grade live financial rates, replace the provider behind `ExchangeRateService` with a paid provider or a private proxy API.
