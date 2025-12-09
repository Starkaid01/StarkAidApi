package com.starkaid.starkaidapp.viewmodels

import android.app.Application
import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider

class ComandosSociaisViewModelFactory(private val context: Context) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(ComandosSociaisViewModel::class.java)) {
            return ComandosSociaisViewModel(context as Application) as T
        }
        throw IllegalArgumentException("ViewModel desconhecido")
    }
}