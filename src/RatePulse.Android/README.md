# RatePulse Android

Android desktop widget prototype for RatePulse.

## Current v1 scope

- Native Android app written in Kotlin.
- Home-screen widget using `AppWidgetProvider` and `RemoteViews`.
- App control page for choosing the USD quote currency.
- Common currency dropdown with Chinese/English labels in app code.
- Default display: `1 USD = x CNY`; widget follows the selected quote currency.
- 15-day USD-based trend chart on the app page.
- Simplified 15-day sparkline image on the home-screen widget.
- Manual refresh button on the widget.
- Daily automatic refresh scheduled for 08:15 local device time.
- Local cache via `SharedPreferences`, so the widget can show the last successful rate and trend offline.

## Open in Android Studio

This machine does not currently expose Android SDK or Gradle on the command line, so open this folder in Android Studio:

```text
D:\program\ratepulse\src\RatePulse.Android
```

Android Studio should install or sync the required Gradle and Android SDK components. After sync, run the `app` configuration on a device or emulator, choose the target currency in the app, refresh once, then add the RatePulse widget from the app button or from the launcher widget picker.
