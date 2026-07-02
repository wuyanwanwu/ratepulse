package com.ratepulse.android

import java.text.DecimalFormat
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

object RatePulseFormat {
    private val rateFormat = DecimalFormat("0.####")
    private val dateFormat = SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.getDefault())

    fun rate(value: Double): String = rateFormat.format(value)

    fun updatedAt(millis: Long): String {
        if (millis <= 0L) {
            return "No update time"
        }

        return dateFormat.format(Date(millis))
    }
}
