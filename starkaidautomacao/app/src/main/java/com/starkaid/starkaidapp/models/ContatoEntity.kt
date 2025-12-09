package com.starkaid.starkaidapp.models

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "contatos")
data class ContatoEntity(
    @PrimaryKey val numero: String,
    val nome: String
)