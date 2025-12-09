package com.starkaid.starkaidapp.services

data class CommandRequest(
    val deviceId: String,
    val command: String,
    val customCommand: String = ""
)