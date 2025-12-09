package com.starkaid.starkaidapp.services

import android.util.Log
import com.starkaid.starkaidapp.models.AnaliseTexto
import com.starkaid.starkaidapp.models.ComandoAutomacao
import java.text.Normalizer

class AnalizaTexto {
    fun analisarTexto(text: String): AnaliseTexto {
        return analisarTextoInterno(text)
    }
}

private fun analisarTextoInterno(text: String): AnaliseTexto {
    var eparcial = false

    Log.i("AnalizaTexto", "Texto recebido: $text")

    var textoLimpo = run {
        val sb = StringBuilder()
        val semAcento1 = Normalizer.normalize(text, Normalizer.Form.NFD)
        var semAcento = semAcento1

        if (semAcento1.contains("parcial:")) {
            eparcial = true
            semAcento = semAcento1.replace("parcial:", "")
        }

        for (c in semAcento) {
            when {
                c.isLetterOrDigit() -> sb.append(c.lowercaseChar())
                c.isWhitespace() -> if (sb.isNotEmpty() && sb.last() != ' ') sb.append(' ')
            }
        }
        sb.toString().trim()
    }

    var score = 0.0

    val palavrasIniciais = setOf("quem", "que", "qual", "quais", "quando", "onde", "como",
        "porque", "por", "quanto", "quanta", "quantos", "quantas", "aonde", "sera")

    val frasesIniciais = setOf("o que é", "pra que", "para que", "sobre o que", "do que estavamos falando",
        "voce sabe", "onde fica", "onde se localiza", "em qual", "em que", "para onde",
        "pesquise", "busque", "encontre")

    val palavrasMeio = setOf("como", "por", "qual", "quais", "quanto", "quanta", "quantos", "quantas")
    val padroesDuv = setOf("saber", "querer", "preciso", "diga", "explique", "conte", "mostre", "me", "poderia", "sera")

    val palavras = textoLimpo.split(" ")

    if (frasesIniciais.any { textoLimpo.startsWith(it) }) {
        score += 0.7
    }

    for ((index, palavra) in palavras.withIndex()) {
        when {
            index == 0 && palavrasIniciais.contains(palavra) -> score += 0.7
            index > 0 && palavrasMeio.contains(palavra) -> score += 0.2
            padroesDuv.contains(palavra) -> score += 0.1
        }
    }

    if (score > 1.0) score = 1.0
    val ehPergunta = score >= 0.4
    val nivelPergunta = score

    val sociais = mapOf(
        "saudacao" to listOf("oi", "ola", "e ai", "fala ai", "bom dia", "boa tarde", "boa noite", "opa", "salve"),
        "despedida" to listOf("tchau", "ate mais", "falou", "ate logo", "ate breve", "boa noite", "durma bem"),
        "pessoal" to listOf("tudo bem", "como vai voce", "eu me chamo", "meu nome e", "este e meu amigo"),
        "sentimento" to listOf("me siento", "estou feliz", "estou triste", "estou cansado", "estou chateado", "estou animado", "estou nervoso", "tenho medo", "tenho vergonha"),
        "humor" to listOf("conte uma piada", "me conta uma piada", "piadinha", "brinca comigo", "fala uma curiosidade"),
        "identidade" to listOf("quem e voce", "quem te criou", "do que voce gosta", "voce gosta de", "me fala sobre voce"),
        "pedido" to listOf("fala comigo", "me ajuda", "me explica", "me ensina", "responde", "me diga"),
        "hobby" to listOf("eu gosto de", "adoro", "prefiro", "meu hobby", "curto", "sou fã de", "eu jogo", "eu assisto", "eu leio")
    )

    var ehSocial = false
    var tipoSocial: String? = null
    loop@ for ((categoria, frases) in sociais) {
        for (f in frases) {
            if (textoLimpo.contains(f)) {
                ehSocial = true
                tipoSocial = categoria
                break@loop
            }
        }
    }

    val acoes = mapOf(
        "atualizar" to listOf("atualizar","atualiza", "atualize"),
        "ligar" to listOf("ligar", "liga", "ligue", "acender", "acenda", "acende", "ativar", "ative", "acionar", "iniciar", "ativar", "ative", "acionar", "ativacao"),
        "desligar" to listOf("desligar", "desliga", "desligue", "apagar", "apague", "apaga", "desativar", "desative", "parar", "desativar", "desative", "desacionar"),
        "aumentar" to listOf("aumentar", "aumenta", "subir", "elevar", "intensificar", "aumente"),
        "diminuir" to listOf("diminuir", "diminui", "baixar", "baixo", "reduzir", "abaixar", "baixa", "baixe", "abaixe", "abaixa"),
        "abrir" to listOf("abrir", "abre", "destravar", "liberar", "abra"),
        "fechar" to listOf("fechar", "fecha", "travar", "bloquear", "feche"),
        "sair" to listOf("sair", "sai", "saia"),
        "tocar" to listOf("tocar", "toca", "toque", "reproduzir"),
        "mensagem" to listOf("mensagem", "mensage"),
        "mandar" to listOf("mandar", "manda", "mande"),
        "parar" to listOf("pausar", "pare", "para", "parar", "desligar", "desliga",  "desligue", "interromper"),
        "resetar" to listOf("resetar", "reiniciar", "reinicie", "reinicializar"),
        "limpar" to listOf("limpar", "limpa", "limpe", "reinicializar"),
        "cade" to listOf("cade", "onde esta", "onde voce"),
        "escrever" to listOf("escrever", "escreve", "escreva"),
        "otimizar" to listOf("otimizar", "otimize", "optmizar")
    )

    var acoesEncontradas = mutableSetOf<String>()
    var dispositivoEncontrado: String? = null

    val acoesOrdenadas = acoes.entries.sortedByDescending { it.value.maxOf { s -> s.length } }

    for ((key, sinonimos) in acoesOrdenadas) {
        for (s in sinonimos) {
            val padraoEscapado = java.util.regex.Pattern.quote(s)
            val regex = "\\b$padraoEscapado\\b".toRegex(RegexOption.IGNORE_CASE)

            Log.i("AnalizaTexto", "Testando ação: '$s' no texto: '$textoLimpo'")

            val match = regex.find(textoLimpo)
            if (match != null) {
                Log.i("AnalizaTexto", "✅ Ação encontrada: $key com sinônimo: $s")
                acoesEncontradas.add(key)

                // Atualiza o dispositivo com base na última ação encontrada
                dispositivoEncontrado = textoLimpo.substring(match.range.last + 1).trim()
                    .takeIf { it.isNotBlank() }
                    ?.replaceFirst(
                        "^\\s*(a|o|os|as|um|uma|uns|umas|de|da|do|das|dos)\\s+".toRegex(),
                        ""
                    )
                    ?.trim()

                // Caso especial para "cade"
                if (dispositivoEncontrado.isNullOrBlank() && key == "cade") {
                    val cadeRegex = "\\b(cade|onde esta|onde voce)\\s+(.+)$".toRegex()
                    val cadeMatch = cadeRegex.find(textoLimpo)
                    dispositivoEncontrado = cadeMatch?.groupValues?.get(2)?.trim()
                }
            }
        }
    }

    val acaoFinal = if (acoesEncontradas.isNotEmpty()) acoesEncontradas.joinToString("|") else null

    if (acaoFinal != null) {
        Log.i("AnalizaTexto", "🔧 Ações: $acaoFinal, Dispositivo: $dispositivoEncontrado")
    } else {
        Log.i("AnalizaTexto", "❌ Nenhuma ação encontrada")
    }

    val comando = if (acaoFinal != null) {
        ComandoAutomacao(
            acao = acaoFinal,
            dispositivo = dispositivoEncontrado
        )
    } else null


    if (eparcial) {
        textoLimpo = "parcial:$textoLimpo"
    }

    return AnaliseTexto(
        textoLimpo = textoLimpo,
        ehPergunta = ehPergunta,
        nivelPergunta = nivelPergunta,
        comandoAutomacao = comando,
        ehSocial = ehSocial,
        tipoSocial = tipoSocial,
        eParcial = eparcial
    )
}