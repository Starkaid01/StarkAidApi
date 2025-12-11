namespace StarkAid.Api.DTOs.V1.License;

public class CreateLicenseForUserRequest
{
    public Guid UserId { get; set; }
    public int MaxMachines { get; set; }
    public decimal? Price { get; set; } // Opcional, se não fornecido usa o preço padrão
}

