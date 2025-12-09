package com.starkaid.starkaidapp.utils

import java.text.Normalizer

object StringUtils {
    fun normalizarNome(nome: String): String {
        var s = nome.lowercase()
        s = Normalizer.normalize(s, Normalizer.Form.NFD)
        s = s.replace(Regex("\\p{InCombiningDiacriticalMarks}+"), "")
        s = s.replace(Regex("[^a-z0-9\\s]"), "")
        s = s.replace(Regex("\\s+"), " ").trim()
        return s
    }
}
