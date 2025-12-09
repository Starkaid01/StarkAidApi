package com.starkaid.starkaidapp.models

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query

@Dao
interface ContatoDao {
    @Query("SELECT * FROM contatos")
    suspend fun getAll(): List<ContatoEntity>

    @Insert(onConflict = OnConflictStrategy.IGNORE)
    suspend fun insertAll(contatos: List<ContatoEntity>)

    @Insert(onConflict = OnConflictStrategy.IGNORE)
    suspend fun insertContato(contato: ContatoEntity)
}