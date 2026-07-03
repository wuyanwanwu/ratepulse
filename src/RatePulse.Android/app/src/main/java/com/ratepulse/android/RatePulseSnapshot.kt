package com.ratepulse.android

data class RatePulseSnapshot(
    val targetCurrency: String,
    val usdToTarget: Double,
    val updatedAtMillis: Long,
    val source: String,
    val isCached: Boolean = false,
    val errorMessage: String? = null
)

data class RateHistoryPoint(
    val date: String,
    val rate: Double,
    val source: String
)

data class RateHistory(
    val targetCurrency: String,
    val points: List<RateHistoryPoint>,
    val updatedAtMillis: Long,
    val source: String,
    val isCached: Boolean = false,
    val errorMessage: String? = null
)
