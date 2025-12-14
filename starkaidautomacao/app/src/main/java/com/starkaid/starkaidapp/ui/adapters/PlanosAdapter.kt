package com.starkaid.starkaidapp.ui.adapters

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.services.PlanoAtivoResponse
import java.text.SimpleDateFormat
import java.util.*

class PlanosAdapter(
    private var planos: List<PlanoAtivoResponse>,
    private val onCancelarClick: (PlanoAtivoResponse) -> Unit
) : RecyclerView.Adapter<PlanosAdapter.PlanoViewHolder>() {

    class PlanoViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        val nomePlano: TextView = itemView.findViewById(R.id.tvNomePlano)
        val status: TextView = itemView.findViewById(R.id.tvStatus)
        val nivel: TextView = itemView.findViewById(R.id.tvNivel)
        val valor: TextView = itemView.findViewById(R.id.tvValor)
        val iniciadaEm: TextView = itemView.findViewById(R.id.tvIniciadaEm)
        val expiraEm: TextView = itemView.findViewById(R.id.tvExpiraEm)
        val dataCriacao: TextView = itemView.findViewById(R.id.tvDataCriacao)
        val stripeId: TextView = itemView.findViewById(R.id.tvStripeId)
        val btnCancelar: Button = itemView.findViewById(R.id.btnCancelar)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): PlanoViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_plano_ativo, parent, false)
        return PlanoViewHolder(view)
    }

    override fun onBindViewHolder(holder: PlanoViewHolder, position: Int) {
        val plano = planos[position]

        val isPremium = plano.nivel == 2
        holder.nomePlano.text = if (isPremium) "StarkAid Premium" else plano.nomePlano
        holder.status.text = plano.status
        holder.nivel.text = if (isPremium) "Premium" else "Nível ${plano.nivel}"
        holder.valor.text = if (isPremium) "R$ 10,00/mês" else "R$ ${String.format("%.2f", plano.valor)}/mês"

        holder.iniciadaEm.text = "Iniciado em: ${formatDate(plano.iniciadaEm)}"
        holder.expiraEm.text = "Expira em: ${formatDate(plano.expiraEm) ?: "Sem expiração"}"
        holder.dataCriacao.text = "Criado em: ${formatDate(plano.dataCriacao)}"

        if (plano.stripeSubscriptionId != null) {
            holder.stripeId.text = "ID Stripe: ${plano.stripeSubscriptionId}"
            holder.stripeId.visibility = View.VISIBLE
        } else {
            holder.stripeId.visibility = View.GONE
        }

        // Determinar cor do badge baseado no nível
        val badgeColor = when (plano.nivel) {
            2 -> android.graphics.Color.parseColor("#10b981") // Premium
            else -> android.graphics.Color.parseColor("#6b7280")
        }
        holder.status.setBackgroundColor(badgeColor)
        holder.status.setTextColor(android.graphics.Color.WHITE)
        holder.status.setPadding(16, 8, 16, 8)

        holder.btnCancelar.setOnClickListener {
            onCancelarClick(plano)
        }

        // Habilitar botão apenas se o status for "ativa" ou "Ativa"
        holder.btnCancelar.isEnabled = plano.status.equals("ativa", ignoreCase = true) || 
                                      plano.status.equals("Ativa", ignoreCase = true)
    }

    override fun getItemCount(): Int = planos.size

    fun updatePlanos(newPlanos: List<PlanoAtivoResponse>) {
        // Somente exibe o plano Premium (nivel 2)
        planos = newPlanos.filter { it.nivel == 2 }
        notifyDataSetChanged()
    }

    private fun formatDate(dateString: String?): String? {
        if (dateString.isNullOrEmpty()) return null
        return try {
            val inputFormat = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault())
            val outputFormat = SimpleDateFormat("dd/MM/yyyy", Locale.getDefault())
            val date = inputFormat.parse(dateString)
            date?.let { outputFormat.format(it) }
        } catch (e: Exception) {
            dateString
        }
    }
}
