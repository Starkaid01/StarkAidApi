package com.starkaid.starkaidapp.services

import android.app.Service
import android.content.Intent
import android.graphics.PixelFormat
import android.os.IBinder
import android.view.*
import android.widget.ImageButton
import com.starkaid.starkaidapp.R
import kotlin.math.abs

class FloatingButtonService : Service() {

    private var windowManager: WindowManager? = null
    private var floatingView: View? = null
    private var isButtonVisible = false

    companion object {
        var FloatingButtonServiceInstance: FloatingButtonService? = null
    }

    override fun onCreate() {
        super.onCreate()

        FloatingButtonServiceInstance = this

        windowManager = getSystemService(WINDOW_SERVICE) as WindowManager
        val inflater = getSystemService(LAYOUT_INFLATER_SERVICE) as LayoutInflater
        floatingView = inflater.inflate(R.layout.layout_floating_button, null)

        val button = floatingView?.findViewById<ImageButton>(R.id.floating_button)

        // Clique + arrastar no mesmo botão
        button?.setOnTouchListener(object : View.OnTouchListener {
            private var initialX = 0
            private var initialY = 0
            private var initialTouchX = 0f
            private var initialTouchY = 0f
            private var isClick = false

            override fun onTouch(v: View, event: MotionEvent): Boolean {
                val params = floatingView?.layoutParams as? WindowManager.LayoutParams
                    ?: return false
                when (event.action) {
                    MotionEvent.ACTION_DOWN -> {
                        initialX = params.x
                        initialY = params.y
                        initialTouchX = event.rawX
                        initialTouchY = event.rawY
                        isClick = true
                        return true
                    }
                    MotionEvent.ACTION_MOVE -> {
                        val dx = (event.rawX - initialTouchX).toInt()
                        val dy = (event.rawY - initialTouchY).toInt()
                        if (abs(dx) > 10 || abs(dy) > 10) {
                            isClick = false
                            params.x = initialX + dx
                            params.y = initialY + dy
                            windowManager?.updateViewLayout(floatingView, params)
                        }
                        return true
                    }
                    MotionEvent.ACTION_UP -> {
                        if (isClick) {
                            // clique rápido abre o app
                            val intent = Intent(this@FloatingButtonService, com.starkaid.starkaidapp.ui.MainActivity::class.java)
                            intent.addFlags(
                                Intent.FLAG_ACTIVITY_NEW_TASK or
                                        Intent.FLAG_ACTIVITY_CLEAR_TOP or
                                        Intent.FLAG_ACTIVITY_SINGLE_TOP
                            )
                            startActivity(intent)
                        }
                        return true
                    }
                }
                return false
            }
        })
    }

    // Mostrar botão
    fun showButton() {
        if (!isButtonVisible && floatingView?.windowToken == null) {
            val layoutParams = WindowManager.LayoutParams(
                WindowManager.LayoutParams.WRAP_CONTENT,
                WindowManager.LayoutParams.WRAP_CONTENT,
                WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY,
                WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE,
                PixelFormat.TRANSLUCENT
            )
            layoutParams.gravity = Gravity.TOP or Gravity.START
            layoutParams.x = 30
            layoutParams.y = 100

            windowManager?.addView(floatingView, layoutParams)
            isButtonVisible = true
        }
    }

    // Ocultar botão
    fun hideButton() {
        if (isButtonVisible && floatingView != null) {
            windowManager?.removeView(floatingView)
            isButtonVisible = false
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        hideButton()
        FloatingButtonServiceInstance = null
    }

    override fun onBind(intent: Intent?): IBinder? = null
}