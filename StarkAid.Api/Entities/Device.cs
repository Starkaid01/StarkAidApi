namespace StarkAid.Api.Entities
{
    public class Device
    {
        public Guid Id { get; set; }               // Identificador único
        public string Name { get; set; }           // Nome do dispositivo (ex: Luz Sala)
        public string ApiKey { get; set; }         // 🔐 API-Key única desse dispositivo
        public Guid UserId { get; set; }           // Relacionamento com o usuário dono do device
        public User User { get; set; }             // Navegação para entidade User

        // Adicione esta propriedade para armazenar o tópico MQTT do dispositivo
        public string MqttTopic { get; set; } = string.Empty;
    }
}