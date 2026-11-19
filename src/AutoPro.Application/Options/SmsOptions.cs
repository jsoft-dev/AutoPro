namespace AutoPro.Application.Options;

public class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>When false, no outbound SMS is attempted.</summary>
    public bool Enabled { get; set; }

    /// <summary><c>Log</c> writes the message to app logs; <c>SmsGo</c> sends via SMSGo.lk REST API.</summary>
    public string Provider { get; set; } = "Log";

    /// <summary>SMSGo API key (header <c>X-API-Key</c>). Use user secrets in dev — never commit.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Approved sender mask (sender ID). Uses your first approved mask on SMSGo if omitted.</summary>
    public string? Mask { get; set; }

    public string BaseUrl { get; set; } = "https://api.smsgo.lk";

    /// <summary>Optional tag grouped in the SMSGo dashboard. Defaults to AutoPro in the sender.</summary>
    public string CampaignName { get; set; } = "AutoPro";
}