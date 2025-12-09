package com.starkaid.starkaidapp.adapters

import android.annotation.SuppressLint
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.models.DisparoResponse

class HistoricoDisparoAdapter(
    private val disparos: List<DisparoResponse>,
    private val onItemClick: (DisparoResponse) -> Unit
) : RecyclerView.Adapter<HistoricoDisparoAdapter.ViewHolder>() {

    class ViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        val txtNome: TextView? = itemView.findViewById(R.id.textViewNomeDispositivo)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_disparo, parent, false)
        return ViewHolder(view)
    }

    override fun getItemCount() = disparos.size

    @SuppressLint("SetTextI18n")
    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val disparo = disparos[position]
        holder.txtNome?.text = "📡 ${disparo.dispositivoNome}"
        holder.itemView.setOnClickListener { onItemClick(disparo) }
    }
}