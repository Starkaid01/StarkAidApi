package com.starkaid.starkaidapp.adapters

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.models.DispositivoDisparoResponse

class DispositivoAdapter(
    private val dispositivos: List<DispositivoDisparoResponse>,
    private val onItemClick: (DispositivoDisparoResponse) -> Unit
) : RecyclerView.Adapter<DispositivoAdapter.ViewHolder>() {

    inner class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val textNome: TextView = view.findViewById(R.id.textNome)
        // --Commented out by Inspection (20/08/2025 14:11):val iconDevice: ImageView = view.findViewById(R.id.iconDevice)
        // Remova esta linha - está causando conflito
        // val cardView: View = view.findViewById(R.id.cardView)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_dispositivo, parent, false)
        return ViewHolder(view)
    }

    override fun getItemCount(): Int = dispositivos.size

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val dispositivo = dispositivos[position]
        val nomeLimpo = dispositivo.nome.substringBeforeLast("-id")
        holder.textNome.text = nomeLimpo

        // Configurar clique em todo o item
        holder.itemView.setOnClickListener {
            onItemClick(dispositivo)
        }

        // Se precisar do clique apenas no card, use:
        // holder.itemView.findViewById<MaterialCardView>(R.id.cardItem).setOnClickListener {
        //     onItemClick(dispositivo)
        // }
    }
}