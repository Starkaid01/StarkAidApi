package com.starkaid.starkaidapp.adapters

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.databinding.ItemRotinaBinding
import com.starkaid.starkaidapp.services.RotinaDto

class RotinasAdapter(
    private val items: List<RotinaDto>,
    private val onToggle: (String, Boolean) -> Unit,
    private val onExecute: (String) -> Unit,
    private val onDelete: (String) -> Unit,
    private val onItemClick: (RotinaDto) -> Unit
) : RecyclerView.Adapter<RotinasAdapter.ViewHolder>() {

    class ViewHolder(val binding: ItemRotinaBinding) : RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val binding = ItemRotinaBinding.inflate(LayoutInflater.from(parent.context), parent, false)
        return ViewHolder(binding)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = items[position]
        holder.binding.tvNome.text = item.nome
        holder.binding.tvDescricao.text = item.descricao ?: ""
        holder.binding.switchAtiva.isChecked = item.ativa
        
        val statusRes = if (item.ativa) R.drawable.circle_green else R.drawable.circle_red
        holder.binding.viewStatusIndicator.setBackgroundResource(statusRes)

        holder.binding.switchAtiva.setOnCheckedChangeListener { _, isChecked ->
            onToggle(item.id, isChecked)
        }

        holder.binding.btnExecutar.setOnClickListener {
            onExecute(item.id)
        }

        holder.binding.root.setOnClickListener {
            onItemClick(item)
        }

        holder.binding.btnRemove.setOnClickListener {
            onDelete(item.id)
        }
    }

    override fun getItemCount() = items.size
}
