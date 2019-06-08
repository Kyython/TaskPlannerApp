using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace TaskPlanner.Services
{
    public class AppService
    {
        public async Task<string> SendEmailAsync(string fromAddress, string fromAddressPassword, string toAddress, string theme, string text)
        {
            const int PORT = 587;

            SmtpClient client = new SmtpClient("smtp.gmail.com", PORT)
            {
                Credentials = new NetworkCredential(fromAddress, fromAddressPassword),
                EnableSsl = true
            };

            MailMessage message = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = theme,
                Body = text
            };

            try
            {
                message.To.Add(new MailAddress(toAddress));
                await client.SendMailAsync(message);
            }
            catch (FormatException)
            {
                return "Адрес электронной почты получателя некорректен!";
            }
            catch (Exception)
            {
                return "Ошибка отправки сообщения";
            }

            return "Сообщение успешно отправлено!";
        }

        public Task<string> MoveCatalog(string fromAddress, string toAddress)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(fromAddress);

            if (directoryInfo.Exists)
            {
                try
                {
                    directoryInfo.MoveTo(toAddress + @"\" + directoryInfo.Name);
                }
                catch(Exception exception)
                {
                    return Task.FromResult("Ошибка перемещения директории" + exception.Message);
                }
            }
            else
            {
                return Task.FromResult("Директория не найдена!");
            }

            return Task.FromResult("Директория успешно перемещена");
        }

        public Task<string> DownloadFile(string fromAddress, string toAddress)
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile(fromAddress, toAddress);
                }
                catch(Exception)
                {
                    return Task.FromResult("Ошибка загрузки файла");
                }
            }

            return Task.FromResult("Файл успешно загружен");
        }
    }
}
