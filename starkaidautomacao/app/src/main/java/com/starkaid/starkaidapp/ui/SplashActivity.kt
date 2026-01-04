package com.starkaid.starkaidapp.ui

import android.annotation.SuppressLint
import android.content.Intent
import android.os.Bundle
import android.webkit.JavascriptInterface
import android.webkit.WebChromeClient
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.activity.addCallback
import androidx.core.splashscreen.SplashScreen.Companion.installSplashScreen
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.base.BaseActivity

class SplashActivity : BaseActivity() {

    private var navigated = false

    @SuppressLint("SetJavaScriptEnabled")
    override fun onCreate(savedInstanceState: Bundle?) {
        installSplashScreen()
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_splash)

        val webView = findViewById<WebView>(R.id.webSplash)

        webView.setBackgroundColor(0x00000000)
        webView.overScrollMode = WebView.OVER_SCROLL_NEVER
        webView.settings.javaScriptEnabled = true
        webView.settings.domStorageEnabled = true
        webView.settings.allowFileAccess = true
        webView.settings.allowContentAccess = true

        webView.addJavascriptInterface(object {
            @JavascriptInterface
            fun onDone() {
                runOnUiThread { navigateNext() }
            }
        }, "AndroidSplash")

        webView.webChromeClient = WebChromeClient()
        webView.webViewClient = object : WebViewClient() {
            override fun onPageFinished(view: WebView?, url: String?) {
                super.onPageFinished(view, url)
                view?.evaluateJavascript("window.Splash && Splash.start && Splash.start()", null)
            }
        }

        onBackPressedDispatcher.addCallback(this) {
            navigateNext()
        }

        webView.loadUrl("file:///android_asset/splash.html")
    }

    private fun navigateNext() {
        if (navigated) return
        navigated = true
        startActivity(Intent(this, LoginActivity::class.java))
        finish()
    }
}
