package com.ratepulse.android

import android.content.Context
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.PeriodicWorkRequest
import androidx.work.WorkManager
import java.util.Calendar
import java.util.concurrent.TimeUnit

object RatePulseSchedule {
    private const val DAILY_WORK_NAME = "ratepulse-daily-refresh"

    fun scheduleDailyRefresh(context: Context) {
        val preferences = RatePulsePreferences(context)
        val delayMillis = nextDelayMillis(preferences.refreshHour(), preferences.refreshMinute())
        val request = PeriodicWorkRequest.Builder(RateSyncWorker::class.java, 24, TimeUnit.HOURS)
            .setInitialDelay(delayMillis, TimeUnit.MILLISECONDS)
            .build()

        WorkManager.getInstance(context).enqueueUniquePeriodicWork(
            DAILY_WORK_NAME,
            ExistingPeriodicWorkPolicy.KEEP,
            request
        )
    }

    private fun nextDelayMillis(hour: Int, minute: Int): Long {
        val now = Calendar.getInstance()
        val next = Calendar.getInstance().apply {
            set(Calendar.HOUR_OF_DAY, hour)
            set(Calendar.MINUTE, minute)
            set(Calendar.SECOND, 0)
            set(Calendar.MILLISECOND, 0)
        }

        if (!next.after(now)) {
            next.add(Calendar.DAY_OF_YEAR, 1)
        }

        return next.timeInMillis - now.timeInMillis
    }
}
