package com.starkaid.starkaidapp.extensions

import android.animation.ArgbEvaluator
import android.animation.ValueAnimator
import android.view.View
import android.view.animation.PathInterpolator
import androidx.core.view.isVisible
import com.airbnb.lottie.LottieAnimationView

fun View.animateBackgroundColor(startColor: Int, endColor: Int, duration: Long = 1000L) {
    val animator = ValueAnimator.ofObject(ArgbEvaluator(), startColor, endColor)
    animator.duration = duration
    animator.addUpdateListener { valueAnimator ->
        this.setBackgroundColor(valueAnimator.animatedValue as Int)
    }
    animator.start()
}

fun LottieAnimationView.startPulseAnimation() {
    val interpolator = PathInterpolator(0.4f, 0.0f, 0.2f, 1.0f)
    this.animate()
        .scaleX(1.2f)
        .scaleY(1.2f)
        .setInterpolator(interpolator)
        .setDuration(500)
        .withEndAction {
            this.animate()
                .scaleX(1f)
                .scaleY(1f)
                .setInterpolator(interpolator)
                .setDuration(500)
                .start()
        }
        .start()
}

fun View.createHoverEffect() {
    // Verifique se a view está attached to window
    if (!isAttachedToWindow) return

    this.animate()
        .translationY(-20f)
        .setDuration(300)
        .withEndAction {
            this.animate()
                .translationY(0f)
                .setDuration(300)
                .start()
        }
        .start()
}

// Extensões adicionais úteis
fun View.show() {
    this.isVisible = true
}

fun View.hide() {
    this.isVisible = false
}

fun View.toggleVisibility() {
    this.isVisible = !this.isVisible
}

fun View.fadeIn(duration: Long = 300) {
    this.animate()
        .alpha(1f)
        .setDuration(duration)
        .start()
}

fun View.fadeOut(duration: Long = 300) {
    this.animate()
        .alpha(0f)
        .setDuration(duration)
        .start()
}