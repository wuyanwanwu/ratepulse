package com.ratepulse.android

import android.app.PendingIntent
import android.appwidget.AppWidgetManager
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.widget.RemoteViews

object RatePulseWidgetUpdater {
    fun updateAll(
        context: Context,
        snapshot: RatePulseSnapshot? = null,
        history: RateHistory? = null
    ) {
        val appWidgetManager = AppWidgetManager.getInstance(context)
        val widgetComponent = ComponentName(context, RatePulseWidgetProvider::class.java)
        val widgetIds = appWidgetManager.getAppWidgetIds(widgetComponent)
        update(context, appWidgetManager, widgetIds, snapshot, history)
    }

    fun update(
        context: Context,
        appWidgetManager: AppWidgetManager,
        appWidgetIds: IntArray,
        snapshot: RatePulseSnapshot? = null,
        history: RateHistory? = null
    ) {
        val repository = RatePulseRepository(context)
        val target = repository.selectedTargetCurrency()
        val effectiveSnapshot = snapshot ?: repository.cachedSnapshot(target)
        val effectiveHistory = history ?: repository.cachedHistory(target)
        appWidgetIds.forEach { widgetId ->
            appWidgetManager.updateAppWidget(widgetId, buildRemoteViews(context, effectiveSnapshot, effectiveHistory))
        }
    }

    private fun buildRemoteViews(
        context: Context,
        snapshot: RatePulseSnapshot?,
        history: RateHistory?
    ): RemoteViews {
        val views = RemoteViews(context.packageName, R.layout.ratepulse_widget)
        val target = snapshot?.targetCurrency ?: RatePulseRepository(context).selectedTargetCurrency()
        val rateText = snapshot?.let { "1 USD = ${RatePulseFormat.rate(it.usdToTarget)} ${it.targetCurrency}" }
            ?: "1 USD = -- $target"
        val metaText = when {
            snapshot == null -> "点刷新获取汇率"
            !snapshot.errorMessage.isNullOrBlank() -> "缓存数据 / 离线"
            else -> "${RatePulseFormat.updatedAt(snapshot.updatedAtMillis)} · ${snapshot.source}"
        }

        views.setTextViewText(R.id.widget_title, "USD / ${RatePulseCurrencies.labelFor(target)}")
        views.setTextViewText(R.id.widget_rate, rateText)
        views.setTextViewText(R.id.widget_meta, metaText)
        views.setImageViewBitmap(R.id.widget_chart, RatePulseChartRenderer.renderSparkline(history))
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
