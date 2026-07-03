package com.ratepulse.android

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Path
import android.util.AttributeSet
import android.view.View
import kotlin.math.abs

class RatePulseChartView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null
) : View(context, attrs) {
    private var history: RateHistory? = null

    private val gridPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(55, 64, 84)
        strokeWidth = dp(1f)
    }
    private val linePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(123, 227, 162)
        strokeWidth = dp(2.5f)
        style = Paint.Style.STROKE
        strokeCap = Paint.Cap.ROUND
        strokeJoin = Paint.Join.ROUND
    }
    private val dotPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(123, 227, 162)
        style = Paint.Style.FILL
    }
    private val textPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(158, 167, 184)
        textSize = dp(11f)
    }
    private val titlePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.rgb(244, 247, 251)
        textSize = dp(13f)
        isFakeBoldText = true
    }

    fun setHistory(history: RateHistory?) {
        this.history = history
        invalidate()
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        val chart = history
        val points = chart?.points.orEmpty().sortedBy { it.date }
        if (points.size < 2) {
            canvas.drawText("暂无曲线数据", dp(12f), height / 2f, titlePaint)
            return
        }

        val left = dp(12f)
        val right = width - dp(12f)
        val top = dp(18f)
        val bottom = height - dp(28f)
        val plotWidth = (right - left).coerceAtLeast(1f)
        val plotHeight = (bottom - top).coerceAtLeast(1f)

        val minRate = points.minOf { it.rate }
        val maxRate = points.maxOf { it.rate }
        val range = (maxRate - minRate).takeIf { abs(it) > 0.0000001 } ?: 1.0

        for (index in 0..2) {
            val y = top + plotHeight * index / 2f
            canvas.drawLine(left, y, right, y, gridPaint)
        }

        val path = Path()
        points.forEachIndexed { index, point ->
            val x = left + plotWidth * index / (points.lastIndex.coerceAtLeast(1))
            val y = bottom - ((point.rate - minRate) / range).toFloat() * plotHeight
            if (index == 0) {
                path.moveTo(x, y)
            } else {
                path.lineTo(x, y)
            }
        }
        canvas.drawPath(path, linePaint)

        points.forEachIndexed { index, point ->
            val x = left + plotWidth * index / (points.lastIndex.coerceAtLeast(1))
            val y = bottom - ((point.rate - minRate) / range).toFloat() * plotHeight
            canvas.drawCircle(x, y, dp(2.5f), dotPaint)
        }

        val first = points.first()
        val middle = points[points.size / 2]
        val last = points.last()
        canvas.drawText(shortDate(first.date), left, height - dp(8f), textPaint)
        canvas.drawText(shortDate(middle.date), width / 2f - dp(16f), height - dp(8f), textPaint)
        canvas.drawText(shortDate(last.date), right - dp(34f), height - dp(8f), textPaint)

        val maxText = RatePulseFormat.rate(maxRate)
        val minText = RatePulseFormat.rate(minRate)
        canvas.drawText(maxText, left, top - dp(4f), textPaint)
        canvas.drawText(minText, left, bottom + dp(13f), textPaint)
    }

    private fun shortDate(date: String): String {
        return if (date.length >= 10) date.substring(5, 10) else date
    }

    private fun dp(value: Float): Float = value * resources.displayMetrics.density
}
