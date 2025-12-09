package com.starkaid.starkaidapp.models

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "logsToSuporte")
data class LogToSuporteEntity(
    @PrimaryKey(autoGenerate = true) val id: Int = 0,
    val ultimoComando: String? = null,
    val ultimaResposta: String? = null,
    val ultimoDispositivoAcionado: String? = null,
    val erroCompleto: String? = null,
    val codigoDeErro: String? = null,
    val dataErro: String, // formato yyyy-MM-dd
    val horaErro: String, // formato HH:mm:ss
    val acaoErro: String // ação que estava ocorrendo quando o erro aconteceu
)

