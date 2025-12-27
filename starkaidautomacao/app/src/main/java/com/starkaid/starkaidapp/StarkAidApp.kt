package com.starkaid.starkaidapp

import android.app.Activity
import android.app.Application
import android.content.Context
import android.content.pm.PackageManager
import android.os.Bundle
import android.util.Log
import com.google.firebase.FirebaseApp
import com.starkaid.starkaidapp.util.NotificationHelper

class StarkAidApp : Application(), Application.ActivityLifecycleCallbacks {

    companion object {
        var isAppVisible = false
        var currentActivity: Activity? = null
        private lateinit var instance: StarkAidApp

        fun getAppContext(): Context = instance.applicationContext
    }

    override fun onCreate() {
        super.onCreate()
        instance = this
        registerActivityLifecycleCallbacks(this)
        FirebaseApp.initializeApp(this)
        NotificationHelper.criarCanais(this)
    }


    override fun onActivityResumed(activity: Activity) {
        isAppVisible = true
        currentActivity = activity
    }
 
    override fun onActivityPaused(activity: Activity) {
        isAppVisible = false
        if (currentActivity == activity) {
            currentActivity = null
        }
    }

    override fun onActivityStarted(activity: Activity) {}
    override fun onActivityStopped(activity: Activity) {}
    override fun onActivityCreated(activity: Activity, savedInstanceState: Bundle?) {}
    override fun onActivitySaveInstanceState(activity: Activity, outState: Bundle) {}
    override fun onActivityDestroyed(activity: Activity) {}
}