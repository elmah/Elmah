#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Mail;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Mannex.Threading.Tasks;

    #endregion

    public static class ErrorMail
    {
        public class Settings
        {
            public string MailRecipient { get; set; }
            public string MailSender { get; set; }
            public string MailCopyRecipient { get; set; }
            public string MailSubjectFormat { get; set; }
            public MailPriority MailPriority { get; set; }
            public string SmtpServer { get; set; }
            public int SmtpPort { get; set; }
            public string AuthUserName { get; set; }
            public string AuthPassword { get; set; }
            public bool DontSendYsod { get; set; }
            public bool UseSsl { get; set; }
            public Func<Error, MailMessage, CancellationToken, Task> OnMailing { get; set; }
            public Func<Error, MailMessage, CancellationToken, Task> OnMailed { get; set; }
            /* TODO */ public Func<Error, MailMessage, CancellationToken, Task> OnDisposingMail { get; set; }
        }

        public static Func<Error, CancellationToken, Task> CreateMailer(Settings options)
        {
            return (error, cancellationToken) => Send(error, options, cancellationToken);
        }

        public static Task Send(Error error, Settings options)
        {
            return Send(error, options, CancellationToken.None);
        }

        public static Task Send(Error error, Settings options, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(SendImpl(error, options, cancellationToken), cancellationToken);
        }

        static IEnumerable<Task> SendImpl(Error error, Settings options, CancellationToken cancellationToken)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            //
            // Start by checking if we have a sender and a recipient.
            // These values may be null if someone overrides the
            // implementation of OnInit but does not override the
            // MailSender and MailRecipient properties.
            //

            var sender = options.MailSender ?? string.Empty;
            var recipient = options.MailRecipient ?? string.Empty;
            var copyRecipient = options.MailCopyRecipient ?? string.Empty;

            if (recipient.Length == 0)
                yield break;

            //
            // Create the mail, setting up the sender and recipient and priority.
            //

            var mail = new MailMessage
            {
                Priority = options.MailPriority, 
                From = new MailAddress(sender)
            };

            mail.To.Add(recipient);

            if (copyRecipient.Length > 0)
                mail.CC.Add(copyRecipient);

            //
            // Format the mail subject.
            // 

            var subjectFormat = Mask.EmptyString(options.MailSubjectFormat, "Error ({1}): {0}");
            mail.Subject = string.Format(subjectFormat, error.Message, error.Type)
                                 .Replace('\r', ' ').Replace('\n', ' ');

            //
            // Format the mail body.
            //

            var formatter = new ErrorMailHtmlFormatter();

            var bodyWriter = new StringWriter();
            formatter.Format(bodyWriter, error);
            mail.Body = bodyWriter.ToString();

            var mimeType = formatter.MimeType;
            switch (mimeType)
            {
                case "text/html": mail.IsBodyHtml = true; break;
                case "text/plain": mail.IsBodyHtml = false; break;

                default:
                {
                    throw new ApplicationException(string.Format(
                        "The error mail module does not know how to handle the {1} media type that is created by the {0} formatter.",
                        formatter.GetType().FullName, mimeType));
                }
            }

            try
            {
                //
                // If an HTML message was supplied by the web host then attach 
                // it to the mail if not explicitly told not to do so.
                //

                if (!options.DontSendYsod && error.WebHostHtmlMessage.Length > 0)
                {
                    var ysodAttachment = Attachment.CreateAttachmentFromString(error.WebHostHtmlMessage, "YSOD.html", Encoding.UTF8, "text/html");
                    mail.Attachments.Add(ysodAttachment);
                }

                //
                // Send off the mail with some chance to pre- or post-process
                // using event.
                //

                if (options.OnMailing != null)
                {
                    Task task;
                    yield return (task = options.OnMailing(error, mail, cancellationToken));
                    /* TODO error and cancellation handling */ Debug.Assert(!task.IsCanceled && !task.IsFaulted);
                }

                yield return SendMail(mail, options);

                if (options.OnMailed != null)
                {
                    Task task;
                    yield return (task = options.OnMailed(error, mail, cancellationToken));
                    /* TODO error and cancellation handling */ Debug.Assert(!task.IsCanceled && !task.IsFaulted);
                }
            }
            finally
            {
                //TODO yield return OnDisposingMail(error, options, mail);
                mail.Dispose();
            }

            Task disposingMailTask;
            yield return (disposingMailTask = options.OnDisposingMail(error, mail, cancellationToken));
            /* TODO error and cancellation handling */ Debug.Assert(!disposingMailTask.IsCanceled && !disposingMailTask.IsFaulted);
        }

        static Task SendMail(MailMessage mail, Settings options)
        {
            return SendMail(mail, options, CancellationToken.None);
        }

        static Task SendMail(MailMessage mail, Settings options, CancellationToken cancellationToken)
        {
            if (mail == null) throw new ArgumentNullException("mail");
            if (options == null) throw new ArgumentNullException("options");

            //
            // Under .NET Framework 2.0, the authentication settings
            // go on the SmtpClient object rather than mail message
            // so these have to be set up here.
            //

            var client = new SmtpClient { EnableSsl = options.UseSsl };

            var host = options.SmtpServer ?? string.Empty;

            if (host.Length > 0)
            {
                client.Host = host;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
            }

            var port = options.SmtpPort;
            if (port > 0)
                client.Port = port;

            var userName = options.AuthUserName ?? string.Empty;
            var password = options.AuthPassword ?? string.Empty;

            if (userName.Length > 0 && password.Length > 0)
                client.Credentials = new NetworkCredential(userName, password);

            if (cancellationToken.IsCancellationRequested)
                return CompletedTask.Cancelled();

            // TODO Consider making this testable
            // May be separate initialization of SmtpClient from actual act of sending?

            var tcs = new TaskCompletionSource<object>();
            client.SendCompleted += (sender, args) =>
            {
                if (args.Cancelled || cancellationToken.IsCancellationRequested)
                    tcs.TrySetCanceled();
                else if (args.Error != null)
                    tcs.TrySetException(args.Error);
                else
                    tcs.TrySetResult(null);
            };
            cancellationToken.Register(client.SendAsyncCancel);
            client.SendAsync(mail, null);
            return tcs.Task;
        }
    }
}
