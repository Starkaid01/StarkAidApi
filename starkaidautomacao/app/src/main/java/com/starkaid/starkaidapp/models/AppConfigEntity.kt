package com.starkaid.starkaidapp.models

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "app_config")
data class AppConfigEntity(
    @PrimaryKey val configKey: String,
    val value: String
)

