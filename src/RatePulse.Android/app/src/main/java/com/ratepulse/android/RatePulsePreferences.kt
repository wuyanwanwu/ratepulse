package com.ratepulse.android

import android.content.Context

class RatePulsePreferences(context: Context) {
    private val preferences = context.getSharedPreferences(PREFERENCES_NAME, Context.MODE_PRIVATE)

    fun loadSnapshot(): RatePulseSnapshot? {
        val rate = preferences.getFloat(KEY_USD_TO_CNY, Float.NaN)
        if (rate.isNaN()) {
            return null
        }

        return RatePulseSnapshot(
            usdToCny = rate.toDouble(),
            updatedAtMillis = preferences.getLong(KEY_UPDATED_AT_MILLIS, 0L),
            source = preferences.getString(KEY_SOURCE, DEFAULT_SOURCE) ?: DEFAULT_SOURCE,
            isCached = true
        )
    }

    fun saveSnapshot(snapshot: RatePulseSnapshot) {
        preferences.edit()
            .putFloat(KEY_USD_TO_CNY, snapshot.usdToCny.toFloat())
            .putLong(KEY_UPDATED_AT_MILLIS, snapshot.updatedAtMillis)
            .putString(KEY_SOURCE, snapshot.source)
            .apply()
    }

    fun refreshHour(): Int = preferences.getInt(KEY_REFRESH_HOUR, 8)

    fun refreshMinute(): Int = preferences.getInt(KEY_REFRESH_MINUTE, 15)

    companion object {
        private const val PREFERENCES_NAME = "ratepulse"
        private const val KEY_USD_TO_CNY = "usd_to_cny"
        private const val KEY_UPDATED_AT_MILLIS = "updated_at_millis"
        private const val KEY_SOURCE = "source"
        private const val KEY_REFRESH_HOUR = "refresh_hour"
        private const val KEY_REFRESH_MINUTE = "refresh_minute"
        private const val DEFAULT_SOURCE = "open.er-api"
    }
}
