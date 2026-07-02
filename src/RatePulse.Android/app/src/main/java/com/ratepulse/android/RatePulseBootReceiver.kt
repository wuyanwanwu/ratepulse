package com.ratepulse.android

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent

class RatePulseBootReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action == Intent.ACTION_BOOT_COMPLETED) {
            RatePulseSchedule.scheduleDailyRefresh(context)
        }
    }
}
