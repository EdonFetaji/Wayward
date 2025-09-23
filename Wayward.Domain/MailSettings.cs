namespace Wayward.Domain
{
    public class MailSettings
    {
        public string? SmtpServer { get; set; }
        public int SmtpPort { get; set; }          
        public bool UseSsl { get; set; }            
        public string? SmtpUserName { get; set; }  
        public string? SmtpPassword { get; set; }   
        public string? FromAddress { get; set; }   
        public string? FromDisplayName { get; set; } = "Wayward & Co";
    }
}
