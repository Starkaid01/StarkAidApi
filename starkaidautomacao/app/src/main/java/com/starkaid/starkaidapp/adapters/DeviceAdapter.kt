package com.starkaid.starkaidapp.adapters

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.models.Device

class DeviceAdapter(
    private val devices: List<Device>,
    private val listener: OnDeviceClickListener
) : RecyclerView.Adapter<DeviceAdapter.DeviceViewHolder>() {

    interface OnDeviceClickListener {
        fun onDeviceClick(device: Device)
    }

    inner class DeviceViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        val ivDeviceIcon: ImageView = itemView.findViewById(R.id.ivDeviceIcon)
        val tvDeviceName: TextView = itemView.findViewById(R.id.tvDeviceName)

        init {
            itemView.setOnClickListener {
                listener.onDeviceClick(devices[adapterPosition])
            }
        }
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): DeviceViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_device_main, parent, false)
        return DeviceViewHolder(view)
    }

    override fun onBindViewHolder(holder: DeviceViewHolder, position: Int) {
        val device = devices[position]

        // Atualiza o nome do dispositivo
        holder.tvDeviceName.text = device.name

        // Atualiza o ícone baseado no estado (ligado/desligado)
        val iconRes = if (device.isOn) {
            R.drawable.ic_light_on
        } else {
            R.drawable.ic_light_off
        }
        holder.ivDeviceIcon.setImageResource(iconRes)
    }

    override fun getItemCount() = devices.size
}