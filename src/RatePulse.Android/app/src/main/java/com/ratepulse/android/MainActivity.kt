package com.ratepulse.android

import android.app.Activity
import android.appwidget.AppWidgetManager
import android.content.ComponentName
import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.Spinner
import android.widget.TextView
import android.widget.Toast

class MainActivity : Activity() {
    private lateinit var currencySpinner: Spinner
    private lateinit var rateText: TextView
    private lateinit var metaText: TextView
    private lateinit var chartStatusText: TextView
    private lateinit var statusText: TextView
    private lateinit var chartView: RatePulseChartView
    private lateinit var repository: RatePulseRepository
    private var currencySpinnerReady = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        repository = RatePulseRepository(this)
        currencySpinner = findViewById(R.id.main_currency_spinner)
        rateText = findViewById(R.id.main_rate_text)
        metaText = findViewById(R.id.main_meta_text)
        chartStatusText = findViewById(R.id.main_chart_status_text)
        statusText = findViewById(R.id.main_status_text)
        chartView = findViewById(R.id.main_chart_view)

        setupCurrencySpinner()

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

    private fun setupCurrencySpinner() {
        val adapter = ArrayAdapter(
            this,
            R.layout.currency_spinner_item,
            RatePulseCurrencies.options.map { it.label }
        )
        adapter.setDropDownViewResource(R.layout.currency_spinner_dropdown_item)
        currencySpinner.adapter = adapter
        currencySpinner.setSelection(RatePulseCurrencies.indexOf(repository.selectedTargetCurrency()), false)
        currencySpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                if (!currencySpinnerReady) {
                    currencySpinnerReady = true
                    return
                }

                val target = RatePulseCurrencies.options[position].code
                repository.saveSelectedTargetCurrency(target)
                bindSnapshot()
                refreshNow()
            }

            override fun onNothingSelected(parent: AdapterView<*>?) = Unit
        }
    }

    private fun bindSnapshot() {
        val target = repository.selectedTargetCurrency()
        val snapshot = repository.cachedSnapshot(target)
        val history = repository.cachedHistory(target)

        if (snapshot == null) {
            rateText.text = "1 USD = -- $target"
            metaText.text = "还没有缓存汇率"
            statusText.text = "选择货币后点刷新，或添加桌面小组件"
        } else {
            rateText.text = "1 USD = ${RatePulseFormat.rate(snapshot.usdToTarget)} ${snapshot.targetCurrency}"
            metaText.text = "${RatePulseFormat.updatedAt(snapshot.updatedAtMillis)} · ${snapshot.source}"
            statusText.text = "当前小组件货币：USD / ${RatePulseCurrencies.labelFor(target)}"
        }

        chartView.setHistory(history)
        chartStatusText.text = if (history == null) {
            "暂无 15 天曲线缓存，点刷新加载"
        } else {
            "近 15 天走势 · ${history.source}"
        }
    }

    private fun refreshNow() {
        val target = repository.selectedTargetCurrency()
        statusText.text = "正在刷新 USD / ${RatePulseCurrencies.labelFor(target)}..."
        Thread {
            try {
                val snapshot = repository.refreshSnapshot(target)
                var history = repository.cachedHistory(target)
                var historyError: Exception? = null
                try {
                    history = repository.refreshHistory(target, snapshot)
                } catch (exception: Exception) {
                    historyError = exception
                }

                RatePulseWidgetUpdater.updateAll(this, snapshot, history)
                runOnUiThread {
                    bindSnapshot()
                    if (historyError == null) {
                        Toast.makeText(this, "汇率和曲线已刷新", Toast.LENGTH_SHORT).show()
                    } else {
                        statusText.text = "汇率已刷新，曲线加载失败：${historyError.message}"
                        Toast.makeText(this, "曲线加载失败", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (exception: Exception) {
                RateSyncWorker.enqueueNow(this)
                val cached = repository.cachedSnapshot(target)
                val history = repository.cachedHistory(target)
                RatePulseWidgetUpdater.updateAll(this, cached?.copy(errorMessage = exception.message), history)
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
            RatePulseWidgetUpdater.updateAll(this)
            widgetManager.requestPinAppWidget(provider, null, null)
            statusText.text = "请在系统弹窗中确认添加"
        } else {
            statusText.text = "当前桌面不支持应用内添加，请从桌面小组件列表添加 RatePulse"
            Toast.makeText(this, "请从手机桌面的小组件列表添加 RatePulse", Toast.LENGTH_LONG).show()
        }
    }
}
