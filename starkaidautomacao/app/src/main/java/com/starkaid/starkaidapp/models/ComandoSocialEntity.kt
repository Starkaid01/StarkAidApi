package com.starkaid.starkaidapp.models

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "comandos_sociais")
data class ComandoSocialEntity(
    @PrimaryKey val id: String,
    val userId: String,
    val comando: String,
    val resposta: String,
    val respostasAleatorias: String? // armazenamos como string JSON
)

