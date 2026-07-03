package com.ratepulse.android

import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Path
import kotlin.math.abs

object RatePulseChartRenderer {
    fun renderSparkline(history: RateHistory?, width: Int = 420, height: Int = 90): Bitmap {
        val bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        val points = history?.points.orEmpty().sortedBy { it.date }

        val gridPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.rgb(55, 64, 84)
            strokeWidth = 1.5f
        }
        val linePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.rgb(123, 227, 162)
            strokeWidth = 4f
            style = Paint.Style.STROKE
            strokeCap = Paint.Cap.ROUND
            strokeJoin = Paint.Join.ROUND
        }
        val textPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = Color.rgb(158, 167, 184)
            textSize = 22f
        }

        if (points.size < 2) {
            canvas.drawLine(8f, height / 2f, width - 8f, height / 2f, gridPaint)
            canvas.drawText("No trend", 12f, height / 2f - 8f, textPaint)
            return bitmap
        }

        val left = 8f
        val right = width - 8f
        val top = 10f
        val bottom = height - 12f
        val plotWidth = right - left
        val plotHeight = bottom - top
        val minRate = points.minOf { it.rate }
        val maxRate = points.maxOf { it.rate }
        val range = (maxRate - minRate).takeIf { abs(it) > 0.0000001 } ?: 1.0

        canvas.drawLine(left, top, right, top, gridPaint)
        canvas.drawLine(left, bottom, right, bottom, gridPaint)

        val path = Path()
        points.forEachIndexed { index, point ->
            val x = left + plotWidth * index / points.lastIndex.coerceAtLeast(1)
            val y = bottom - ((point.rate - minRate) / range).toFloat() * plotHeight
            if (index == 0) {
                path.moveTo(x, y)
            } else {
                path.lineTo(x, y)
            }
        }

        canvas.drawPath(path, linePaint)
        return bitmap
    }
}
