namespace AutoPro.Application.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>When false, no outbound email is attempted.</summary>
    public bool Enabled { get; set; }

    /// <summary><c>Log</c> writes the message to app logs; <c>Smtp</c> sends via SMTP.</summary>
    public string Provider { get; set; } = "Log";

    public string FromAddress { get; set; } = "noreply@autopro.local";
    public string FromName { get; set; } = "AutoPro";

    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }

    /// <summary>When true (default), uses STARTTLS on connect (typical for port 587).</summary>
    public bool UseStartTls { get; set; } = true;
}
