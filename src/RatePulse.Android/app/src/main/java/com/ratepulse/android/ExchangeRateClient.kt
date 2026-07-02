package com.ratepulse.android

import org.json.JSONObject
import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL

class ExchangeRateClient {
    fun fetchUsdToCny(): RatePulseSnapshot {
        val connection = URL(USD_LATEST_URL).openConnection() as HttpURLConnection
        connection.connectTimeout = 8000
        connection.readTimeout = 8000
        connection.requestMethod = "GET"
        connection.setRequestProperty("Accept", "application/json")

        try {
            val responseCode = connection.responseCode
            if (responseCode !in 200..299) {
                throw IllegalStateException("HTTP $responseCode")
            }

            val body = BufferedReader(InputStreamReader(connection.inputStream)).use { reader ->
                reader.readText()
            }
            val root = JSONObject(body)
            val rate = root.getJSONObject("rates").getDouble("CNY")
            val updatedAtMillis = if (root.has("time_last_update_unix")) {
                root.getLong("time_last_update_unix") * 1000L
            } else {
                System.currentTimeMillis()
            }

            return RatePulseSnapshot(
                usdToCny = rate,
                updatedAtMillis = updatedAtMillis,
                source = "open.er-api"
            )
        } finally {
            connection.disconnect()
        }
    }

    companion object {
        private const val USD_LATEST_URL = "https://open.er-api.com/v6/latest/USD"
    }
}
