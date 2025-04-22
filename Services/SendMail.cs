using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Services
{
    public class SendMail
    {
        public static void SendMessage(string Message, string To)
        {
            var smtpClient = new SmtpClient("smtp.yandex.ru")
            {
                Port = 587,
                Credentials = new NetworkCredential("alenafilimonovafilimonowa@yandex.ru", "uphnsxzyigrfgsrx"),
                EnableSsl = true
            };
            smtpClient.Send("Plagiarism Gurd", To, "Plagiarism", Message);
        }
    }
}
