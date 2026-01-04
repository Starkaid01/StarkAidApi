package com.starkaid.starkaidapp.services.pipeline

import android.util.Log

class PipelineEngine(private val stages: List<CommandStage>) {
    
    suspend fun execute(ctx: CommandContext): Boolean {
        Log.d("Pipeline", "Iniciando processamento: '${ctx.input.cleanText}' (Partial: ${ctx.input.isPartial}, Escutando: ${ctx.session.escutando.get()})")
        
        val startTimeTotal = System.currentTimeMillis()
        var handled = false
        var stageThatHandled: String? = null
        
        for (stage in stages) {
            val startTimeStage = System.currentTimeMillis()
            val result = stage.process(ctx)
            val durationStage = System.currentTimeMillis() - startTimeStage
            
            Log.d("PipelineTrace", "Stage: ${stage.javaClass.simpleName} | Duration: ${durationStage}ms | Result: ${result.javaClass.simpleName} | Kind: ${ctx.kind}")
            
            when (result) {
                is StageResult.Handled -> {
                    Log.d("Pipeline", "✅ Stage ${stage.javaClass.simpleName} tratou o comando em ${durationStage}ms. (Kind: ${ctx.kind})")
                    handled = true
                    stageThatHandled = stage.javaClass.simpleName
                    break
                }
                is StageResult.StopPipeline -> {
                    Log.d("Pipeline", "⛔ Stage ${stage.javaClass.simpleName} abortou o pipeline em ${durationStage}ms.")
                    handled = false
                    break
                }
                is StageResult.Pass -> {
                    // Continue
                }
            }
        }
        
        val durationTotal = System.currentTimeMillis() - startTimeTotal
        Log.d("Pipeline", "Fim do processamento. Handled: $handled, Duração total: ${durationTotal}ms")
        
        // Enviar Telemetria Unificada
        if (!ctx.input.isPartial) {
            val metadata = mutableMapOf<String, Any>(
                "texto" to ctx.input.rawText,
                "durationMs" to durationTotal,
                "handled" to handled,
                "kind" to ctx.kind.name
            )
            stageThatHandled?.let { metadata["stage"] = it }
            ctx.analysis?.let { metadata["isQuestion"] = it.ehPergunta }
            
            ctx.actions.sendTelemetry(
                evento = if (handled) "command_handled" else "command_ignored",
                categoria = "comando",
                metadata = metadata
            )
        }
        
        return handled
    }
}
