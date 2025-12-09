package com.starkaid.starkaidapp.ewelink.adapter


import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.SeekBar
import androidx.recyclerview.widget.RecyclerView
import com.starkaid.starkaidapp.R
import com.starkaid.starkaidapp.ewelink.models.EwelinkDevice
import com.google.android.material.switchmaterial.SwitchMaterial
import android.widget.TextView
import android.widget.ImageView
import androidx.core.content.ContextCompat
import org.json.JSONObject

class DeviceEwelinkAdapter(
    private var devices: List<EwelinkDevice>,
    private val onDeviceToggle: (EwelinkDevice, Boolean) -> Unit,
    private val onBrightnessChange: (EwelinkDevice, Int) -> Unit
) : RecyclerView.Adapter<DeviceEwelinkAdapter.DeviceViewHolder>() {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): DeviceViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_device_ewelink, parent, false)
        return DeviceViewHolder(view)
    }

    override fun onBindViewHolder(holder: DeviceViewHolder, position: Int) {
        val device = devices[position]
        holder.bind(device)
    }

    override fun getItemCount(): Int = devices.size

    fun updateDevices(newDevices: List<EwelinkDevice>) {
        devices = newDevices
        notifyDataSetChanged()
    }

    inner class DeviceViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        private val txtDeviceName: TextView = itemView.findViewById(R.id.txtDeviceName)
        private val txtDeviceStatus: TextView = itemView.findViewById(R.id.txtDeviceStatus)
        private val txtDeviceType: TextView = itemView.findViewById(R.id.txtDeviceType)
        private val txtDeviceId: TextView = itemView.findViewById(R.id.txtDeviceId)
        private val imgDeviceIcon: ImageView = itemView.findViewById(R.id.imgDeviceIcon)
        private val switchPower: SwitchMaterial = itemView.findViewById(R.id.switchPower)
        private val layoutControls: View = itemView.findViewById(R.id.layoutControls)
        private val seekBrightness: SeekBar = itemView.findViewById(R.id.seekBrightness)

        // No método bind do DeviceEwelinkAdapter:
        fun bind(device: EwelinkDevice) {
            txtDeviceName.text = device.name
            txtDeviceId.text = "ID: ${device.id.take(8)}..."
            txtDeviceType.text = getDeviceTypeText(device.uiid)

            // Status do dispositivo - VERSÃO MELHORADA
            if (device.online) {
                val isOn = device.params.optString("switch", "off") == "on"
                txtDeviceStatus.text = if (isOn) "✅ Online - Ligado" else "✅ Online - Desligado"
                txtDeviceStatus.setTextColor(ContextCompat.getColor(itemView.context, R.color.online_green))
                switchPower.isEnabled = true
            } else {
                txtDeviceStatus.text = "❌ Offline"
                txtDeviceStatus.setTextColor(ContextCompat.getColor(itemView.context, R.color.offline_red))
                switchPower.isEnabled = false
                switchPower.isChecked = false
            }

            // Ícone do dispositivo
            imgDeviceIcon.setImageResource(getDeviceIcon(device.uiid))

            // Estado do switch
            val isOn = device.params.optString("switch", "off") == "on"
            
            // Remover listener temporariamente para evitar loop ao atualizar programaticamente
            switchPower.setOnCheckedChangeListener(null)
            switchPower.isChecked = isOn

            // Configurar controles baseados no tipo de dispositivo
            setupDeviceControls(device)

            // Re-adicionar listener após atualizar o estado
            switchPower.setOnCheckedChangeListener { _, isChecked ->
                // Verificar se o estado realmente mudou para evitar chamadas desnecessárias
                val currentState = device.params.optString("switch", "off") == "on"
                if (isChecked != currentState) {
                    onDeviceToggle(device, isChecked)
                }
            }

            seekBrightness.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
                override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {
                    if (fromUser) {
                        onBrightnessChange(device, progress)
                    }
                }

                override fun onStartTrackingTouch(seekBar: SeekBar?) {}
                override fun onStopTrackingTouch(seekBar: SeekBar?) {}
            })
        }

        private fun setupDeviceControls(device: EwelinkDevice) {
            try {
                when (device.uiid) {
                    1, 6, 14 -> { // Interruptores simples
                        layoutControls.visibility = View.GONE
                    }
                    44, 32, 36 -> { // Lâmpadas com dimmer
                        layoutControls.visibility = View.VISIBLE
                        val brightness = device.params.optInt("brightness", 50)
                        seekBrightness.progress = brightness
                    }
                    else -> {
                        // Para dispositivos desconhecidos, verificar se têm controle de brilho
                        if (device.params.has("brightness")) {
                            layoutControls.visibility = View.VISIBLE
                            val brightness = device.params.optInt("brightness", 50)
                            seekBrightness.progress = brightness
                        } else {
                            layoutControls.visibility = View.GONE
                        }
                    }
                }
            } catch (e: Exception) {
                Log.e("EWE", "Erro ao configurar controles: ${e.message}")
                layoutControls.visibility = View.GONE
            }
        }

        private fun getDeviceTypeText(uiid: Int): String {
            return when (uiid) {
                1 -> "Interruptor Simples"
                6 -> "Tomada Inteligente"
                14 -> "Interruptor Duplo"
                32 -> "Dimmer"
                36 -> "Dimmer RGB"
                44 -> "Lâmpada RGB"
                else -> "Dispositivo ($uiid)"
            }
        }

        private fun getDeviceIcon(uiid: Int): Int {
            return when (uiid) {
                1, 14 -> R.drawable.ic_switch
                6 -> R.drawable.ic_power_outlet
                32, 36, 44 -> R.drawable.ic_light_on
                else -> R.drawable.ic_iot
            }
        }
    }
}