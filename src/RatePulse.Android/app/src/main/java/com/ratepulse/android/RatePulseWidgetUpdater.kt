package com.ratepulse.android

import android.app.PendingIntent
import android.appwidget.AppWidgetManager
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.widget.RemoteViews

object RatePulseWidgetUpdater {
    fun updateAll(context: Context, snapshot: RatePulseSnapshot? = RatePulseRepository(context).cachedSnapshot()) {
        val appWidgetManager = AppWidgetManager.getInstance(context)
        val widgetComponent = ComponentName(context, RatePulseWidgetProvider::class.java)
        val widgetIds = appWidgetManager.getAppWidgetIds(widgetComponent)
        update(context, appWidgetManager, widgetIds, snapshot)
    }

    fun update(
        context: Context,
        appWidgetManager: AppWidgetManager,
        appWidgetIds: IntArray,
        snapshot: RatePulseSnapshot?
    ) {
        appWidgetIds.forEach { widgetId ->
            appWidgetManager.updateAppWidget(widgetId, buildRemoteViews(context, snapshot))
        }
    }

    private fun buildRemoteViews(context: Context, snapshot: RatePulseSnapshot?): RemoteViews {
        val views = RemoteViews(context.packageName, R.layout.ratepulse_widget)
        val rateText = snapshot?.let { "1 USD = ${RatePulseFormat.rate(it.usdToCny)} CNY" }
            ?: "1 USD = -- CNY"
        val metaText = when {
            snapshot == null -> "Tap R to refresh"
            !snapshot.errorMessage.isNullOrBlank() -> "Cached / offline"
            else -> "${RatePulseFormat.updatedAt(snapshot.updatedAtMillis)} · ${snapshot.source}"
        }

        views.setTextViewText(R.id.widget_rate, rateText)
        views.setTextViewText(R.id.widget_meta, metaText)
        views.setOnClickPendingIntent(R.id.widget_refresh, refreshPendingIntent(context))
        views.setOnClickPendingIntent(R.id.widget_root, mainActivityPendingIntent(context))
        return views
    }

    private fun refreshPendingIntent(context: Context): PendingIntent {
        val intent = Intent(context, RatePulseWidgetProvider::class.java).apply {
            action = RatePulseWidgetProvider.ACTION_REFRESH
        }
        return PendingIntent.getBroadcast(
            context,
            1001,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
    }

    private fun mainActivityPendingIntent(context: Context): PendingIntent {
        val intent = Intent(context, MainActivity::class.java)
        return PendingIntent.getActivity(
            context,
            1002,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
    }
}
