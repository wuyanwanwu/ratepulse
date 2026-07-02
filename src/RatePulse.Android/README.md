# RatePulse Android

Android desktop widget prototype for RatePulse.

## Current v1 scope

- Native Android app written in Kotlin.
- Home-screen widget using `AppWidgetProvider` and `RemoteViews`.
- Default display: `1 USD = x CNY`.
- Manual refresh button on the widget.
- Daily automatic refresh scheduled for 08:15 local device time.
- Local cache via `SharedPreferences`, so the widget can show the last successful rate offline.

## Open in Android Studio

This machine does not currently expose Android SDK or Gradle on the command line, so open this folder in Android Studio:

```text
D:\program\ratepulse\src\RatePulse.Android
```

Android Studio should install or sync the required Gradle and Android SDK components. After sync, run the `app` configuration on a device or emulator, then add the RatePulse widget from the launcher widget picker.
