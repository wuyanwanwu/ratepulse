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
- `rate-cache.json` stores the last successful quote set so the widget can render cached data on startup or when offline.

## Android follow-up

The Android widget should be implemented as a native Kotlin App Widget. It should reuse the same currency-pair defaults and API/provider contract, but not share WPF UI code.
