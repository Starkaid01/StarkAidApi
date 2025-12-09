package com.starkaid.starkaidapp.services

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.net.ConnectivityManager
import com.starkaid.starkaidapp.viewmodels.ComandosSociaisViewModel
import com.starkaid.starkaidapp.viewmodels.ComandosSociaisViewModelFactory

class NetworkChangeReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (ConnectivityManager.CONNECTIVITY_ACTION == intent.action) {
            val connectivityManager = context.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
            val networkInfo = connectivityManager.activeNetworkInfo

            if (networkInfo != null && networkInfo.isConnected) {
                // Sincronizar comandos quando a conexÃ£o for restabelecida
                // FIXME: BroadcastReceiver cannot be a ViewModelStoreOwner.
                //  viewModels() can only be called from an Activity or Fragment.
                //  Consider alternative ways to access the ViewModel or perform the synchronization.
                //  For now, creating the ViewModel directly, but this is not the recommended approach.
                val factory = ComandosSociaisViewModelFactory(context)
                val viewModel = factory.create(ComandosSociaisViewModel::class.java)
                viewModel.carregarComandos()
            }
        }
    }
}