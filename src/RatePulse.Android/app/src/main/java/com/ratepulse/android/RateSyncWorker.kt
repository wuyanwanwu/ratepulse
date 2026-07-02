package com.ratepulse.android

import android.content.Context
import androidx.work.ExistingWorkPolicy
import androidx.work.OneTimeWorkRequest
import androidx.work.WorkManager
import androidx.work.Worker
import androidx.work.WorkerParameters

class RateSyncWorker(
    context: Context,
    workerParameters: WorkerParameters
) : Worker(context, workerParameters) {
    override fun doWork(): Result {
        return try {
            val snapshot = RatePulseRepository(applicationContext).refreshSnapshot()
            RatePulseWidgetUpdater.updateAll(applicationContext, snapshot)
            Result.success()
        } catch (exception: Exception) {
            val cached = RatePulseRepository(applicationContext).cachedSnapshot()
            RatePulseWidgetUpdater.updateAll(
                applicationContext,
                cached?.copy(errorMessage = exception.message)
            )
            Result.retry()
        }
    }

    companion object {
        private const val MANUAL_WORK_NAME = "ratepulse-manual-refresh"

        fun enqueueNow(context: Context) {
            val request = OneTimeWorkRequest.Builder(RateSyncWorker::class.java).build()
            WorkManager.getInstance(context).enqueueUniqueWork(
                MANUAL_WORK_NAME,
                ExistingWorkPolicy.REPLACE,
                request
            )
        }
    }
}
