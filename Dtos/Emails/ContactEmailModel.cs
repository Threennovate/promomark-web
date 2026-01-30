namespace PromomaxWeb.Dtos.Emails;

public class ContactEmailModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? PageName { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}
