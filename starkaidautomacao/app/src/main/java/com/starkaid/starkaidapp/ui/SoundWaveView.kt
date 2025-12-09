package com.starkaid.starkaidapp.ui

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.Path
import android.util.AttributeSet
import android.view.View
import android.view.animation.AccelerateDecelerateInterpolator
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.collect
import kotlin.math.sin
import kotlin.math.abs
import kotlin.random.Random

class SoundWaveView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    private val waves = mutableListOf<Wave>()
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        strokeWidth = 4f
        style = Paint.Style.STROKE
        color = Color.parseColor("#00D9FF")
        alpha = 200
    }

    private var animationJob: Job? = null
    private var timeOffset = 0f
    private var currentAudioLevel = 0f // 0-100
    private val targetAudioLevel = kotlinx.coroutines.flow.MutableStateFlow(0f)

    private data class Wave(
        var x: Float,
        var y: Float,
        var amplitude: Float,
        var frequency: Float,
        var phase: Float,
        var speed: Float,
        var baseAmplitude: Float = amplitude
    )

    private var isAnimating = false

    init {
        setBackgroundColor(Color.TRANSPARENT)
    }

    override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
        super.onSizeChanged(w, h, oldw, oldh)
        // Reinicializar ondas quando o tamanho mudar
        if (isAnimating && waves.isEmpty()) {
            initializeWaves()
        }
    }

    private fun initializeWaves() {
        waves.clear()
        if (width == 0 || height == 0) return
        
        repeat(4) { // Menos ondas ainda
            waves.add(
                Wave(
                    x = (width / 5f) * (it + 1),
                    y = height / 2f,
                    amplitude = Random.nextFloat() * 15f + 8f, // Amplitudes bem menores
                    frequency = Random.nextFloat() * 0.025f + 0.015f,
                    phase = Random.nextFloat() * 2f * kotlin.math.PI.toFloat(),
                    speed = Random.nextFloat() * 0.3f + 0.2f, // Velocidades ainda mais rápidas
                    baseAmplitude = Random.nextFloat() * 15f + 8f
                )
            )
        }
    }

    fun startAnimation() {
        stopAnimation()
        isAnimating = true
        
        // Inicializar ondas se o tamanho já estiver disponível
        if (width > 0 && height > 0) {
            initializeWaves()
        }

        animationJob = CoroutineScope(Dispatchers.Main).launch {
            // Aguardar até que a view tenha tamanho
            while (width == 0 || height == 0) {
                delay(50)
            }
            
            // Garantir que as ondas foram inicializadas
            if (waves.isEmpty()) {
                initializeWaves()
            }
            
            // Suavizar transições do nível de áudio com resposta mais rápida
            var smoothedLevel = 0f
            
            launch {
                targetAudioLevel.collect { targetLevel ->
                    // Interpolação muito mais rápida para resposta imediata
                    val smoothingFactor = if (targetLevel > smoothedLevel) 0.7f else 0.5f
                    while (abs(smoothedLevel - targetLevel) > 1f) {
                        smoothedLevel += (targetLevel - smoothedLevel) * smoothingFactor
                        currentAudioLevel = smoothedLevel
                        delay(4) // Atualização muito mais rápida
                    }
                    currentAudioLevel = targetLevel
                }
            }
            
            // Loop principal de animação
            while (isActive && isAnimating) {
                timeOffset += 0.25f // Movimento bem mais rápido
                
                // Sem mínimo forçado - mais realista
                val audioFactor = (currentAudioLevel / 100f).coerceIn(0f, 1f)
                
                // Atualizar amplitudes das ondas - resposta mais rápida e sutil
                waves.forEachIndexed { index, wave ->
                    // Multiplicador bem mais controlado para não tampar a tela
                    val audioMultiplier = 0.9f + audioFactor * (1.2f + index * 0.15f)
                    wave.amplitude = wave.baseAmplitude * audioMultiplier.coerceIn(0.8f, 2.5f) // Limitar máximo
                    
                    // Velocidade varia mais com o áudio para ritmo
                    val baseSpeed = 0.15f + index * 0.05f
                    wave.speed = baseSpeed * (1f + audioFactor * 1.2f)
                    
                    // Fase varia mais rápido para movimento mais dinâmico
                    wave.phase += audioFactor * 0.12f
                    
                    // Movimento base menor
                    wave.phase += 0.03f
                }
                
                invalidate()
                delay(16) // ~60 FPS
            }
        }
    }
    
    fun updateAudioLevel(level: Int) {
        // Aplicar mudanças mais rapidamente, menos suavização
        val clampedLevel = level.coerceIn(0, 100).toFloat()
        if (abs(targetAudioLevel.value - clampedLevel) > 0.5f) {
            targetAudioLevel.value = clampedLevel
        }
    }

    fun stopAnimation() {
        isAnimating = false
        animationJob?.cancel()
        animationJob = null
        timeOffset = 0f
        waves.clear()
        currentAudioLevel = 0f
        targetAudioLevel.value = 0f
        invalidate()
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)

        if (waves.isEmpty() || width == 0 || height == 0) return

        // Desenhar cada onda
        waves.forEachIndexed { index, wave ->
            val path = Path()
            val waveColor = when (index % 3) {
                0 -> Color.parseColor("#00D9FF")
                1 -> Color.parseColor("#00CCFF")
                else -> Color.parseColor("#66E5FF")
            }
            paint.color = waveColor

            val startX = 0f
            val endX = width.toFloat()
            val step = 3f // Step ainda menor para ondas mais suaves e detalhadas

            path.moveTo(startX, wave.y)

            var x = startX
            while (x <= endX) {
                val relativeX = (x - wave.x) * wave.frequency
                // Variação mais dramática baseada no nível de áudio
                val audioVariation = (currentAudioLevel / 100f) * 0.8f
                // Adicionar múltiplas frequências para ondas mais complexas
                val wave1 = kotlin.math.sin(relativeX + wave.phase + timeOffset * wave.speed)
                val wave2 = kotlin.math.sin((relativeX + wave.phase) * 2f + timeOffset * wave.speed * 1.3f) * 0.3f
                val combinedWave = (wave1 + wave2) * (1f + audioVariation * 0.5f)
                val y = wave.y + combinedWave * wave.amplitude
                path.lineTo(x, y)
                x += step
            }

            // Ajustar espessura e opacidade baseado no áudio - bem mais sutil
            val audioFactor = (currentAudioLevel / 100f).coerceIn(0f, 1f)
            paint.strokeWidth = 1f + audioFactor * 2f // Linhas bem mais finas
            paint.alpha = (40 + audioFactor * 80).toInt().coerceIn(30, 120) // Muito mais transparente
            canvas.drawPath(path, paint)
            
            // Adicionar pontos brilhantes bem mais sutis (opcional - pode remover)
            paint.style = Paint.Style.FILL
            paint.alpha = (30 + audioFactor * 60).toInt().coerceIn(15, 90) // Muito transparente
            for (i in 0..10) { // Ainda menos pontos
                val pointX = (width / 10f) * i
                val relativeX = (pointX - wave.x) * wave.frequency
                val audioVariation = audioFactor * 0.4f
                val wave1 = kotlin.math.sin(relativeX + wave.phase + timeOffset * wave.speed)
                val wave2 = kotlin.math.sin((relativeX + wave.phase) * 2f + timeOffset * wave.speed * 1.3f) * 0.3f
                val combinedWave = (wave1 + wave2) * (1f + audioVariation * 0.2f)
                val pointY = wave.y + combinedWave * wave.amplitude
                // Tamanho dos pontos bem menor
                val pointSize = 1.5f + audioFactor * 3f
                canvas.drawCircle(pointX, pointY, pointSize, paint)
            }
            paint.style = Paint.Style.STROKE
            paint.strokeWidth = 4f
        }
    }

    override fun onDetachedFromWindow() {
        super.onDetachedFromWindow()
        stopAnimation()
    }
}

