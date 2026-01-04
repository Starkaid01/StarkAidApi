package com.starkaid.starkaidapp.services.pipeline

interface CommandStage {
    suspend fun process(ctx: CommandContext): StageResult
}
