package com.starkaid.starkaidapp.services.pipeline

sealed class StageResult {
    object Handled : StageResult()
    object StopPipeline : StageResult()
    object Pass : StageResult()
}
