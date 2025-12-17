namespace LocalLinker.Models
{
    public interface IDataLog
    {
        public void Log(string methodname,string message);
        //void ShowNotification(string message, string type = "success");
        public  Task SendEmail(string toEmail, string subject, string body);

    }
}
