package com.ratepulse.android

import android.app.Activity
import android.os.Bundle
import android.widget.Button
import android.widget.TextView

class MainActivity : Activity() {
    private lateinit var rateText: TextView
    private lateinit var metaText: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        rateText = findViewById(R.id.main_rate_text)
        metaText = findViewById(R.id.main_meta_text)

        findViewById<Button>(R.id.main_refresh_button).setOnClickListener {
            RatePulseWidgetUpdater.updateAll(this)
            RateSyncWorker.enqueueNow(this)
            bindSnapshot()
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
            metaText.text = "No cached rate yet. Refresh now or add the widget."
            return
        }

        rateText.text = "1 USD = ${RatePulseFormat.rate(snapshot.usdToCny)} CNY"
        metaText.text = "${RatePulseFormat.updatedAt(snapshot.updatedAtMillis)} · ${snapshot.source}"
    }
}
