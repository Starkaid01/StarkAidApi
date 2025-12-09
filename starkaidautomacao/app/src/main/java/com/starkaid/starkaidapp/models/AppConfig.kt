package com.starkaid.starkaidapp.models
import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "app_config")
data class AppConfig(
    @PrimaryKey
    val configKey: String,  // Mude de "key" para "configKey"
    val value: String
)