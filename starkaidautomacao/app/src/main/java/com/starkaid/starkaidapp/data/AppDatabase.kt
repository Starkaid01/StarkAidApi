package com.starkaid.starkaidapp.data

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import com.starkaid.starkaidapp.models.AppConfigDao
import com.starkaid.starkaidapp.models.AppConfigEntity
import com.starkaid.starkaidapp.models.ComandoSocialDao
import com.starkaid.starkaidapp.models.ComandoSocialEntity
import com.starkaid.starkaidapp.models.ContatoDao
import com.starkaid.starkaidapp.models.ContatoEntity
import com.starkaid.starkaidapp.models.LogToSuporteDao
import com.starkaid.starkaidapp.models.LogToSuporteEntity

@Database(
    entities = [
        ComandoSocialEntity::class,
        ContatoEntity::class,
        AppConfigEntity::class,
        LogToSuporteEntity::class
    ],
    version = 8  // Aumente a versão devido à mudança de entidade
)
abstract class AppDatabase : RoomDatabase() {
    abstract fun comandoSocialDao(): ComandoSocialDao
    abstract fun contatoDao(): ContatoDao
    abstract fun appConfigDao(): AppConfigDao
    abstract fun logToSuporteDao(): LogToSuporteDao

    companion object {
        @Volatile
        private var INSTANCE: AppDatabase? = null

        fun getInstance(context: Context): AppDatabase {
            return INSTANCE ?: synchronized(this) {
                val instance = Room.databaseBuilder(
                    context.applicationContext,
                    AppDatabase::class.java,
                    "app_database"
                )
                    .fallbackToDestructiveMigration(true)
                    .build()
                INSTANCE = instance
                instance
            }
        }
    }
}