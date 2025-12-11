package com.starkaid.starkaidapp.services

import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.*

interface AgendamentosApi {
    @GET("api/v1/Agendamentos")
    suspend fun listarAgendamentos(): Response<List<AgendamentoResponse>>

    @POST("api/v1/Agendamentos")
    suspend fun criarAgendamento(
        @Body request: CriarAgendamentoRequest
    ): Response<AgendamentoResponse>

    @PUT("api/v1/Agendamentos/{id}")
    suspend fun atualizarAgendamento(
        @Path("id") id: String,
        @Body request: AtualizarAgendamentoRequest
    ): Response<String>

    @DELETE("api/v1/Agendamentos/{id}")
    suspend fun deletarAgendamento(
        @Path("id") id: String
    ): Response<ResponseBody>

    // Novos endpoints para tipos específicos de agendamento
    @POST("api/v1/Agendamentos/esp")
    suspend fun criarAgendamentoEsp(
        @Body request: CriarAgendamentoEspRequest
    ): Response<AgendamentoResponse>

    @POST("api/v1/Agendamentos/starkswitch")
    suspend fun criarAgendamentoStarkswitch(
        @Body request: CriarAgendamentoStarkswitchRequest
    ): Response<AgendamentoResponse>

    @POST("api/v1/Agendamentos/ewelink")
    suspend fun criarAgendamentoEwelink(
        @Body request: CriarAgendamentoEwelinkRequest
    ): Response<AgendamentoResponse>
}

data class AgendamentoResponse(
    val id: String,
    val userId: String,
    val deviceId: String?,
    val dispositivoEspId: String?,
    val ewelinkDeviceId: String?,
    val tipoAgendamento: Any?, // Pode ser Int ou String - 1/"starkswitch" = Starkswitch, 2/"esp" = ESP, 3/"ewelink" = Ewelink
    val agendadoPara: String,
    val comando: String,
    val executado: Boolean,
    val recorrencia: String?,
    val user: Any?,
    val device: Any?,
    val dispositivoEsp: Any?
) {
    // Função auxiliar para obter tipoAgendamento como Int
    fun getTipoAgendamentoInt(): Int {
        return when (tipoAgendamento) {
            is Int -> tipoAgendamento
            is String -> {
                when (tipoAgendamento.lowercase()) {
                    "starkswitch" -> 1
                    "esp" -> 2
                    "ewelink" -> 3
                    else -> tipoAgendamento.toIntOrNull() ?: 0
                }
            }
            is Number -> tipoAgendamento.toInt()
            else -> 0
        }
    }
}

data class CriarAgendamentoRequest(
    val deviceId: String,
    val agendadoPara: String,
    val comando: String,
    val recorrencia: String
)

data class AtualizarAgendamentoRequest(
    val agendadoPara: String?,
    val comando: String?,
    val recorrencia: String?
)

data class CriarAgendamentoEspRequest(
    val dispositivoEspId: String,
    val data: String, // formato: "2024-01-15"
    val hora: Int,
    val minuto: Int,
    val recorrencia: String // "NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno"
)

data class CriarAgendamentoStarkswitchRequest(
    val deviceId: String,
    val acao: String, // "ligar" ou "desligar"
    val data: String, // formato: "2024-01-15"
    val hora: Int,
    val minuto: Int,
    val recorrencia: String // "NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno"
)

data class CriarAgendamentoEwelinkRequest(
    val ewelinkDeviceId: String,
    val acao: String, // "ligar" ou "desligar"
    val data: String, // formato: "2024-01-15"
    val hora: Int,
    val minuto: Int,
    val recorrencia: String // "NaoRepetir", "TodosOsDias", "TodaSemana", "TodoMes", "TodoAno"
)

