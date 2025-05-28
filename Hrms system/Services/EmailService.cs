using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;
using System.Threading.Tasks;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string message);
    Task SendTemplateEmailAsync(string toEmail, object dynamicData);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;


    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var senderEmail = _configuration["SendGrid:SenderEmail"];
        var senderName = _configuration["SendGrid:SenderName"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(senderEmail))
            throw new Exception("SendGrid configuration is missing.");

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(senderEmail, senderName ?? "HR Team");
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);
        var response = await client.SendEmailAsync(msg);

        var responseBody = await response.Body.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"SendGrid email failed with status: {response.StatusCode}. Body: {responseBody}");
        }
    }

    public async Task SendTemplateEmailAsync(string toEmail, object dynamicData)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var senderEmail = _configuration["SendGrid:SenderEmail"];
        var senderName = _configuration["SendGrid:SenderName"];
        var templateId = _configuration["SendGrid:TemplateId"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(templateId))
            throw new Exception("SendGrid configuration is missing.");

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(senderEmail, senderName ?? "HR Team");
        var to = new EmailAddress(toEmail);

        var msg = new SendGridMessage();
        msg.SetFrom(from);
        msg.AddTo(to);
        msg.SetTemplateId(templateId);
        msg.SetTemplateData(dynamicData);

        var response = await client.SendEmailAsync(msg);
        var responseBody = await response.Body.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"SendGrid template email failed with status: {response.StatusCode}. Body: {responseBody}");
        }
    }

}
