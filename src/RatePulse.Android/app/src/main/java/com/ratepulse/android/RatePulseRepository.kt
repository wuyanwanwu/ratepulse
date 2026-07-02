package com.ratepulse.android

import android.content.Context

class RatePulseRepository(context: Context) {
    private val appContext = context.applicationContext
    private val preferences = RatePulsePreferences(appContext)
    private val exchangeRateClient = ExchangeRateClient()

    fun cachedSnapshot(): RatePulseSnapshot? = preferences.loadSnapshot()

    fun refreshSnapshot(): RatePulseSnapshot {
        val snapshot = exchangeRateClient.fetchUsdToCny()
        preferences.saveSnapshot(snapshot)
        return snapshot
    }
}
