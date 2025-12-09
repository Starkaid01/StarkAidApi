package com.starkaid.starkaidapp.ui

import android.os.Bundle
import android.util.Log
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import com.starkaid.starkaidapp.base.BaseActivity
import com.starkaid.starkaidapp.screens.ComandosSociaisScreen
import com.starkaid.starkaidapp.ui.theme.StarkAidAppTheme
import com.starkaid.starkaidapp.viewmodels.ComandosSociaisViewModel
import com.starkaid.starkaidapp.viewmodels.ComandosSociaisViewModelFactory

class ComandosSociaisActivity : BaseActivity() {
    private val viewModel: ComandosSociaisViewModel by viewModels {
        ComandosSociaisViewModelFactory(application)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        Log.d("ComandosSociais", "Activity onCreate")

        // Configurar a seta de voltar na ActionBar
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.setDisplayShowHomeEnabled(true)

        setContent {
            Log.d("ComandosSociais", "Compose setContent")
            StarkAidAppTheme {
                Surface(
                    color = MaterialTheme.colorScheme.background,
                    modifier = Modifier.fillMaxSize()
                ) {
                    Log.d("ComandosSociais", "Compose content")
                    ComandosSociaisScreen(
                        viewModel = viewModel,
                        onBackPressed = { onBackPressed() } // Passar a função de voltar
                    )
                }
            }
        }
    }

    // Adicionar este método para lidar com o clique no botão de voltar
    override fun onSupportNavigateUp(): Boolean {
        onBackPressed()
        return true
    }
}