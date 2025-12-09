package com.starkaid.starkaidapp.models

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface LogToSuporteDao {
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertLog(log: LogToSuporteEntity): Long

    @Query("SELECT * FROM logsToSuporte ORDER BY dataErro DESC, horaErro DESC")
    suspend fun getAllLogs(): List<LogToSuporteEntity>

    @Query("SELECT * FROM logsToSuporte WHERE id = :id")
    suspend fun getLogById(id: Int): LogToSuporteEntity?

    @Query("DELETE FROM logsToSuporte")
    suspend fun deleteAllLogs()

    @Query("SELECT COUNT(*) FROM logsToSuporte")
    suspend fun getLogCount(): Int
}

