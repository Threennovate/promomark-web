namespace PromomaxWeb.Services.Email;

public class SmtpOptions
{
    public string? From { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 25;
    public string? Username { get; set; }
    public string? Password { get; set; }
    // Expected values: None | StartTls | SslOnConnect
    public string SecureSocketOptions { get; set; } = "None";
}
