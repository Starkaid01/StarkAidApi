package com.starkaid.starkaidapp.models

data class AnaliseTexto(
    val textoLimpo: String,
    val ehPergunta: Boolean,
    val nivelPergunta: Double,
    val comandoAutomacao: ComandoAutomacao?,
    val ehSocial: Boolean,
    val tipoSocial: String?,
    val eParcial: Boolean = false
)


data class ComandoAutomacao(
    val acao: String?,       // "ligar", "desligar", etc.
    val dispositivo: String?
)