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

[assembly: Elmah.Scc("$Id: ErrorMailModule.cs 923 2011-12-23 22:02:10Z azizatif $")]

namespace Elmah
{
    #region Imports
    
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Net.Mail;
    using MailAttachment = System.Net.Mail.Attachment;
    using IDictionary = System.Collections.IDictionary;
    using ThreadPool = System.Threading.ThreadPool;

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
        private bool _reportAsynchronously;
        Func<Error, CancellationToken, Task> _mailer;

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

            var config = (IDictionary) GetConfig();

            if (config == null)
                return;

            //
            // Extract the settings.
            //

            var reportAsynchronously = Convert.ToBoolean(GetSetting(config, "async", bool.TrueString));
            string mailRecipient;

            var mailer = ErrorMail.CreateMailer(new ErrorMail.Settings
            {
                MailRecipient     = mailRecipient = GetSetting(config, "to"),
                MailSender        = GetSetting(config, "from", mailRecipient),
                MailCopyRecipient = GetSetting(config, "cc", string.Empty),
                MailSubjectFormat = GetSetting(config, "subject", string.Empty),
                MailPriority      = (MailPriority)Enum.Parse(typeof(MailPriority), GetSetting(config, "priority", MailPriority.Normal.ToString()), true),
                SmtpServer        = GetSetting(config, "smtpServer", string.Empty),
                SmtpPort          = Convert.ToUInt16(GetSetting(config, "smtpPort", "0"), CultureInfo.InvariantCulture),
                AuthUserName      = GetSetting(config, "userName", string.Empty),
                AuthPassword      = GetSetting(config, "password", string.Empty),
                DontSendYsod      = Convert.ToBoolean(GetSetting(config, "noYsod", bool.FalseString)),
                UseSsl            = Convert.ToBoolean(GetSetting(config, "useSsl", bool.FalseString)),
                OnMailing         = OnFirer(OnMailing),
                OnMailed          = OnFirer(OnMailed),
                OnDisposingMail   = OnFirer(OnDisposingMail),
            });

            //
            // Hook into the Error event of the application.
            //

            application.Error += OnError;
            ErrorSignal.Get(application).Raised += OnErrorSignaled;
            
            //
            // Finally, commit the state of the module if we got this far.
            // Anything beyond this point should not cause an exception.
            //

            _reportAsynchronously = reportAsynchronously;
            _mailer = mailer;
        }

        static Func<Error, MailMessage, CancellationToken, Task> OnFirer(Action<ErrorMailEventArgs> handler)
        {
            return (err, mm, _) => Async.RunSynchronously(() =>
            {
                handler(new ErrorMailEventArgs(err, mm));
                return (object) null;
            });
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
        /// The handler called when an unhandled exception bubbles up to 
        /// the module.
        /// </summary>

        protected virtual void OnError(object sender, EventArgs e)
        {
            var context = new HttpContextWrapper(((HttpApplication) sender).Context);
            OnError(context.Server.GetLastError(), context);
        }

        /// <summary>
        /// The handler called when an exception is explicitly signaled.
        /// </summary>

        protected virtual void OnErrorSignaled(object sender, ErrorSignalEventArgs args)
        {
            using (args.Exception.TryScopeCallerInfo(args.CallerInfo))
                OnError(args.Exception, args.Context);
        }

        /// <summary>
        /// Reports the exception.
        /// </summary>

        protected virtual void OnError(Exception e, HttpContextBase context)
        {
            if (e == null) 
                throw new ArgumentNullException("e");

            //
            // Fire an event to check if listeners want to filter out
            // reporting of the uncaught exception.
            //

            var args = new ExceptionFilterEventArgs(e, context);
            OnFiltering(args);

            if (args.Dismissed)
                return;

            //
            // Get the last error and then report it synchronously or 
            // asynchronously based on the configuration.
            //

            var error = new Error(e, context);

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
            var handler = Filtering;
            
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

            ThreadPool.QueueUserWorkItem(ReportError, error);
        }

        private void ReportError(object state)
        {
            var task = _mailer((Error) state, CancellationToken.None);

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

            if (task.IsFaulted)
            {
                Debug.Assert(task.Exception != null);
                task.Exception.Handle(ee =>
                {
                    if (!(ee is SmtpException))
                        return false;
                    Trace.TraceError(ee.ToString());
                    return true;
                });
            }
        }

        /// <summary>
        /// Fires the <see cref="Mailing"/> event.
        /// </summary>

        protected virtual void OnMailing(ErrorMailEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            var handler = Mailing;

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

            var handler = Mailed;

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

            var handler = DisposingMail;

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

        static string GetSetting(IDictionary config, string name, string defaultValue = null)
        {
            Debug.Assert(config != null);
            Debug.AssertStringNotEmpty(name);

            var value = ((string) config[name]) ?? string.Empty;

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
