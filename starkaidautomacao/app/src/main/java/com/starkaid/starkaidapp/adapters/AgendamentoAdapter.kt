package com.starkaid.starkaidapp.adapters

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.services.AgendamentoResponse
import java.text.SimpleDateFormat
import java.util.*

class AgendamentoAdapter(
    private val agendamentos: List<AgendamentoResponse>,
    private val onDeleteClick: (String) -> Unit
) : RecyclerView.Adapter<AgendamentoAdapter.AgendamentoViewHolder>() {

    inner class AgendamentoViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        val tvTitulo: TextView = itemView.findViewById(R.id.tvAgendamentoTitulo)
        val tvTipo: TextView = itemView.findViewById(R.id.tvAgendamentoTipo)
        val tvDataHora: TextView = itemView.findViewById(R.id.tvAgendamentoDataHora)
        val tvComando: TextView = itemView.findViewById(R.id.tvAgendamentoComando)
        val tvRecorrencia: TextView = itemView.findViewById(R.id.tvAgendamentoRecorrencia)
        val tvStatus: TextView = itemView.findViewById(R.id.tvAgendamentoStatus)
        val btnExcluir: Button = itemView.findViewById(R.id.btnExcluirAgendamento)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): AgendamentoViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_agendamento, parent, false)
        return AgendamentoViewHolder(view)
    }

    override fun onBindViewHolder(holder: AgendamentoViewHolder, position: Int) {
        val agendamento = agendamentos[position]

        // Determinar tipo de agendamento
        val tipoNum = agendamento.getTipoAgendamentoInt()
        val tipo = when (tipoNum) {
            1 -> "Starkswitch"
            2 -> "ESP"
            3 -> "Ewelink"
            else -> "Desconhecido"
        }

        holder.tvTitulo.text = "Agendamento $tipo"
        holder.tvTipo.text = "Tipo: $tipo"

        // Formatar data/hora
        try {
            // Tentar diferentes formatos de data
            val formats = listOf(
                "yyyy-MM-dd'T'HH:mm:ss",
                "yyyy-MM-dd'T'HH:mm:ss.SSS",
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                "yyyy-MM-dd HH:mm:ss"
            )
            
            var date: Date? = null
            for (format in formats) {
                try {
                    val dateFormat = SimpleDateFormat(format, Locale.getDefault())
                    date = dateFormat.parse(agendamento.agendadoPara)
                    if (date != null) break
                } catch (e: Exception) {
                    // Continuar tentando outros formatos
                }
            }
            
            if (date != null) {
                val dataFormatada = SimpleDateFormat("dd/MM/yyyy", Locale.getDefault()).format(date)
                val horaFormatada = SimpleDateFormat("HH:mm", Locale.getDefault()).format(date)
                holder.tvDataHora.text = "Data/Hora: $dataFormatada às $horaFormatada"
            } else {
                holder.tvDataHora.text = "Data/Hora: ${agendamento.agendadoPara}"
            }
        } catch (e: Exception) {
            holder.tvDataHora.text = agendamento.agendadoPara
        }

        holder.tvComando.text = "Comando: ${agendamento.comando ?: "N/A"}"
        holder.tvRecorrencia.text = "Recorrência: ${agendamento.recorrencia ?: "NaoRepetir"}"

        // Status
        val status = if (agendamento.executado) "Executado" else "Pendente"
        holder.tvStatus.text = "Status: $status"
        holder.tvStatus.setTextColor(
            if (agendamento.executado) {
                holder.itemView.context.getColor(android.R.color.holo_green_dark)
            } else {
                holder.itemView.context.getColor(android.R.color.holo_orange_dark)
            }
        )

        // Botão excluir
        holder.btnExcluir.setOnClickListener {
            onDeleteClick(agendamento.id)
        }
    }

    override fun getItemCount() = agendamentos.size
}

