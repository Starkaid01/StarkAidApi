namespace StarkAid.WindowsForms.Models;

public class EwelinkDevice
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public int Uiid { get; set; }
    public dynamic? Params { get; set; }
    public bool Online { get; set; }
    public string? FamilyId { get; set; }
    public string? RoomId { get; set; }
    public bool IsOn { get; set; }
}
