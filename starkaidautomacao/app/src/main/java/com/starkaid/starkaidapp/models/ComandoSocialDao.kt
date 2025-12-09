package com.starkaid.starkaidapp.models

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import kotlinx.coroutines.flow.Flow

@Dao
interface ComandoSocialDao {

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertAll(comandos: List<ComandoSocialEntity>)

    @Query("SELECT * FROM comandos_sociais")
    suspend fun getAll(): List<ComandoSocialEntity>

    @Query("DELETE FROM comandos_sociais")
    suspend fun deleteAll()

    @Query("SELECT * FROM comandos_sociais")
    fun getAllFlow(): Flow<List<ComandoSocialEntity>>
}
