namespace StarkAid.Api.Features.TuyaAdmin.Models
{
    public record TuyaUserDto(
        string Uid,
        string Username,
        string CountryCode,
        string? CreateTime
    );

    public record TuyaOperationResultDto(
        bool Success,
        string Message
    );

    public record CleanDuplicatesRequestDto(
        string[] Emails
    );
}
