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

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;
    using System.Reflection;
    using System.Threading;
    using System.Web;
    using System.Xml;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using IDictionary = System.Collections.IDictionary;

    #endregion

    /// <summary>
    /// HTTP module implementation that logs unhandled exceptions in an
    /// ASP.NET Web application to an error log.
    /// </summary>

    public sealed class ErrorModule : HttpModuleBase
    {
        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>

        protected override void OnInit(HttpApplication application)
        {
            if (application == null) throw new ArgumentNullException("application");

            application.Error += (sender, _) =>
            {
                var app = (HttpApplication) sender;
                var exception = app.Server.GetLastError();
                var context = new HttpContextWrapper(app.Context);
                ErrorEvent.Fire(this, exception, context);
            };
        }
    }

    public sealed class ExceptionFilterEvent : Event<ExceptionFilterEventArgs>
    {
        public static bool Fire(EventStation es, object sender, Exception exception, object context)
        {
            var handler = es.Find<ExceptionFilterEvent>();
            if (handler == null) 
                return false;
            var args = new ExceptionFilterEventArgs(exception, context);
            handler.Fire(sender, EventFiringEventArgs.Create(args));
            return args.Dismissed;
        }
    }

    public sealed class ErrorLogEvent : Event<ErrorLoggedEventArgs> { }

    public static class Extensions
    {
        public static EventConnectionHandler Filter(NameValueCollection settings)
        {
            var config = (ErrorFilterConfiguration)Configuration.GetSubsection("errorFilter");

            if (config == null)
                return delegate { };

            var assertion = config.Assertion;

            return Filter(assertion.Test);
        }

        public static EventConnectionHandler Filter(Func<ErrorFilterModule.AssertionHelperContext, bool> predicate)
        {
            return es =>
            {
                es.Get<ExceptionFilterEvent>().Firing += (sender, args) =>
                {
                    try
                    {
                        if (predicate(new ErrorFilterModule.AssertionHelperContext(/* TODO source */ sender, args.Payload.Exception, args.Payload.Context)))
                            args.Payload.Dismiss();
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e);
                        throw; // TODO PrepareToRethrow()?
                    }
                };
            };
        }

        public static EventConnectionHandler Log(NameValueCollection settings)
        {
            return es =>
            {
                es.Get<ErrorEvent>().Firing += (_, args) =>
                {
                    var payload = args.Payload;
                    var exception = payload.Exception;
                    if (exception != null
                        && ExceptionFilterEvent.Fire(es, /* TODO source? */ null, exception, payload.ErrorContext))
                            return;

                    var log = ErrorLog.GetDefault(null);
                    var error = payload.Error;
                    var id = log.Log(error);
                    
                    var handler = es.Find<ErrorLogEvent>();
                    if (handler != null)
                        handler.Fire(null, EventFiringEventArgs.Create(new ErrorLoggedEventArgs(new ErrorLogEntry(log, id, error))));
                };
            };
        }

        public class ErrorMailReportingOptions
        {
            public string MailRecipient { get; set; }
            public string MailSender { get; set; }
            public string MailCopyRecipient { get; set; }
            public string MailSubjectFormat { get; set; }
            public MailPriority MailPriority { get; set; }
            public bool ReportAsynchronously { get; set; }
            public string SmtpServer { get; set; }
            public int SmtpPort { get; set; }
            public string AuthUserName { get; set; }
            public string AuthPassword { get; set; }
            public bool DontSendYsod { get; set; }
            public bool UseSsl { get; set; }
        }

        public static EventConnectionHandler Mail(NameValueCollection settings)
        {
            var config = new Dictionary<string, string>(settings.Count);
            foreach (var i in Enumerable.Range(0, settings.Count))
                config.Add(settings.GetKey(i), settings[i]);

            var mailRecipient = ErrorMailModule.GetSetting(config, "to");
            
            var options = new ErrorMailReportingOptions
            {
                MailRecipient        = mailRecipient, 
                MailSender           = ErrorMailModule.GetSetting(config, "from", mailRecipient), 
                MailCopyRecipient    = ErrorMailModule.GetSetting(config, "cc", string.Empty), 
                MailSubjectFormat    = ErrorMailModule.GetSetting(config, "subject", string.Empty), 
                MailPriority         = (MailPriority) Enum.Parse(typeof(MailPriority), ErrorMailModule.GetSetting(config, "priority", MailPriority.Normal.ToString()), true), 
                ReportAsynchronously = Convert.ToBoolean(ErrorMailModule.GetSetting(config, "async", bool.TrueString)),
                SmtpServer           = ErrorMailModule.GetSetting(config, "smtpServer", string.Empty), 
                SmtpPort             = Convert.ToUInt16(ErrorMailModule.GetSetting(config, "smtpPort", "0"), CultureInfo.InvariantCulture), 
                AuthUserName         = ErrorMailModule.GetSetting(config, "userName", string.Empty),                 
                AuthPassword         = ErrorMailModule.GetSetting(config, "password", string.Empty), 
                DontSendYsod         = Convert.ToBoolean(ErrorMailModule.GetSetting(config, "noYsod", bool.FalseString)), 
                UseSsl               = Convert.ToBoolean(ErrorMailModule.GetSetting(config, "useSsl", bool.FalseString))
            };

            return es =>
            {
                es.Get<ErrorEvent>().Firing += (_, args) =>
                {
                    var error = args.Payload.Error;
                    if (options.ReportAsynchronously)
                    {
                        //
                        // Schedule the reporting at a later time using a worker from 
                        // the system thread pool. This makes the implementation
                        // simpler, but it might have an impact on reducing the
                        // number of workers available for processing ASP.NET
                        // requests in the case where lots of errors being generated.
                        //

                        ThreadPool.QueueUserWorkItem(delegate 
                        {
                            try
                            {
                                ReportError(error, options);
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

                            catch (SmtpException e)
                            {
                                Trace.TraceError(e.ToString());
                            }
                        });
                    }
                    else
                    {
                        ReportError(error, options);
                    }
                };
            };
        }

        /// <summary>
        /// Schedules the error to be e-mailed synchronously.
        /// </summary>

        private static void ReportError(Error error, ErrorMailReportingOptions options)
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
                return;

            //
            // Create the mail, setting up the sender and recipient and priority.
            //

            var mail = new MailMessage();
            mail.Priority = options.MailPriority;

            mail.From = new MailAddress(sender);
            mail.To.Add(recipient);

            if (copyRecipient.Length > 0)
                mail.CC.Add(copyRecipient);

            //
            // Format the mail subject.
            // 

            string subjectFormat = Mask.EmptyString(options.MailSubjectFormat, "Error ({1}): {0}");
            mail.Subject = string.Format(subjectFormat, error.Message, error.Type).
                Replace('\r', ' ').Replace('\n', ' ');

            //
            // Format the mail body.
            //

            var formatter = new ErrorMailHtmlFormatter(); // TODO CreateErrorFormatter();

            var bodyWriter = new StringWriter();
            formatter.Format(bodyWriter, error);
            mail.Body = bodyWriter.ToString();

            switch (formatter.MimeType)
            {
                case "text/html": mail.IsBodyHtml = true; break;
                case "text/plain": mail.IsBodyHtml = false; break;

                default:
                {
                    throw new ApplicationException(string.Format(
                        "The error mail module does not know how to handle the {1} media type that is created by the {0} formatter.",
                        formatter.GetType().FullName, formatter.MimeType));
                }
            }

            var args = EventFiringEventArgs.Create(new ErrorMailEventArgs(error, mail));
            var es = EventStation.Default;

            try
            {
                //
                // If an HTML message was supplied by the web host then attach 
                // it to the mail if not explicitly told not to do so.
                //

                if (!options.DontSendYsod && error.WebHostHtmlMessage.Length > 0)
                {
                    var ysodAttachment = ErrorMailModule.CreateHtmlAttachment("YSOD", error.WebHostHtmlMessage);
                    if (ysodAttachment != null)
                        mail.Attachments.Add(ysodAttachment);
                }

                //
                // Send off the mail with some chance to pre- or post-process
                // using event.
                //

                es.Get<ErrorMailEvents.Mailing>().Fire(null, args);
                SendMail(mail, options);
                es.Get<ErrorMailEvents.Mailed>().Fire(null, args);
            }
            finally
            {
                es.Get<ErrorMailEvents.Mailed>().Fire(null, args);
                mail.Dispose();
            }
        }

        /// <summary>
        /// Sends the e-mail using SmtpMail or SmtpClient.
        /// </summary>
        
        private static void SendMail(MailMessage mail, ErrorMailReportingOptions options)
        {
            if (mail == null)
                throw new ArgumentNullException("mail");

            //
            // Under .NET Framework 2.0, the authentication settings
            // go on the SmtpClient object rather than mail message
            // so these have to be set up here.
            //

            var client = new SmtpClient();

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

            client.EnableSsl = options.UseSsl;

            client.Send(mail);
        }
    }

    public sealed class ErrorEvent : Event<ErrorEvent.Context>
    {
        public static void Fire(object sender, Exception exception, object context)
        {
            Fire(sender, exception, context, EventStation.Default);
        }

        public static void Fire(object sender, Exception exception, object context, EventStation station)
        {
            var error = new Error(exception, context, station);

            var handler = station.Find<ErrorEvent>();
            if (handler == null)
                return;

            var args = EventFiringEventArgs.Create(new Context(error, exception, context));
            handler.Fire(sender, args);
        }

        public sealed class Context
        {
            public Error Error { get; private set; }
            public Exception Exception { get; private set; }
            public object ErrorContext { get; private set; }

            public Context(Error error) : 
                this(error, null) {}

            public Context(Error error, Exception exception) : 
                this(error, exception, null) {}

            public Context(Error error, Exception exception, object context)
            {
                if (exception == null) throw new ArgumentNullException("exception");

                Exception = exception;
                ErrorContext = context;
                Error = error;
            }
        }
    }

    public static class ErrorMailEvents
    {
        public sealed class Mailing   : Event<ErrorMailEventArgs> { }
        public sealed class Mailed    : Event<ErrorMailEventArgs> { }
        public sealed class Disposing : Event<ErrorMailEventArgs> { }
    }

    public static class EventFiringEventArgs
    {
        public static EventFiringEventArgs<T> Create<T>(T payload)
        {
            return new EventFiringEventArgs<T>(payload);
        }
    }

    public class EventFiringEventArgs<T> : EventArgs
    {
        private readonly T _payload;

        public EventFiringEventArgs() : 
            this(default(T)) {}

        public EventFiringEventArgs(T payload)
        {
            _payload = payload;
        }

        public bool IsHandled { get; set; }
        public T Payload { get { return _payload; } }
        public object Result { get; set; }
    }

    public delegate void EventFiringHandler<T>(object sender, EventFiringEventArgs<T> args);

    public class Event<T>
    {
        public event EventFiringHandler<T> Firing;

        public virtual void Fire(object sender, EventFiringEventArgs<T> args)
        {
            if (args.IsHandled) // Rare but possible
                return;
            var handler = Firing;
            if (handler == null) 
                return;
            Fire(handler.GetInvocationList(), sender, args);
        }

        private static void Fire(IEnumerable<Delegate> handlers, object sender, EventFiringEventArgs<T> args)
        {
            Debug.Assert(handlers != null);
            Debug.Assert(args != null);
            Debug.Assert(!args.IsHandled);

            foreach (EventFiringHandler<T> handler in handlers)
            {
                handler(sender, args);
                if (args.IsHandled)
                    return;
            }
        }
    }

    public delegate void EventConnectionHandler(EventStation container);
    public delegate EventConnectionHandler ExtensionSetupHandler(NameValueCollection settings);

    public sealed class EventStation
    {
        private readonly Dictionary<Type, object> _events = new Dictionary<Type, object>();

        public T Get<T>() where T : class, new()
        {
            return (Find<T>() ?? (T) (_events[typeof(T)] = new T()));
        }

        public T Find<T>() where T : class
        {
            return (T) _events.Find(typeof(T));
        }

        private static ICollection<EventConnectionHandler> _modules = Array.AsReadOnly(new EventConnectionHandler[]
        {
            ErrorInitialization.InitUserName, 
            ErrorInitialization.InitHostName, 
            ErrorInitialization.InitWebCollections, 
        });

        private static readonly EventConnectionHandler[] _zeroModules = new EventConnectionHandler[0];

        static EventStation()
        {
            AppendModules(LoadModules());
        }

        public static EventConnectionHandler[] LoadModules()
        {
            var config = (IDictionary) Configuration.GetSubsection("modules");
            return config != null ? LoadModules(config) : _zeroModules;
        }

        public static EventConnectionHandler[] LoadModules(IDictionary config)
        {
            if (config == null) throw new ArgumentNullException("config");

            var customizations = new List<EventConnectionHandler>(config.Count);

            var e = config.GetEnumerator();
            while (e.MoveNext())
            {
                var xqn = (XmlQualifiedName)e.Key;

                if (0 == string.CompareOrdinal(xqn.Namespace, "elmah"))
                    continue;

                string assemblyName, ns;

                if (!Assertions.AssertionFactory.DecodeClrTypeNamespaceFromXmlNamespace(xqn.Namespace, out ns, out assemblyName) ||
                    ns.Length > 0)
                {
                    throw new Exception(string.Format("Error decoding CLR type namespace and assembly from the XML namespace '{0}'.", xqn.Namespace));
                }

                var assembly = Assembly.Load(assemblyName);
                var type = assembly.GetType(ns + ".ErrorInitialization", /* throwOnError */ true);
                var handler = (ExtensionSetupHandler)Delegate.CreateDelegate(typeof(ExtensionSetupHandler), type, xqn.Name, true, /* throwOnBindFailure */ false);
                // TODO Null handler handling
                var settings = (NameValueCollection)e.Value;
                customizations.Add(handler(settings));
            }

            return customizations.ToArray();
        }

        public static ICollection<EventConnectionHandler> Modules
        {
            get { return _modules; }
        }

        public static void AppendModules(params EventConnectionHandler[] modules)
        {
            SetModules(modules, true);
        }

        public static void ResetModules(params EventConnectionHandler[] modules)
        {
            SetModules(modules, false);
        }

        public static void SetModules(EventConnectionHandler[] modules, bool append)
        {
            if (modules == null)
                return;

            ICollection<EventConnectionHandler> current;
            EventConnectionHandler[] updated;
            do
            {
                current = _modules;
                int currentCount = append ? current.Count : 0;
                updated = new EventConnectionHandler[currentCount + modules.Length];
                if (append)
                    current.CopyTo(updated, 0);
                Array.Copy(modules, 0, updated, currentCount, modules.Length);
                // TODO handle duplicates
            }
            while (current != Interlocked.CompareExchange(ref _modules, Array.AsReadOnly(updated), current));
        }

        [ThreadStatic] static EventStation _thread;
        [ThreadStatic] static object _dependency;

        public static EventStation Default
        {
            get
            {
                var modules = Modules;
                var self = _dependency == modules
                         ? _thread : null;
                if (self == null)
                {
                    self = new EventStation();
                    _dependency = modules;
                    foreach (var module in modules)
                        module(self);
                    _thread = self;
                }
                return self;
            }
        }
    }

    public class ErrorInitializationContext
    {
        private readonly Error _error;
        private readonly object _context;

        public ErrorInitializationContext(Error error) : 
            this(error, null) {}

        public ErrorInitializationContext(Error error, object context)
        {
            if (error == null) throw new ArgumentNullException("error");
            _error = error;
            _context = context;
        }

        public Error Error { get { return _error; } }
        public object Context { get { return _context; } }
    }

    public static class ErrorInitialization
    {
        public sealed class Initializing : Event<ErrorInitializationContext> { }
        public sealed class Initialized : Event<ErrorInitializationContext> { }

        private static void OnErrorInitializing(EventStation extensions, ErrorInitializationContext args)
        {
            Debug.Assert(args != null);
            var handler = extensions.Find<Initializing>();
            if (handler != null) handler.Fire(/* TODO sender */ null, new EventFiringEventArgs<ErrorInitializationContext>(args));
        }

        private static void OnErrorInitialized(EventStation extensions, ErrorInitializationContext args)
        {
            Debug.Assert(args != null);
            var handler = extensions.Find<Initialized>();
            if (handler != null) handler.Fire(/* TODO sender */ null, new EventFiringEventArgs<ErrorInitializationContext>(args));
        }

        public static void Initialize(EventStation extensions, ErrorInitializationContext args)
        {
            OnErrorInitializing(extensions, args);
            OnErrorInitialized(extensions, args);
        }

        public static EventConnectionHandler InitHostName(NameValueCollection settings)
        {
            return InitHostName;
        }

        public static void InitHostName(EventStation container)
        {
            container.Get<Initializing>().Firing += (_, args) => InitHostName(args.Payload);
        }

        private static void InitHostName(ErrorInitializationContext args)
        {
            var context = args.Context as HttpContext;
            args.Error.HostName = Environment.TryGetMachineName(context != null ? new HttpContextWrapper(context) : null);
        }

        public static void InitUserName(EventStation container)
        {
            container.Get<Initializing>().Firing += (_, args) =>
            {
                args.Payload.Error.User = Thread.CurrentPrincipal.Identity.Name ?? String.Empty;
            };
        }

        public static void InitWebCollections(EventStation container)
        {
            container.Get<Initializing>().Firing += (_, args) => InitWebCollections(args.Payload);
        }

        private static void InitWebCollections(ErrorInitializationContext args)
        {
            var error = args.Error;
            var e = error.Exception;

            //
            // If this is an HTTP exception, then get the status code
            // and detailed HTML message provided by the host.
            //

            var httpException = e as HttpException;

            if (httpException != null)
            {
                error.StatusCode = httpException.GetHttpCode();
                error.WebHostHtmlMessage = Error.TryGetHtmlErrorMessage(httpException) ?? String.Empty;
            }

            //
            // If the HTTP context is available, then capture the
            // collections that represent the state request.
            //

            if (args.Context != null)
            {
                var sp = args.Context as IServiceProvider;
                if (sp != null)
                {
                    var hc = ((HttpApplication)sp.GetService(typeof(HttpApplication))).Context;
                    if (hc != null)
                    {
                        var webUser = hc.User;
                        if (webUser != null
                            && (webUser.Identity.Name ?? String.Empty).Length > 0)
                        {
                            error.User = webUser.Identity.Name;
                        }

                        var request = hc.Request;

                        error.ServerVariables.Add(request.ServerVariables);
                        error.QueryString.Add(request.QueryString);
                        error.Form.Add(request.Form);
                        var cookies = request.Cookies;
                        error.Cookies.Add(from i in Enumerable.Range(0, cookies.Count)
                                          let cookie = cookies[i]
                                          select cookie.Name.AsKeyTo(cookie.Value));
                    }
                }
            }
        }
    }
}
