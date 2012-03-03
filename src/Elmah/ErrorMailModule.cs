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

[assembly: Elmah.Scc("$Id: ErrorMailModule.cs 907 2011-12-18 13:03:58Z azizatif $")]

namespace Elmah
{
    #region Imports
    
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Web;
    using System.IO;

#if NET_1_0 || NET_1_1
    using System.Web.Mail;
#else
    using System.Net.Mail;
    using MailAttachment = System.Net.Mail.Attachment;
#endif

    using IDictionary = System.Collections.IDictionary;
    using ThreadPool = System.Threading.ThreadPool;
    using WaitCallback = System.Threading.WaitCallback;
    using Encoding = System.Text.Encoding;
    using NetworkCredential = System.Net.NetworkCredential;

    #endregion

    public sealed class ErrorMailEventArgs : EventArgs
    {
        private readonly Error _error;
        private readonly MailMessage _mail;

        public ErrorMailEventArgs(Error error, MailMessage mail)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            if (mail == null)
                throw new ArgumentNullException("mail");

            _error = error;
            _mail = mail;
        }

        public Error Error
        {
            get { return _error; }
        }

        public MailMessage Mail
        {
            get { return _mail; }
        }
    }

    public delegate void ErrorMailEventHandler(object sender, ErrorMailEventArgs args);

    /// <summary>
    /// HTTP module that sends an e-mail whenever an unhandled exception
    /// occurs in an ASP.NET web application.
    /// </summary>

    public class ErrorMailModule : HttpModuleBase, IExceptionFiltering
    {
        private string _mailSender;
        private string _mailRecipient;
        private string _mailCopyRecipient;
        private string _mailSubjectFormat;
        private MailPriority _mailPriority;
        private bool _reportAsynchronously;
        private string _smtpServer;
        private int _smtpPort;
        private string _authUserName;
        private string _authPassword;
        private bool _noYsod;
#if !NET_1_0 && !NET_1_1
        private bool _useSsl;
#endif

        public event ExceptionFilterEventHandler Filtering;
        public event ErrorMailEventHandler Mailing;
        public event ErrorMailEventHandler Mailed;
        public event ErrorMailEventHandler DisposingMail;

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>
        
        protected override void OnInit(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");
            
            //
            // Get the configuration section of this module.
            // If it's not there then there is nothing to initialize or do.
            // In this case, the module is as good as mute.
            //

            IDictionary config = (IDictionary) GetConfig();

            if (config == null)
                return;

            //
            // Extract the settings.
            //

            string mailRecipient = GetSetting(config, "to");
            string mailSender = GetSetting(config, "from", mailRecipient);
            string mailCopyRecipient = GetSetting(config, "cc", string.Empty);
            string mailSubjectFormat = GetSetting(config, "subject", string.Empty);
            MailPriority mailPriority = (MailPriority) Enum.Parse(typeof(MailPriority), GetSetting(config, "priority", MailPriority.Normal.ToString()), true);
            bool reportAsynchronously = Convert.ToBoolean(GetSetting(config, "async", bool.TrueString));
            string smtpServer = GetSetting(config, "smtpServer", string.Empty);
            int smtpPort = Convert.ToUInt16(GetSetting(config, "smtpPort", "0"), CultureInfo.InvariantCulture);
            string authUserName = GetSetting(config, "userName", string.Empty);
            string authPassword = GetSetting(config, "password", string.Empty);
            bool sendYsod = Convert.ToBoolean(GetSetting(config, "noYsod", bool.FalseString));
#if !NET_1_0 && !NET_1_1
            bool useSsl = Convert.ToBoolean(GetSetting(config, "useSsl", bool.FalseString));
#endif
            //
            // Hook into the Error event of the application.
            //

            application.Error += new EventHandler(OnError);
            ErrorSignal.Get(application).Raised += new ErrorSignalEventHandler(OnErrorSignaled);
            
            //
            // Finally, commit the state of the module if we got this far.
            // Anything beyond this point should not cause an exception.
            //

            _mailRecipient = mailRecipient;
            _mailSender = mailSender;
            _mailCopyRecipient = mailCopyRecipient;
            _mailSubjectFormat = mailSubjectFormat;
            _mailPriority = mailPriority;
            _reportAsynchronously = reportAsynchronously;
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _authUserName = authUserName;
            _authPassword = authPassword;
            _noYsod = sendYsod;
#if !NET_1_0 && !NET_1_1
            _useSsl = useSsl;
#endif
        }
        
        /// <summary>
        /// Determines whether the module will be registered for discovery
        /// in partial trust environments or not.
        /// </summary>
        
        protected override bool SupportDiscoverability
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the e-mail address of the sender.
        /// </summary>
        
        protected virtual string MailSender
        {
            get { return _mailSender; }
        }

        /// <summary>
        /// Gets the e-mail address of the recipient, or a 
        /// comma-/semicolon-delimited list of e-mail addresses in case of 
        /// multiple recipients.
        /// </summary>
        /// <remarks>
        /// When using System.Web.Mail components under .NET Framework 1.x, 
        /// multiple recipients must be semicolon-delimited.
        /// When using System.Net.Mail components under .NET Framework 2.0
        /// or later, multiple recipients must be comma-delimited.
        /// </remarks>

        protected virtual string MailRecipient
        {
            get { return _mailRecipient; }
        }

        /// <summary>
        /// Gets the e-mail address of the recipient for mail carbon 
        /// copy (CC), or a comma-/semicolon-delimited list of e-mail 
        /// addresses in case of multiple recipients.
        /// </summary>
        /// <remarks>
        /// When using System.Web.Mail components under .NET Framework 1.x, 
        /// multiple recipients must be semicolon-delimited.
        /// When using System.Net.Mail components under .NET Framework 2.0
        /// or later, multiple recipients must be comma-delimited.
        /// </remarks>

        protected virtual string MailCopyRecipient
        {
            get { return _mailCopyRecipient; }
        }

        /// <summary>
        /// Gets the text used to format the e-mail subject.
        /// </summary>
        /// <remarks>
        /// The subject text specification may include {0} where the
        /// error message (<see cref="Error.Message"/>) should be inserted 
        /// and {1} <see cref="Error.Type"/> where the error type should 
        /// be insert.
        /// </remarks>

        protected virtual string MailSubjectFormat
        {
            get { return _mailSubjectFormat; }
        }

        /// <summary>
        /// Gets the priority of the e-mail. 
        /// </summary>
        
        protected virtual MailPriority MailPriority
        {
            get { return _mailPriority; }
        }

        /// <summary>
        /// Gets the SMTP server host name used when sending the mail.
        /// </summary>

        protected string SmtpServer
        {
            get { return _smtpServer; }
        }

        /// <summary>
        /// Gets the SMTP port used when sending the mail.
        /// </summary>

        protected int SmtpPort
        {
            get { return _smtpPort; }
        }

        /// <summary>
        /// Gets the user name to use if the SMTP server requires authentication.
        /// </summary>

        protected string AuthUserName
        {
            get { return _authUserName; }
        }

        /// <summary>
        /// Gets the clear-text password to use if the SMTP server requires 
        /// authentication.
        /// </summary>

        protected string AuthPassword
        {
            get { return _authPassword; }
        }

        /// <summary>
        /// Indicates whether <a href="http://en.wikipedia.org/wiki/Screens_of_death#ASP.NET">YSOD</a> 
        /// is attached to the e-mail or not. If <c>true</c>, the YSOD is 
        /// not attached.
        /// </summary>
        
        protected bool NoYsod
        {
            get { return _noYsod; }
        }

#if !NET_1_0 && !NET_1_1
        /// <summary>
        /// Determines if SSL will be used to encrypt communication with the 
        /// mail server.
        /// </summary>

        protected bool UseSsl
        {
            get { return _useSsl; }
        }
#endif

        /// <summary>
        /// The handler called when an unhandled exception bubbles up to 
        /// the module.
        /// </summary>

        protected virtual void OnError(object sender, EventArgs e)
        {
            HttpContext context = ((HttpApplication) sender).Context;
            OnError(context.Server.GetLastError(), context);
        }

        /// <summary>
        /// The handler called when an exception is explicitly signaled.
        /// </summary>

        protected virtual void OnErrorSignaled(object sender, ErrorSignalEventArgs args)
        {
            OnError(args.Exception, args.Context);
        }

        /// <summary>
        /// Reports the exception.
        /// </summary>

        protected virtual void OnError(Exception e, HttpContext context)
        {
            if (e == null) 
                throw new ArgumentNullException("e");

            //
            // Fire an event to check if listeners want to filter out
            // reporting of the uncaught exception.
            //

            ExceptionFilterEventArgs args = new ExceptionFilterEventArgs(e, context);
            OnFiltering(args);

            if (args.Dismissed)
                return;

            //
            // Get the last error and then report it synchronously or 
            // asynchronously based on the configuration.
            //

            Error error = new Error(e, context);

            if (_reportAsynchronously)
                ReportErrorAsync(error);
            else
                ReportError(error);
        }

        /// <summary>
        /// Raises the <see cref="Filtering"/> event.
        /// </summary>

        protected virtual void OnFiltering(ExceptionFilterEventArgs args)
        {
            ExceptionFilterEventHandler handler = Filtering;
            
            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Schedules the error to be e-mailed asynchronously.
        /// </summary>
        /// <remarks>
        /// The default implementation uses the <see cref="ThreadPool"/>
        /// to queue the reporting.
        /// </remarks>

        protected virtual void ReportErrorAsync(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            //
            // Schedule the reporting at a later time using a worker from 
            // the system thread pool. This makes the implementation
            // simpler, but it might have an impact on reducing the
            // number of workers available for processing ASP.NET
            // requests in the case where lots of errors being generated.
            //

            ThreadPool.QueueUserWorkItem(new WaitCallback(ReportError), error);
        }

        private void ReportError(object state)
        {
            try
            {
                ReportError((Error) state);
            }

            //
            // Catch and trace COM/SmtpException here because this
            // method will be called on a thread pool thread and
            // can either fail silently in 1.x or with a big band in
            // 2.0. For latter, see the following MS KB article for
            // details:
            //
            //     Unhandled exceptions cause ASP.NET-based applications 
            //     to unexpectedly quit in the .NET Framework 2.0
            //     http://support.microsoft.com/kb/911816
            //

#if NET_1_0 || NET_1_1
            catch (System.Runtime.InteropServices.COMException e)
            {
                Trace.WriteLine(e);
            }
#else
            catch (SmtpException e)
            {
                Trace.TraceError(e.ToString());
            }
#endif
        }

        /// <summary>
        /// Schedules the error to be e-mailed synchronously.
        /// </summary>

        protected virtual void ReportError(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            //
            // Start by checking if we have a sender and a recipient.
            // These values may be null if someone overrides the
            // implementation of OnInit but does not override the
            // MailSender and MailRecipient properties.
            //

            string sender = Mask.NullString(this.MailSender);
            string recipient = Mask.NullString(this.MailRecipient);
            string copyRecipient = Mask.NullString(this.MailCopyRecipient);

#if NET_1_0 || NET_1_1

            //
            // The sender can be defaulted in the <system.net> settings in 2.0
            //

            if (sender.Length == 0)		
                return;
#endif

            if (recipient.Length == 0)
                return;

            //
            // Create the mail, setting up the sender and recipient and priority.
            //

            MailMessage mail = new MailMessage();
            mail.Priority = this.MailPriority;

#if NET_1_0 || NET_1_1
            mail.From = sender;
            mail.To = recipient;

            if (copyRecipient.Length > 0)
                mail.Cc = copyRecipient;
#else
            mail.From = new MailAddress(sender);
            mail.To.Add(recipient);
            
            if (copyRecipient.Length > 0)
                mail.CC.Add(copyRecipient);
#endif
            //
            // Format the mail subject.
            // 

            string subjectFormat = Mask.EmptyString(this.MailSubjectFormat, "Error ({1}): {0}");
            mail.Subject = string.Format(subjectFormat, error.Message, error.Type).
                Replace('\r', ' ').Replace('\n', ' ');

            //
            // Format the mail body.
            //

            ErrorTextFormatter formatter = CreateErrorFormatter();

            StringWriter bodyWriter = new StringWriter();
            formatter.Format(bodyWriter, error);
            mail.Body = bodyWriter.ToString();

            switch (formatter.MimeType)
            {
#if NET_1_0 || NET_1_1
                case "text/html" : mail.BodyFormat = MailFormat.Html; break;
                case "text/plain" : mail.BodyFormat = MailFormat.Text; break;
#else
                case "text/html": mail.IsBodyHtml = true; break;
                case "text/plain": mail.IsBodyHtml = false; break;
#endif
                default :
                {
                    throw new ApplicationException(string.Format(
                        "The error mail module does not know how to handle the {1} media type that is created by the {0} formatter.",
                        formatter.GetType().FullName, formatter.MimeType));
                }
            }

#if NET_1_1
            //
            // If the mail needs to be delivered to a particular SMTP server
            // then set-up the corresponding CDO configuration fields of the 
            // mail message.
            //
            
            string smtpServer = Mask.NullString(this.SmtpServer);

            if (smtpServer.Length > 0)
            {
                IDictionary fields = mail.Fields;

                fields.Add(CdoConfigurationFields.SendUsing, /* cdoSendUsingPort */ 2);
                fields.Add(CdoConfigurationFields.SmtpServer, smtpServer);
                int smtpPort = this.SmtpPort;
                fields.Add(CdoConfigurationFields.SmtpServerPort, smtpPort <= 0 ? 25 : smtpPort);

                //
                // If the SMTP server requires authentication (indicated by 
                // non-blank user name and password settings) then set-up 
                // the corresponding CDO configuration fields of the mail 
                // message.
                //
            
                string userName = Mask.NullString(this.AuthUserName);
                string password = Mask.NullString(this.AuthPassword);

                if (userName.Length > 0 && password.Length > 0)
                {
                    fields.Add(CdoConfigurationFields.SmtpAuthenticate, 1);
                    fields.Add(CdoConfigurationFields.SendUserName, userName);
                    fields.Add(CdoConfigurationFields.SendPassword, password);
                }
            }
#endif
            MailAttachment ysodAttachment = null;
            ErrorMailEventArgs args = new ErrorMailEventArgs(error, mail);

            try
            {
                //
                // If an HTML message was supplied by the web host then attach 
                // it to the mail if not explicitly told not to do so.
                //

                if (!NoYsod && error.WebHostHtmlMessage.Length > 0)
                {
                    ysodAttachment = CreateHtmlAttachment("YSOD", error.WebHostHtmlMessage);

                    if (ysodAttachment != null)
                        mail.Attachments.Add(ysodAttachment);
                }

                //
                // Send off the mail with some chance to pre- or post-process
                // using event.
                //

                OnMailing(args);
                SendMail(mail);
                OnMailed(args);
            }
            finally
            {
#if NET_1_0 || NET_1_1
                //
                // Delete any attached files, if necessary.
                //
                
                if (ysodAttachment != null)
                {
                    File.Delete(ysodAttachment.Filename);
                    mail.Attachments.Remove(ysodAttachment);
                }
#endif
                OnDisposingMail(args);

#if !NET_1_0 && !NET_1_1
                mail.Dispose();
#endif
            }
        }

        private static MailAttachment CreateHtmlAttachment(string name, string html)
        {
            Debug.AssertStringNotEmpty(name);
            Debug.AssertStringNotEmpty(html);

#if NET_1_0 || NET_1_1
            //
            // Create a temporary file to hold the attachment. Note that 
            // the temporary file is created in the location returned by
            // System.Web.HttpRuntime.CodegenDir. It is assumed that
            // this code will have sufficient rights to create the
            // temporary file in that area.
            //

            string fileName = name + "-" + Guid.NewGuid().ToString() + ".html";
            string path = Path.Combine(HttpRuntime.CodegenDir, fileName);

            try
            {
                using (StreamWriter attachementWriter = File.CreateText(path))
                    attachementWriter.Write(html);

                return new MailAttachment(path);
            }
            catch (IOException)
            {
                //
                // Ignore I/O errors as non-critical. It's not the
                // end of the world if the attachment could not be
                // created (though it would be nice). It is more
                // important to get to deliver the error message!
                //
                
                return null;
            }
#else
            return MailAttachment.CreateAttachmentFromString(html,
                name + ".html", Encoding.UTF8, "text/html");
#endif
        }

        /// <summary>
        /// Creates the <see cref="ErrorTextFormatter"/> implementation to 
        /// be used to format the body of the e-mail.
        /// </summary>

        protected virtual ErrorTextFormatter CreateErrorFormatter()
        {
            return new ErrorMailHtmlFormatter();
        }

        /// <summary>
        /// Sends the e-mail using SmtpMail or SmtpClient.
        /// </summary>

        protected virtual void SendMail(MailMessage mail)
        {
            if (mail == null)
                throw new ArgumentNullException("mail");

#if NET_1_0 || NET_1_1
            SmtpMail.Send(mail);
#else
            //
            // Under .NET Framework 2.0, the authentication settings
            // go on the SmtpClient object rather than mail message
            // so these have to be set up here.
            //

            SmtpClient client = new SmtpClient();

            string host = SmtpServer ?? string.Empty;

            if (host.Length > 0)
            {
                client.Host = host;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
            }

            int port = SmtpPort;
            if (port > 0)
                client.Port = port;

            string userName = AuthUserName ?? string.Empty;
            string password = AuthPassword ?? string.Empty;

            if (userName.Length > 0 && password.Length > 0)
                client.Credentials = new NetworkCredential(userName, password);

            client.EnableSsl = UseSsl;

            client.Send(mail);
#endif
        }

        /// <summary>
        /// Fires the <see cref="Mailing"/> event.
        /// </summary>

        protected virtual void OnMailing(ErrorMailEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            ErrorMailEventHandler handler = Mailing;

            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Fires the <see cref="Mailed"/> event.
        /// </summary>

        protected virtual void OnMailed(ErrorMailEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            ErrorMailEventHandler handler = Mailed;

            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Fires the <see cref="DisposingMail"/> event.
        /// </summary>
        
        protected virtual void OnDisposingMail(ErrorMailEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            ErrorMailEventHandler handler = DisposingMail;

            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Gets the configuration object used by <see cref="OnInit"/> to read
        /// the settings for module.
        /// </summary>

        protected virtual object GetConfig()
        {
            return Configuration.GetSubsection("errorMail");
        }

        /// <summary>
        /// Builds an <see cref="Error"/> object from the last context
        /// exception generated.
        /// </summary>

        [ Obsolete ]
        protected virtual Error GetLastError(HttpContext context)
        {
            throw new NotSupportedException();
        }

        private static string GetSetting(IDictionary config, string name)
        {
            return GetSetting(config, name, null);
        }

        private static string GetSetting(IDictionary config, string name, string defaultValue)
        {
            Debug.Assert(config != null);
            Debug.AssertStringNotEmpty(name);

            string value = Mask.NullString((string) config[name]);

            if (value.Length == 0)
            {
                if (defaultValue == null)
                {
                    throw new ApplicationException(string.Format(
                        "The required configuration setting '{0}' is missing for the error mailing module.", name));
                }

                value = defaultValue;
            }

            return value;
        }
    }
}
