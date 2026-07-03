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
            val repository = RatePulseRepository(applicationContext)
            val snapshot = repository.refreshSnapshot()
            val history = repository.refreshHistory(snapshot.targetCurrency, snapshot)
            RatePulseWidgetUpdater.updateAll(applicationContext, snapshot, history)
            Result.success()
        } catch (exception: Exception) {
            val repository = RatePulseRepository(applicationContext)
            val target = repository.selectedTargetCurrency()
            val cached = repository.cachedSnapshot(target)
            val history = repository.cachedHistory(target)
            RatePulseWidgetUpdater.updateAll(
                applicationContext,
                cached?.copy(errorMessage = exception.message),
                history
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
