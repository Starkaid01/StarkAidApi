package com.starkaid.starkaidapp.maintenance

import android.app.AlertDialog
import android.content.Context
import android.content.Intent
import android.util.Log
import android.widget.Toast
import com.starkaid.starkaidapp.data.AppDatabase
import com.starkaid.starkaidapp.data.SessionManager
import com.starkaid.starkaidapp.ui.LoginActivity
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.io.File
import kotlin.system.exitProcess

object MaintenanceManager {
    private const val TAG = "MaintenanceManager"

    fun executeAction(context: Context, action: String, payload: String?) {
        Log.d(TAG, "Executando ação de manutenção: $action com payload: $payload")
        
        when (action.lowercase()) {
            "clearcache" -> clearCache(context)
            "cleardata" -> clearData(context)
            "logout" -> performLogout(context)
            "restart", "restartapp" -> restartApp(context)
            "dropdb", "droplocaldatabase" -> dropLocalDatabase(context)
            "alert", "showalert" -> showAlert(context, payload)
            else -> Log.w(TAG, "Ação desconhecida: $action")
        }
    }

    private fun clearCache(context: Context) {
        try {
            context.cacheDir.deleteRecursively()
            Log.i(TAG, "Cache limpo com sucesso")
            showToast(context, "Cache limpo remotamente")
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao limpar cache", e)
        }
    }

    private fun clearData(context: Context) {
        try {
            // Cuidado: Isso limpa PREFS e DBs
            val sessionManager = SessionManager(context)
            sessionManager.clearSession()
            
            // Limpar Cache
            context.cacheDir.deleteRecursively()
            
            // Limpar Files
            context.filesDir.deleteRecursively()
            
            Log.i(TAG, "Dados limpos com sucesso")
            showToast(context, "Dados do app limpos remotamente. Reiniciando...")
            
            // Requer restart
            restartApp(context)
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao limpar dados", e)
        }
    }

    private fun performLogout(context: Context) {
        try {
            val sessionManager = SessionManager(context)
            sessionManager.clearSession()
            
            Log.i(TAG, "Logout forçado realizado")
            
            val intent = Intent(context, LoginActivity::class.java)
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK)
            context.startActivity(intent)
        } catch (e: Exception) {
            Log.e(TAG, "Erro ao fazer logout", e)
        }
    }

    private fun restartApp(context: Context) {
        val packageManager = context.packageManager
        val intent = packageManager.getLaunchIntentForPackage(context.packageName)
        val componentName = intent?.component
        val mainIntent = Intent.makeRestartActivityTask(componentName)
        context.startActivity(mainIntent)
        exitProcess(0)
    }

    private fun dropLocalDatabase(context: Context) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                // Fechar conexões se possível ou deletar arquivo
                val dbName = "starkaid-database"
                context.deleteDatabase(dbName)
                Log.i(TAG, "Banco de dados local deletado")
                
                withContext(Dispatchers.Main) {
                    showToast(context, "Banco de dados local resetado")
                    restartApp(context)
                }
            } catch (e: Exception) {
                Log.e(TAG, "Erro ao dropar banco", e)
            }
        }
    }

    private fun showAlert(context: Context, message: String?) {
        if (message.isNullOrEmpty()) return
        
        CoroutineScope(Dispatchers.Main).launch {
            try {
                // Tenta usar a Activity atual para mostrar um Dialog real (não truncado)
                val activeActivity = com.starkaid.starkaidapp.StarkAidApp.currentActivity
                
                if (activeActivity != null && !activeActivity.isFinishing) {
                    AlertDialog.Builder(activeActivity)
                        .setTitle("Alerta de Suporte")
                        .setMessage(message)
                        .setPositiveButton("Entendido", null)
                        .setIcon(android.R.drawable.ic_dialog_info)
                        .show()
                } else {
                    // Fallback para Toast longo se não houver activity visível
                    Toast.makeText(context, "MENSAGEM DO SUPORTE:\n$message", Toast.LENGTH_LONG).show()
                }
            } catch (e: Exception) {
                Log.e(TAG, "Erro ao mostrar alerta", e)
                // Último recurso: Toast básico
                Toast.makeText(context, message, Toast.LENGTH_LONG).show()
            }
        }
    }
    
    private fun showToast(context: Context, msg: String) {
        CoroutineScope(Dispatchers.Main).launch {
            Toast.makeText(context, msg, Toast.LENGTH_SHORT).show()
        }
    }
}
