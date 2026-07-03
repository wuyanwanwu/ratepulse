package com.ratepulse.android

import android.content.Context

class RatePulseRepository(context: Context) {
    private val appContext = context.applicationContext
    private val preferences = RatePulsePreferences(appContext)
    private val exchangeRateClient = ExchangeRateClient()

    fun selectedTargetCurrency(): String = preferences.selectedTargetCurrency()

    fun saveSelectedTargetCurrency(targetCurrency: String) {
        preferences.saveSelectedTargetCurrency(targetCurrency)
    }

    fun cachedSnapshot(targetCurrency: String = selectedTargetCurrency()): RatePulseSnapshot? {
        return preferences.loadSnapshot(targetCurrency)
    }

    fun cachedHistory(targetCurrency: String = selectedTargetCurrency()): RateHistory? {
        return preferences.loadHistory(targetCurrency)
    }

    fun refreshSnapshot(targetCurrency: String = selectedTargetCurrency()): RatePulseSnapshot {
        val target = RatePulseCurrencies.normalize(targetCurrency)
        val snapshot = exchangeRateClient.fetchUsdToTarget(target)
        preferences.saveSnapshot(snapshot)
        return snapshot
    }

    fun refreshHistory(
        targetCurrency: String = selectedTargetCurrency(),
        latestSnapshot: RatePulseSnapshot? = cachedSnapshot(targetCurrency)
    ): RateHistory {
        val target = RatePulseCurrencies.normalize(targetCurrency)
        val history = exchangeRateClient.fetchUsdHistory(
            targetCurrency = target,
            latestSnapshot = latestSnapshot
        )
        preferences.saveHistory(history)
        return history
    }
}
