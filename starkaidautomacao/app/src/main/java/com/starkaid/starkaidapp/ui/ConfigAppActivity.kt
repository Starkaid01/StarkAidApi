package com.starkaid.starkaidapp.ui

import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.core.widget.NestedScrollView
import androidx.lifecycle.lifecycleScope
import com.google.android.material.textfield.TextInputEditText
import com.starkaid.starkaidapp.data.AppDatabase
import com.starkaid.starkaidapp.databinding.ActivityConfigAppBinding
import com.starkaid.starkaidapp.models.AppConfig
import kotlinx.coroutines.launch

class ConfigAppActivity : AppCompatActivity() {

    private lateinit var binding: ActivityConfigAppBinding
    private lateinit var database: AppDatabase
    private var isKeyboardOpen = false

    companion object {
        private const val KEY_ASSISTANT_NAME = "assistant_name"
        private const val KEY_DEFAULT_RESPONSE = "default_response"
        private const val KEY_PERSONALITY = "personality"

        private const val DEFAULT_ASSISTANT_NAME = "Assistente"
        private const val DEFAULT_RESPONSE = "Desculpe, não entendi. Pode reformular?"
        private const val DEFAULT_PERSONALITY = "Descolada"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityConfigAppBinding.inflate(layoutInflater)
        setContentView(binding.root)

        database = AppDatabase.getInstance(this)

        setupUI()
        setupKeyboardVisibilityListener()
        loadCurrentSettings()
    }

    private fun setupUI() {
        // Configurar click listeners para os EditTexts
        binding.etAssistantName.setOnClickListener { scrollToEditText(binding.etAssistantName) }
        binding.etDefaultResponse.setOnClickListener { scrollToEditText(binding.etDefaultResponse) }
        binding.etPersonality.setOnClickListener { scrollToEditText(binding.etPersonality) }

        // Configurar focus listeners
        binding.etAssistantName.setOnFocusChangeListener { _, hasFocus ->
            if (hasFocus) scrollToEditText(binding.etAssistantName)
        }
        binding.etDefaultResponse.setOnFocusChangeListener { _, hasFocus ->
            if (hasFocus) scrollToEditText(binding.etDefaultResponse)
        }
        binding.etPersonality.setOnFocusChangeListener { _, hasFocus ->
            if (hasFocus) scrollToEditText(binding.etPersonality)
        }

        // Botão para atualizar nome do assistente
        binding.btnUpdateName.setOnClickListener {
            val newName = binding.etAssistantName.text.toString().trim()
            if (newName.isNotEmpty()) {
                updateAssistantName(newName)
            } else {
                binding.etAssistantName.error = "Digite um nome para o assistente"
            }
        }

        // Botão para atualizar resposta padrão
        binding.btnUpdateResponse.setOnClickListener {
            val newResponse = binding.etDefaultResponse.text.toString().trim()
            if (newResponse.isNotEmpty()) {
                updateDefaultResponse(newResponse)
            } else {
                binding.etDefaultResponse.error = "Digite uma resposta padrão"
            }
        }

        // Botão para atualizar personalidade
        binding.btnUpdatePersonality.setOnClickListener {
            val newPersonality = binding.etPersonality.text.toString().trim()
            if (newPersonality.isNotEmpty()) {
                updatePersonality(newPersonality)
            } else {
                binding.etPersonality.error = "Digite uma personalidade"
            }
        }
    }

    private fun setupKeyboardVisibilityListener() {
        val rootView = findViewById<View>(android.R.id.content)
        rootView.viewTreeObserver.addOnGlobalLayoutListener {
            val rect = android.graphics.Rect()
            rootView.getWindowVisibleDisplayFrame(rect)
            val screenHeight = rootView.height
            val keypadHeight = screenHeight - rect.bottom

            val wasKeyboardOpen = isKeyboardOpen
            isKeyboardOpen = keypadHeight > screenHeight * 0.15 // 15% da tela

            if (isKeyboardOpen && !wasKeyboardOpen) {
                // Teclado acabou de abrir
                handleKeyboardOpened()
            }
        }
    }

    private fun handleKeyboardOpened() {
        // Encontra qual EditText está com foco e scrolla para ele
        when {
            binding.etAssistantName.hasFocus() -> scrollToEditText(binding.etAssistantName)
            binding.etDefaultResponse.hasFocus() -> scrollToEditText(binding.etDefaultResponse)
            binding.etPersonality.hasFocus() -> scrollToEditText(binding.etPersonality)
        }
    }

    private fun scrollToEditText(editText: TextInputEditText) {
        val scrollView = binding.root as? NestedScrollView
        scrollView?.postDelayed({
            // Calcula a posição para scroll
            val location = IntArray(2)
            editText.getLocationInWindow(location)
            val scrollY = location[1] - getScrollOffset()

            scrollView.smoothScrollTo(0, scrollY)
        }, 100)
    }

    private fun getScrollOffset(): Int {
        // Retorna um offset baseado na densidade da tela
        return (150 * resources.displayMetrics.density).toInt()
    }

    private fun loadCurrentSettings() {
        lifecycleScope.launch {
            try {
                // Carrega o nome atual do assistente
                val currentName = database.appConfigDao().getConfig(KEY_ASSISTANT_NAME)
                binding.tvCurrentName.text = currentName ?: DEFAULT_ASSISTANT_NAME
                binding.etAssistantName.setText(currentName ?: DEFAULT_ASSISTANT_NAME)

                // Carrega a resposta padrão atual
                val currentResponse = database.appConfigDao().getConfig(KEY_DEFAULT_RESPONSE)
                binding.tvCurrentResponse.text = currentResponse ?: DEFAULT_RESPONSE
                binding.etDefaultResponse.setText(currentResponse ?: DEFAULT_RESPONSE)

                // Carrega a personalidade atual
                val currentPersonality = database.appConfigDao().getConfig(KEY_PERSONALITY)
                binding.tvCurrentPersonality.text = currentPersonality ?: DEFAULT_PERSONALITY
                binding.etPersonality.setText(currentPersonality ?: DEFAULT_PERSONALITY)

            } catch (e: Exception) {
                e.printStackTrace()
                // Em caso de erro, mostra valores padrão
                setDefaultValues()
            }
        }
    }

    private fun updateAssistantName(newName: String) {
        lifecycleScope.launch {
            try {
                database.appConfigDao().saveConfig(AppConfig(configKey = KEY_ASSISTANT_NAME, value = newName))
                binding.tvCurrentName.text = newName
                showSuccessMessage("Nome do assistente atualizado com sucesso!")
            } catch (e: Exception) {
                e.printStackTrace()
                showErrorMessage("Erro ao atualizar nome do assistente")
            }
        }
    }

    private fun updateDefaultResponse(newResponse: String) {
        lifecycleScope.launch {
            try {
                database.appConfigDao().saveConfig(AppConfig(configKey = KEY_DEFAULT_RESPONSE, value = newResponse))
                binding.tvCurrentResponse.text = newResponse
                showSuccessMessage("Resposta padrão atualizada com sucesso!")
            } catch (e: Exception) {
                e.printStackTrace()
                showErrorMessage("Erro ao atualizar resposta padrão")
            }
        }
    }

    private fun updatePersonality(newPersonality: String) {
        lifecycleScope.launch {
            try {
                database.appConfigDao().saveConfig(AppConfig(configKey = KEY_PERSONALITY, value = newPersonality))
                binding.tvCurrentPersonality.text = newPersonality
                showSuccessMessage("Personalidade atualizada com sucesso!")
            } catch (e: Exception) {
                e.printStackTrace()
                showErrorMessage("Erro ao atualizar personalidade")
            }
        }
    }

    private fun setDefaultValues() {
        binding.tvCurrentName.text = DEFAULT_ASSISTANT_NAME
        binding.etAssistantName.setText(DEFAULT_ASSISTANT_NAME)

        binding.tvCurrentResponse.text = DEFAULT_RESPONSE
        binding.etDefaultResponse.setText(DEFAULT_RESPONSE)

        binding.tvCurrentPersonality.text = DEFAULT_PERSONALITY
        binding.etPersonality.setText(DEFAULT_PERSONALITY)
    }

    private fun showSuccessMessage(message: String) {
        android.widget.Toast.makeText(this, message, android.widget.Toast.LENGTH_SHORT).show()
    }

    private fun showErrorMessage(message: String) {
        android.widget.Toast.makeText(this, message, android.widget.Toast.LENGTH_LONG).show()
    }
}