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
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;
    using System.Runtime.InteropServices;
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
                ExceptionEvent.Fire(this, exception, context);
            };
        }
    }

    public sealed class ExceptionFilterEvent : Event<ExceptionFilterEventArgs, bool>
    {
        public static bool Fire(ExtensionHub ehub, object sender, Exception exception, object context)
        {
            var handler = ehub.Find<ExceptionFilterEvent>();
            if (handler == null) 
                return false;
            var args = new ExceptionFilterEventArgs(exception, context);
            return handler.Fire(sender, args);
        }
    }

    public sealed class ErrorLogEvent : Event<ErrorLoggedEventArgs> { }

    namespace Modules
    {
        public static class Configuration
        {
            public class Entry
            {
                public string Name { get; private set; }
                public string TypeName { get; private set; }
                public IEnumerable<KeyValuePair<string, string>> Settings { get; private set; }

                public Entry(string name, string typeName, IEnumerable<KeyValuePair<string, string>> settings)
                {
                    //TODO arg checking
                    Name = name;
                    TypeName = typeName;
                    Settings = settings.ToArray(); // Don't trust source so copy
                }

                public ModuleConnectionHandler LoadInitialized()
                {
                    return Load((m, s) => m.Initialize(s));
                }

                public virtual T Load<T>(Func<Module, object, T> resultor)
                {
                    if (resultor == null) throw new ArgumentNullException("resultor");

                    var type = Type.GetType(TypeName, /* throwOnError */ true);
                    Debug.Assert(type != null);
                    // TODO check type compatibility

                    var module = (Module)Activator.CreateInstance(type);
                    module.Name = Name;

                    var descriptor = module.SettingsDescriptor;
                    var settingsObject = module.CreateSettings();
                    if (descriptor != null && settingsObject != null)
                    {
                        var properties = descriptor.GetProperties();
                        foreach (var ee in Settings)
                        {
                            var property = properties.Find(ee.Key, true);
                            // TODO property != null
                            var converter = property.Converter;
                            // TODO converter != null
                            property.SetValue(settingsObject, converter.ConvertFromInvariantString(ee.Value));
                        }
                    }

                    return resultor(module, settingsObject);
                }
            }

            public static IEnumerable<Entry> Parse()
            {
                return Parse(null);
            }

            public static IEnumerable<Entry> Parse(NameValueCollection config)
            {
                return ParseImpl(config ?? ConfigurationManager.AppSettings);
            }

            static IEnumerable<Entry> ParseImpl(NameValueCollection config)
            {
                Debug.Assert(config != null);

                return
                    from item in (config["Elmah.Modules"] ?? string.Empty).Split(',')
                    let name = item.Trim()
                    where name.Length > 0
                    let prefix = name + "."
                    let typeName = ValidatedTypeName((config[name] ?? string.Empty))
                    let settings =
                        from i in Enumerable.Range(0, config.Count)
                        let key = config.GetKey(i).Trim()
                        where key.Length > prefix.Length
                            && key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        select key.Substring(prefix.Length).AsKeyTo(config[i])
                    select new Entry(name, typeName, settings);
            }

            private static string ValidatedTypeName(string input)
            {
                Debug.Assert(input != null);
                var trimmed = input.Trim();
                if (trimmed.Length == 0)
                    throw new Exception(String.Format("Missing '{0}' module type specification.", input));
                return trimmed;
            }

            public static IEnumerable<T> Parse<T>(Func<Module, object, T> resultor)
            {
                return Parse(null, resultor);
            }

            public static IEnumerable<T> Parse<T>(
                NameValueCollection config,
                Func<Module, object, T> resultor)
            {
                if (resultor == null) throw new ArgumentNullException("resultor");

                var entries = Parse(config).ToArray();
                return from entry in entries
                       select entry.Load(resultor);
            }
        }

        public class Module
        {
            public virtual string Name { get; set; }
            public bool HasSettingsSupport { get { return SettingsDescriptor != null; } }
            public virtual ICustomTypeDescriptor SettingsDescriptor { get { return null; } }
            public virtual object CreateSettings() { return null; }
            public virtual ModuleConnectionHandler Initialize(object settings) { return delegate { }; }
        }

        public class Module<T> : Module where T : new()
        {
            public override ICustomTypeDescriptor SettingsDescriptor { get { return TypeDescriptor.GetProvider(typeof(T)).GetTypeDescriptor(typeof(T)); } }
            public override object CreateSettings() { return new T(); }
            public sealed override ModuleConnectionHandler Initialize(object settings) { return Initialize((T)settings); }
            protected virtual ModuleConnectionHandler Initialize(T settings) { return base.Initialize(settings); }
        }

        public class LogModule : Module
        {
            public override ModuleConnectionHandler Initialize(object settings)
            {
                return ehub => ehub.Get<ExceptionEvent>().AddHandler(next => (sender, args) =>
                {
                    var exception = args.Exception;
                    if (exception == null || !ExceptionFilterEvent.Fire(ehub, this, exception, args.ExceptionContext))
                    {
                        var log = ErrorLog.GetDefault(null);
                        var error = args.CreateError(ehub);
                        var id = log.Log(error);

                        var handler = ehub.Find<ErrorLogEvent>();
                        if (handler != null)
                            handler.Fire(this, new ErrorLoggedEventArgs(new ErrorLogEntry(log, id, error)));
                    }

                    return new Unit();
                });
            }
        }

        public class FilterModule : Module
        {
            public override ModuleConnectionHandler Initialize(object settings)
            {
                var config = (ErrorFilterConfiguration) Elmah.Configuration.GetSubsection("errorFilter");

                if (config == null)
                    return delegate { };

                var assertion = config.Assertion;

                return Filter(assertion.Test);
            }

            public static ModuleConnectionHandler Filter(Func<ErrorFilterModule.AssertionHelperContext, bool> predicate)
            {
                return ehub => ehub.Get<ExceptionFilterEvent>().AddHandler(next => (sender, args) =>
                {
                    try
                    {
                        return predicate(new ErrorFilterModule.AssertionHelperContext(sender, args.Exception, args.Context));
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e);
                        throw; // TODO PrepareToRethrow()?
                    }
                });
            }
        }

        public class MailModuleSettings
        {
            public /* TODO MailAddressCollection */ string To { get; set; }
            public /* TODO MailAddress */ string From { get; set; }
            // ReSharper disable InconsistentNaming
            public /* TODO MailAddressCollection */ string CC { get; set; } // ReSharper restore InconsistentNaming
            public string Subject { get; set; }
            public MailPriority Priority { get; set; }
            public bool Async { get; set; } // TODO Invert!
            public string SmtpServer { get; set; }
            public int SmtpPort { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public bool NoYsod { get; set; }
            public bool UseSsl { get; set; }
        }

        public class MailModule : Module<MailModuleSettings>
        {
            protected override ModuleConnectionHandler Initialize(MailModuleSettings settings)
            {
                return Mail(new ErrorMailReportingOptions
                {
                    MailSender = settings.From,
                    MailRecipient = settings.To,
                    MailCopyRecipient = settings.CC,
                    MailSubjectFormat = settings.Subject,
                    MailPriority = settings.Priority,
                    ReportAsynchronously = settings.Async,
                    SmtpServer = settings.SmtpServer,
                    SmtpPort = settings.SmtpPort,
                    AuthUserName = settings.UserName,
                    AuthPassword = settings.Password,
                    DontSendYsod = settings.NoYsod,
                    UseSsl = settings.UseSsl,
                });
            }

            class ErrorMailReportingOptions
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

            ModuleConnectionHandler Mail(ErrorMailReportingOptions options)
            {
                return ehub => ehub.Get<ExceptionEvent>().AddHandler(next => (sender, args) =>
                {
                    var exception = args.Exception;
                    if (exception == null || !ExceptionFilterEvent.Fire(ehub, this, exception, args.ExceptionContext))
                    {
                        var error = args.CreateError(ehub);

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
                    }

                    return new Unit();
                });
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

                var sender = options.MailSender ?? String.Empty;
                var recipient = options.MailRecipient ?? String.Empty;
                var copyRecipient = options.MailCopyRecipient ?? String.Empty;

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
                mail.Subject = String.Format(subjectFormat, error.Message, error.Type).
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
                            throw new ApplicationException(String.Format(
                                "The error mail module does not know how to handle the {1} media type that is created by the {0} formatter.",
                                formatter.GetType().FullName, formatter.MimeType));
                        }
                }

                var args = new ErrorMailEventArgs(error, mail);
                var ehub = ExtensionHub.Default;

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

                    ehub.Get<ErrorMailEvents.Mailing>().Fire(/* TODO sender */ null, args);
                    SendMail(mail, options);
                    ehub.Get<ErrorMailEvents.Mailed>().Fire(/* TODO sender */ null, args);
                }
                finally
                {
                    ehub.Get<ErrorMailEvents.Mailed>().Fire(/* TODO sender */ null, args);
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

                var host = options.SmtpServer ?? String.Empty;

                if (host.Length > 0)
                {
                    client.Host = host;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                }

                var port = options.SmtpPort;
                if (port > 0)
                    client.Port = port;

                var userName = options.AuthUserName ?? String.Empty;
                var password = options.AuthPassword ?? String.Empty;

                if (userName.Length > 0 && password.Length > 0)
                    client.Credentials = new NetworkCredential(userName, password);

                client.EnableSsl = options.UseSsl;

                client.Send(mail);
            }
        }
    }

    public sealed class ExceptionEvent : Event<ExceptionEvent.Context>
    {
        public static void Fire(object sender, Exception exception, object context)
        {
            Fire(sender, exception, context, ExtensionHub.Default);
        }

        public static void Fire(object sender, Exception exception, object context, ExtensionHub ehub)
        {
            var handler = ehub.Find<ExceptionEvent>();
            if (handler == null)
                return;

            handler.Fire(sender, new Context(exception, context));
        }

        public sealed class Context
        {
            public Exception Exception { get; private set; }
            public object ExceptionContext { get; private set; }

            public Context(Exception exception) : 
                this(exception, null) {}

            public Context(Exception exception, object context)
            {
                if (exception == null) throw new ArgumentNullException("exception");

                Exception = exception;
                ExceptionContext = context;
            }

            public Error CreateError(ExtensionHub ehub)
            {
                return new Error(Exception, ExceptionContext, ehub);
            }
        }
    }

    public static class ErrorMailEvents
    {
        public sealed class Mailing   : Event<ErrorMailEventArgs> { }
        public sealed class Mailed    : Event<ErrorMailEventArgs> { }
        public sealed class Disposing : Event<ErrorMailEventArgs> { }
    }

    sealed class ModulesSectionHandler : DictionarySectionHandler
    {
        protected override object GetKey(XmlNode node) { return GetKey(node, "name"); }
        
        protected override void OnAdd(IDictionary dictionary, object key, XmlNode node)
        {
            if (dictionary == null) throw new ArgumentNullException("dictionary");
            if (key == null) throw new ArgumentNullException("key");
            if (node == null) throw new ArgumentNullException("node");

            var typeName = GetValue(node, "type");
            if (string.IsNullOrEmpty(typeName))
            {
                var message = String.Format("Missing type specification for module named '{0}'.", (object[]) key);
                throw new ConfigurationException(message, node);
            }

            dictionary.Add(key, new ModuleSectionEntry(key.ToString(), typeName));
        }
    }

    class ModuleSectionEntry
    {
        private NameValueCollection _settings;
        
        public string Name { get; private set; }
        public string TypeName { get; private set; }
        
        public NameValueCollection Settings
        {
            get { return _settings ?? (_settings = GetSettings()); }
        }

        public ModuleSectionEntry(string name, string typeName)
        {
            Debug.AssertStringNotEmpty(name);
            Debug.AssertStringNotEmpty(typeName);

            Name = name;
            TypeName = typeName;
        }

        private NameValueCollection GetSettings()
        {
            var dict = (IDictionary) Configuration.GetSubsection(Name);
            var settings = new NameValueCollection();
            settings.Add(from System.Collections.DictionaryEntry e in dict 
                         select e.Key.ToString().AsKeyTo(e.Value.ToString()));
            return settings;
        }
    }
   
    public delegate void ModuleConnectionHandler(ExtensionHub ehub);

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct Unit : IEquatable<Unit>
    {
        public static bool operator ==(Unit unit1, Unit unit2) { return true; }
        public static bool operator !=(Unit unit1, Unit unit2) { return false; }
        public bool Equals(Unit other) { return true; }
        public override bool Equals(object obj) { return obj is Unit; }
        public override int GetHashCode() { return 0; }
    }

    sealed class DelegatingDisposable : IDisposable
    {
        private Action _disposer;

        public DelegatingDisposable(Action disposer)
        {
            if (disposer == null) throw new ArgumentNullException("disposer");
            _disposer = disposer;
        }

        public void Dispose()
        {
            var disposer = _disposer;
            if (disposer == null)
                return;
            _disposer = null;
            disposer();
        }
    }

    public class Event<TInput, TOutput>
    {
        private Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>> _binder;
        private Func<object, TInput, TOutput> _cachedHandler;

        public IDisposable AddHandler(Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>> binder)
        {
            if (binder == null) throw new ArgumentNullException("binder");
            
            _binder += binder;
            _cachedHandler = null;
            return new DelegatingDisposable(() => RemoveHandler(binder));
        }

        private void RemoveHandler(Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>> binder)
        {
            if (binder == null) throw new ArgumentNullException("binder");

            _binder -= binder;
            _cachedHandler = null;
        }

        public TOutput Fire(TInput input)
        {
            return Fire(null, input);
        }

        public virtual TOutput Fire(object sender, TInput input)
        {
            var handler = _cachedHandler;
            if (handler == null)
            {
                var binder = _binder;
                if (binder == null)
                    return default(TOutput);

                handler = _cachedHandler = GetBinders().Aggregate((Func<object, TInput, TOutput>) null, (next, b) => b(next));
            }

            return handler(sender, input);
        }

        protected virtual IEnumerable<Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>>> GetBinders()
        {
            var delegates = _binder.GetInvocationList();
            Array.Reverse(delegates);
            var binders =
                from Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>> d 
                    in delegates 
                select d;
            return binders;
        }
    }

    public class Event<TInput> : Event<TInput, Unit>
    {
        public new void Fire(TInput input)
        {
            Fire(null, input);
        }

        public new void Fire(object sender, TInput input)
        {
            base.Fire(sender, input);
        }
    }

    public sealed class ExtensionHub
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

        private static ICollection<ModuleConnectionHandler> _modules = Array.AsReadOnly(new ModuleConnectionHandler[]
        {
            ErrorInitialization.InitUserName, 
            ErrorInitialization.InitHostName, 
            ErrorInitialization.InitWebCollections, 
        });

        private static readonly ModuleConnectionHandler[] _zeroModules = new ModuleConnectionHandler[0];

        static ExtensionHub()
        {
            // TODO Move out of here to avoid type initialization from failing
            AppendModules(LoadModules());
        }

        public static ModuleConnectionHandler[] LoadModules()
        {
            return LoadModules(null);
        }

        public static ModuleConnectionHandler[] LoadModules(NameValueCollection config)
        {
            return Elmah.Modules.Configuration.Parse(config)
                                              .Select(entry => entry.LoadInitialized())
                                              .ToArray();
        }

        public static ICollection<ModuleConnectionHandler> Modules
        {
            get { return _modules; }
        }

        public static void AppendModules(params ModuleConnectionHandler[] modules)
        {
            SetModules(modules, true);
        }

        public static void ResetModules(params ModuleConnectionHandler[] modules)
        {
            SetModules(modules, false);
        }

        public static void SetModules(ModuleConnectionHandler[] modules, bool append)
        {
            if (modules == null)
                return;

            ICollection<ModuleConnectionHandler> current;
            ModuleConnectionHandler[] updated;
            do
            {
                current = _modules;
                int currentCount = append ? current.Count : 0;
                updated = new ModuleConnectionHandler[currentCount + modules.Length];
                if (append)
                    current.CopyTo(updated, 0);
                Array.Copy(modules, 0, updated, currentCount, modules.Length);
                // TODO handle duplicates
            }
            while (current != Interlocked.CompareExchange(ref _modules, Array.AsReadOnly(updated), current));
        }

        [ThreadStatic] static ExtensionHub _thread;
        [ThreadStatic] static object _dependency;

        public static ExtensionHub Default
        {
            get
            {
                var modules = Modules;
                var self = _dependency == modules
                         ? _thread : null;
                if (self == null)
                {
                    self = new ExtensionHub();
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
        public sealed class Initialized  : Event<ErrorInitializationContext> { }

        private static void OnErrorInitializing(ExtensionHub ehub, ErrorInitializationContext args)
        {
            Debug.Assert(args != null);
            var handler = ehub.Find<Initializing>();
            if (handler != null) handler.Fire(/* TODO sender */ null, args);
        }

        private static void OnErrorInitialized(ExtensionHub ehub, ErrorInitializationContext args)
        {
            Debug.Assert(args != null);
            var handler = ehub.Find<Initialized>();
            if (handler != null) handler.Fire(/* TODO sender */ null, args);
        }

        public static void Initialize(ExtensionHub ehub, ErrorInitializationContext args)
        {
            OnErrorInitializing(ehub, args);
            OnErrorInitialized(ehub, args);
        }

        public static ModuleConnectionHandler InitHostName(NameValueCollection settings)
        {
            return InitHostName;
        }

        public static void InitHostName(ExtensionHub ehub)
        {
            ehub.Get<Initializing>().AddHandler(next => (sender, args) => { InitHostName(args); return new Unit(); });
        }

        private static void InitHostName(ErrorInitializationContext args)
        {
            var context = args.Context as HttpContext;
            args.Error.HostName = Environment.TryGetMachineName(context != null ? new HttpContextWrapper(context) : null);
        }

        public static void InitUserName(ExtensionHub ehub)
        {
            ehub.Get<Initializing>().AddHandler(next => (sender, args) =>
            {
                args.Error.User = Thread.CurrentPrincipal.Identity.Name ?? String.Empty;
                return new Unit();
            });
        }

        public static void InitWebCollections(ExtensionHub ehub)
        {
            ehub.Get<Initializing>().AddHandler(next => (sender, args) => { InitWebCollections(args); return new Unit(); });
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
