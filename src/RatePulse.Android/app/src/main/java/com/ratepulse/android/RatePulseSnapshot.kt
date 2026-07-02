package com.ratepulse.android

data class RatePulseSnapshot(
    val usdToCny: Double,
    val updatedAtMillis: Long,
    val source: String,
    val isCached: Boolean = false,
    val errorMessage: String? = null
)
