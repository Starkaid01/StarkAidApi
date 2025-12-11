using Newtonsoft.Json;

namespace StarkAid.Api.DTOs.V1.Ewelink
{
    public class EwelinkTokens
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public long AccessTokenExpiry { get; set; }
        public long RefreshTokenExpiry { get; set; }
        public string Region { get; set; }
    }

    public class EwelinkDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int Uiid { get; set; }
        public dynamic Params { get; set; }
        public bool Online { get; set; }
        public string FamilyId { get; set; }
        public string RoomId { get; set; }
    }

    public class EwelinkFamily
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<EwelinkRoom> Rooms { get; set; }
    }

    public class EwelinkRoom
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    // 🔥 CORREÇÃO: Classes específicas para requests
    public class EwelinkDeviceControlRequest
    {
        public string DeviceId { get; set; }
        public dynamic Parameters { get; set; }
    }

    public class EwelinkRefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }

    // 🔥 NOVA CLASSE: Para requisição de controle para API eWeLink
    public class EwelinkApiControlRequest
    {
        [JsonProperty("type")]
        public int Type { get; set; } = 1;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("params")]
        public dynamic Params { get; set; }
    }

    // DTOs para respostas da API
    public class EwelinkLoginRequest
    {
        public string Code { get; set; }
        public string Region { get; set; } // Região retornada pelo callback (as, cn, us, eu)
    }

    public class EwelinkDirectLoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string AreaCode { get; set; } = "+55"; // Código do país padrão (Brasil)
    }

    public class EwelinkDeviceResponse
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int Uiid { get; set; }
        public dynamic Params { get; set; }
        public bool Online { get; set; }
        public string? FamilyId { get; set; }
        public string? RoomId { get; set; }
        public bool IsOn { get; set; }
    }

    public class EwelinkControlDeviceRequest
    {
        public bool Switch { get; set; }
    }
}