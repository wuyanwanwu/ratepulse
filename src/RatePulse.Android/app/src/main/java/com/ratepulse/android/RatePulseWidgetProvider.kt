package com.ratepulse.android

import android.appwidget.AppWidgetManager
import android.appwidget.AppWidgetProvider
import android.content.Context
import android.content.Intent

class RatePulseWidgetProvider : AppWidgetProvider() {
    override fun onUpdate(context: Context, appWidgetManager: AppWidgetManager, appWidgetIds: IntArray) {
        RatePulseWidgetUpdater.update(context, appWidgetManager, appWidgetIds, RatePulseRepository(context).cachedSnapshot())
        RatePulseSchedule.scheduleDailyRefresh(context)
        RateSyncWorker.enqueueNow(context)
    }

    override fun onReceive(context: Context, intent: Intent) {
        super.onReceive(context, intent)
        if (intent.action == ACTION_REFRESH) {
            RatePulseWidgetUpdater.updateAll(context, RatePulseRepository(context).cachedSnapshot())
            RateSyncWorker.enqueueNow(context)
        }
    }

    companion object {
        const val ACTION_REFRESH = "com.ratepulse.android.action.REFRESH"
    }
}
