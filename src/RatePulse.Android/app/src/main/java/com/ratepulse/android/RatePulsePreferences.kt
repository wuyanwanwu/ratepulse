package com.ratepulse.android

import android.content.Context
import org.json.JSONArray
import org.json.JSONObject

class RatePulsePreferences(context: Context) {
    private val preferences = context.getSharedPreferences(PREFERENCES_NAME, Context.MODE_PRIVATE)

    fun selectedTargetCurrency(): String {
        return RatePulseCurrencies.normalize(
            preferences.getString(KEY_TARGET_CURRENCY, RatePulseCurrencies.DEFAULT_TARGET)
        )
    }

    fun saveSelectedTargetCurrency(targetCurrency: String) {
        preferences.edit()
            .putString(KEY_TARGET_CURRENCY, RatePulseCurrencies.normalize(targetCurrency))
            .apply()
    }

    fun loadSnapshot(targetCurrency: String = selectedTargetCurrency()): RatePulseSnapshot? {
        val target = RatePulseCurrencies.normalize(targetCurrency)
        val rateText = preferences.getString(snapshotRateKey(target), null)
        val rate = rateText?.toDoubleOrNull()

        if (rate != null) {
            return RatePulseSnapshot(
                targetCurrency = target,
                usdToTarget = rate,
                updatedAtMillis = preferences.getLong(snapshotUpdatedAtKey(target), 0L),
                source = preferences.getString(snapshotSourceKey(target), DEFAULT_SOURCE) ?: DEFAULT_SOURCE,
                isCached = true
            )
        }

        if (target == "CNY") {
            val legacyRate = preferences.getFloat(KEY_LEGACY_USD_TO_CNY, Float.NaN)
            if (!legacyRate.isNaN()) {
                return RatePulseSnapshot(
                    targetCurrency = "CNY",
                    usdToTarget = legacyRate.toDouble(),
                    updatedAtMillis = preferences.getLong(KEY_LEGACY_UPDATED_AT_MILLIS, 0L),
                    source = preferences.getString(KEY_LEGACY_SOURCE, DEFAULT_SOURCE) ?: DEFAULT_SOURCE,
                    isCached = true
                )
            }
        }

        return null
    }

    fun saveSnapshot(snapshot: RatePulseSnapshot) {
        val target = RatePulseCurrencies.normalize(snapshot.targetCurrency)
        preferences.edit()
            .putString(snapshotRateKey(target), snapshot.usdToTarget.toString())
            .putLong(snapshotUpdatedAtKey(target), snapshot.updatedAtMillis)
            .putString(snapshotSourceKey(target), snapshot.source)
            .apply()
    }

    fun loadHistory(targetCurrency: String = selectedTargetCurrency()): RateHistory? {
        val target = RatePulseCurrencies.normalize(targetCurrency)
        val raw = preferences.getString(historyKey(target), null) ?: return null

        return try {
            val root = JSONObject(raw)
            val pointsJson = root.getJSONArray("points")
            val points = buildList {
                for (index in 0 until pointsJson.length()) {
                    val item = pointsJson.getJSONObject(index)
                    add(
                        RateHistoryPoint(
                            date = item.getString("date"),
                            rate = item.getDouble("rate"),
                            source = item.optString("source", root.optString("source", DEFAULT_HISTORY_SOURCE))
                        )
                    )
                }
            }

            if (points.isEmpty()) {
                null
            } else {
                RateHistory(
                    targetCurrency = target,
                    points = points,
                    updatedAtMillis = root.optLong("updatedAtMillis", 0L),
                    source = root.optString("source", DEFAULT_HISTORY_SOURCE),
                    isCached = true
                )
            }
        } catch (_: Exception) {
            null
        }
    }

    fun saveHistory(history: RateHistory) {
        val target = RatePulseCurrencies.normalize(history.targetCurrency)
        val pointsJson = JSONArray()
        history.points.forEach { point ->
            pointsJson.put(
                JSONObject()
                    .put("date", point.date)
                    .put("rate", point.rate)
                    .put("source", point.source)
            )
        }

        val root = JSONObject()
            .put("targetCurrency", target)
            .put("updatedAtMillis", history.updatedAtMillis)
            .put("source", history.source)
            .put("points", pointsJson)

        preferences.edit()
            .putString(historyKey(target), root.toString())
            .apply()
    }

    fun refreshHour(): Int = preferences.getInt(KEY_REFRESH_HOUR, 8)

    fun refreshMinute(): Int = preferences.getInt(KEY_REFRESH_MINUTE, 15)

    private fun snapshotRateKey(targetCurrency: String): String = "snapshot_${targetCurrency}_rate"

    private fun snapshotUpdatedAtKey(targetCurrency: String): String = "snapshot_${targetCurrency}_updated_at"

    private fun snapshotSourceKey(targetCurrency: String): String = "snapshot_${targetCurrency}_source"

    private fun historyKey(targetCurrency: String): String = "history_$targetCurrency"

    companion object {
        private const val PREFERENCES_NAME = "ratepulse"
        private const val KEY_TARGET_CURRENCY = "target_currency"
        private const val KEY_REFRESH_HOUR = "refresh_hour"
        private const val KEY_REFRESH_MINUTE = "refresh_minute"
        private const val KEY_LEGACY_USD_TO_CNY = "usd_to_cny"
        private const val KEY_LEGACY_UPDATED_AT_MILLIS = "updated_at_millis"
        private const val KEY_LEGACY_SOURCE = "source"
        private const val DEFAULT_SOURCE = "open.er-api"
        private const val DEFAULT_HISTORY_SOURCE = "frankfurter"
    }
}
