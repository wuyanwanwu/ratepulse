# RatePulse Architecture

## Product shape

RatePulse has two target surfaces:

- Windows: an always-on-top floating desktop window for Windows 11.
- Android: a native home-screen App Widget, planned after the Windows proof of concept.

## Windows v1

The Windows app is a .NET 8 WPF application. It uses a transparent, borderless, resizable window with topmost behavior and manual drag handling.

The exchange-rate code is isolated in `ExchangeRateService` so the provider can be swapped without changing UI behavior.

Settings and cache data are stored under `%LOCALAPPDATA%\RatePulse`.

- `settings.json` stores currency pairs, refresh interval, topmost state, and window placement.
- `settings.json` also stores the last converter amount, source currency, and target currency.
- `settings.json` stores the selected UI language. Chinese mode renders currencies as Chinese-English labels, such as `人民币 (CNY)`.
- `rate-cache.json` stores the last successful quote set and USD bridge conversion so the widget can render cached data on startup or when offline.

The converter always uses USD as the bridge currency. For example, `1000 CNY` to `JPY` is calculated as `1000 / USD_CNY * USD_JPY`.

## Android follow-up

The Android widget should be implemented as a native Kotlin App Widget. It should reuse the same currency-pair defaults and API/provider contract, but not share WPF UI code.
