package com.ratepulse.android

import android.app.Activity
import android.appwidget.AppWidgetManager
import android.content.ComponentName
import android.os.Bundle
import android.widget.Button
import android.widget.TextView
import android.widget.Toast

class MainActivity : Activity() {
    private lateinit var rateText: TextView
    private lateinit var metaText: TextView
    private lateinit var statusText: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        rateText = findViewById(R.id.main_rate_text)
        metaText = findViewById(R.id.main_meta_text)
        statusText = findViewById(R.id.main_status_text)

        findViewById<Button>(R.id.main_refresh_button).setOnClickListener {
            refreshNow()
        }

        findViewById<Button>(R.id.main_add_widget_button).setOnClickListener {
            requestPinWidget()
        }

        RatePulseSchedule.scheduleDailyRefresh(this)
        bindSnapshot()
    }

    override fun onResume() {
        super.onResume()
        bindSnapshot()
    }

    private fun bindSnapshot() {
        val snapshot = RatePulseRepository(this).cachedSnapshot()
        if (snapshot == null) {
            rateText.text = "1 USD = -- CNY"
            metaText.text = "还没有缓存汇率"
            statusText.text = "先刷新一次，再添加桌面小组件"
            return
        }

        rateText.text = "1 USD = ${RatePulseFormat.rate(snapshot.usdToCny)} CNY"
        metaText.text = "${RatePulseFormat.updatedAt(snapshot.updatedAtMillis)} · ${snapshot.source}"
        statusText.text = "桌面小组件已就绪"
    }

    private fun refreshNow() {
        statusText.text = "正在刷新..."
        Thread {
            try {
                val snapshot = RatePulseRepository(this).refreshSnapshot()
                RatePulseWidgetUpdater.updateAll(this, snapshot)
                runOnUiThread {
                    bindSnapshot()
                    Toast.makeText(this, "汇率已刷新", Toast.LENGTH_SHORT).show()
                }
            } catch (exception: Exception) {
                RateSyncWorker.enqueueNow(this)
                runOnUiThread {
                    bindSnapshot()
                    statusText.text = "刷新失败，已继续显示缓存"
                    Toast.makeText(this, "刷新失败：${exception.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }.start()
    }

    private fun requestPinWidget() {
        val widgetManager = getSystemService(AppWidgetManager::class.java)
        val provider = ComponentName(this, RatePulseWidgetProvider::class.java)
        if (widgetManager.isRequestPinAppWidgetSupported) {
            widgetManager.requestPinAppWidget(provider, null, null)
            statusText.text = "请在系统弹窗中确认添加"
        } else {
            statusText.text = "当前桌面不支持应用内添加，请从桌面小组件列表添加 RatePulse"
            Toast.makeText(this, "请从手机桌面的小组件列表添加 RatePulse", Toast.LENGTH_LONG).show()
        }
    }
}
