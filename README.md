# RatePulse

RatePulse is a lightweight exchange-rate widget project.

Current scaffold:

- Windows 11 floating desktop window: `src/RatePulse.Windows`
- Framework: .NET 8 WPF
- SDK location on this machine: `E:\DevTools\dotnet`
- Default currency pairs: USD/CNY, EUR/CNY, JPY/CNY, HKD/CNY, GBP/CNY

## Run the Windows widget

```powershell
E:\DevTools\dotnet\dotnet.exe run --project D:\program\ratepulse\src\RatePulse.Windows\RatePulse.Windows.csproj
```

## Build

```powershell
E:\DevTools\dotnet\dotnet.exe build D:\program\ratepulse\RatePulse.sln
```

## Notes

The first data provider is `open.er-api.com` so the app can work without an API key during early development. For production-grade live financial rates, replace the provider behind `ExchangeRateService` with a paid provider or a private proxy API.
