package com.ratepulse.android

import org.json.JSONObject
import org.json.JSONArray
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL
import java.time.LocalDate

class ExchangeRateClient {
    fun fetchUsdToTarget(targetCurrency: String): RatePulseSnapshot {
        val target = RatePulseCurrencies.normalize(targetCurrency)
        val root = fetchJsonObject(USD_LATEST_URL)
        val rates = root.getJSONObject("rates")
        val rate = if (target == "USD") 1.0 else rates.getDouble(target)
        val updatedAtMillis = if (root.has("time_last_update_unix")) {
            root.getLong("time_last_update_unix") * 1000L
        } else {
            System.currentTimeMillis()
        }

        return RatePulseSnapshot(
            targetCurrency = target,
            usdToTarget = rate,
            updatedAtMillis = updatedAtMillis,
            source = "open.er-api"
        )
    }

    fun fetchUsdHistory(
        targetCurrency: String,
        days: Int = HISTORY_DAYS,
        latestSnapshot: RatePulseSnapshot? = null
    ): RateHistory {
        val target = RatePulseCurrencies.normalize(targetCurrency)
        val safeDays = days.coerceIn(2, 60)
        if (target == "USD") {
            return flatUsdHistory(safeDays)
        }

        val today = LocalDate.now()
        val startDate = today.minusDays((safeDays - 1).toLong())

        val history = try {
            fetchFrankfurterV2History(target, startDate, today, safeDays)
        } catch (v2Exception: Exception) {
            try {
                fetchFrankfurterClassicHistory(target, startDate, today, safeDays)
            } catch (classicException: Exception) {
                throw IllegalStateException(
                    "历史汇率加载失败。v2: ${v2Exception.message}; fallback: ${classicException.message}",
                    classicException
                )
            }
        }

        return latestSnapshot
            ?.takeIf { RatePulseCurrencies.normalize(it.targetCurrency) == target }
            ?.let { appendLatestPoint(history, it, safeDays) }
            ?: history
    }

    private fun fetchFrankfurterV2History(
        targetCurrency: String,
        startDate: LocalDate,
        endDate: LocalDate,
        days: Int
    ): RateHistory {
        val requestUrl =
            "https://api.frankfurter.dev/v2/rates?from=$startDate&to=$endDate&base=USD&quotes=$targetCurrency"
        val values = fetchJsonArrayOrValueObject(requestUrl)
        val points = buildList {
            for (index in 0 until values.length()) {
                val item = values.getJSONObject(index)
                if (item.optString("quote").equals(targetCurrency, ignoreCase = true)) {
                    add(
                        RateHistoryPoint(
                            date = item.getString("date"),
                            rate = item.getDouble("rate"),
                            source = "frankfurter.dev"
                        )
                    )
                }
            }
        }.sortedBy { it.date }.takeLast(days)

        if (points.isEmpty()) {
            throw IllegalStateException("缺少 USD/$targetCurrency 历史点")
        }

        return RateHistory(
            targetCurrency = targetCurrency,
            points = points,
            updatedAtMillis = System.currentTimeMillis(),
            source = "frankfurter.dev"
        )
    }

    private fun fetchFrankfurterClassicHistory(
        targetCurrency: String,
        startDate: LocalDate,
        endDate: LocalDate,
        days: Int
    ): RateHistory {
        val requestUrl = "https://api.frankfurter.app/$startDate..$endDate?from=USD&to=$targetCurrency"
        val root = fetchJsonObject(requestUrl)
        val rates = root.getJSONObject("rates")
        val points = buildList {
            val keys = rates.keys()
            while (keys.hasNext()) {
                val date = keys.next()
                val rateObject = rates.getJSONObject(date)
                if (rateObject.has(targetCurrency)) {
                    add(
                        RateHistoryPoint(
                            date = date,
                            rate = rateObject.getDouble(targetCurrency),
                            source = "frankfurter.app"
                        )
                    )
                }
            }
        }.sortedBy { it.date }.takeLast(days)

        if (points.isEmpty()) {
            throw IllegalStateException("缺少 USD/$targetCurrency 历史点")
        }

        return RateHistory(
            targetCurrency = targetCurrency,
            points = points,
            updatedAtMillis = System.currentTimeMillis(),
            source = "frankfurter.app"
        )
    }

    private fun flatUsdHistory(days: Int): RateHistory {
        val today = LocalDate.now()
        val points = (days - 1 downTo 0).map { offset ->
            RateHistoryPoint(
                date = today.minusDays(offset.toLong()).toString(),
                rate = 1.0,
                source = "local"
            )
        }

        return RateHistory(
            targetCurrency = "USD",
            points = points,
            updatedAtMillis = System.currentTimeMillis(),
            source = "local"
        )
    }

    private fun appendLatestPoint(
        history: RateHistory,
        latestSnapshot: RatePulseSnapshot,
        days: Int
    ): RateHistory {
        val today = LocalDate.now().toString()
        val points = history.points
            .filterNot { it.date == today }
            .plus(
                RateHistoryPoint(
                    date = today,
                    rate = latestSnapshot.usdToTarget,
                    source = latestSnapshot.source
                )
            )
            .sortedBy { it.date }
            .takeLast(days)

        return history.copy(
            points = points,
            updatedAtMillis = latestSnapshot.updatedAtMillis,
            source = "${history.source}+${latestSnapshot.source}"
        )
    }

    private fun fetchJsonObject(requestUrl: String): JSONObject {
        return JSONObject(fetchText(requestUrl))
    }

    private fun fetchJsonArrayOrValueObject(requestUrl: String): JSONArray {
        val body = fetchText(requestUrl).trim()
        if (body.startsWith("[")) {
            return JSONArray(body)
        }

        val root = JSONObject(body)
        return root.optJSONArray("value")
            ?: throw IllegalStateException("历史接口没有返回数组")
    }

    private fun fetchText(requestUrl: String): String {
        val connection = URL(requestUrl).openConnection() as HttpURLConnection
        connection.connectTimeout = 8000
        connection.readTimeout = 8000
        connection.requestMethod = "GET"
        connection.setRequestProperty("Accept", "application/json")

        try {
            val responseCode = connection.responseCode
            if (responseCode !in 200..299) {
                throw IllegalStateException("HTTP $responseCode")
            }

            return BufferedReader(InputStreamReader(connection.inputStream)).use { reader ->
                reader.readText()
            }
        } finally {
            connection.disconnect()
        }
    }

    companion object {
        const val HISTORY_DAYS = 15
        private const val USD_LATEST_URL = "https://open.er-api.com/v6/latest/USD"
    }
}
