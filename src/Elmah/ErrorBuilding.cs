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
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using Modules;

    #endregion

    public sealed class ExceptionFilterMessage : Message<ExceptionFilterEventArgs, bool>
    {
        public static bool Send(ExtensionHub ehub, object sender, Exception exception, object context)
        {
            var handler = ehub.Find<ExceptionFilterMessage>();
            if (handler == null) 
                return false;
            var args = new ExceptionFilterEventArgs(exception, context);
            return handler.Send(sender, args);
        }
    }

    public delegate void ErrorLoggedEventHandler(object sender, ErrorLoggedEventArgs args);

    [Serializable]
    public sealed class ErrorLoggedEventArgs : EventArgs
    {
        private readonly ErrorLogEntry _entry;

        public ErrorLoggedEventArgs(ErrorLogEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            _entry = entry;
        }

        public ErrorLogEntry Entry
        {
            get { return _entry; }
        }
    }

    public sealed class ErrorLoggedEvent : Event<ErrorLoggedEventArgs> { }

    namespace Modules
    {
        using System.Reflection;
        using System.Text;
        using System.Net.Mail;

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

                public InitializedModule LoadInitialized()
                {
                    return Load(InitializedModule.Initialize);
                }

                public virtual T Load<T>(Func<Module, object, T> resultor)
                {
                    if (resultor == null) throw new ArgumentNullException("resultor");

                    var type = Type.GetType(TypeName, /* throwOnError */ true);
                    Debug.Assert(type != null);
                    // TODO check type compatibility

                    var module = (Module) Activator.CreateInstance(type, Name);

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
                    from item in (config["Elmah.Modules"] ?? string.Empty).Split(StringSeparatorStock.Comma)
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
            public Module(string name)
            {
                if (name == null) throw new ArgumentNullException("name");
                if (name.Trim().Length == 0) throw new ArgumentException(null, "name");
                Name = name;
            }

            public string Name { get; private set; }
            public bool HasSettingsSupport { get { return SettingsDescriptor != null; } }
            public virtual ICustomTypeDescriptor SettingsDescriptor { get { return null; } }
            public virtual object CreateSettings() { return null; }
            public virtual ModuleConnectionHandler Initialize(object settings) { return delegate { }; }
        }

        public sealed class InitializedModule
        {
            public Module Module { get; private set; }
            public object Settings { get; private set; }
            public ModuleConnectionHandler ConnectionHandler { get; private set; }

            private InitializedModule(Module module, object settings, ModuleConnectionHandler connectionHandler)
            {
                Debug.Assert(module != null);
                Debug.Assert(connectionHandler != null);
                Module = module;
                Settings = settings;
                ConnectionHandler = connectionHandler;
            }

            public static InitializedModule Initialize(Module module, object settings)
            {
                if (module == null) throw new ArgumentNullException("module");
                return new InitializedModule(module, settings, module.Initialize(settings));
            }

            public void Connect(ExtensionHub ehub)
            {
                ConnectionHandler(ehub);
            }

            public override string ToString()
            {
                return Module.Name;
            }

            public static InitializedModule Static(string name, ModuleConnectionHandler connectionHandler)
            {
                return Initialize(new StaticModule(name, connectionHandler), null);
            }
            
            sealed class StaticModule : Module
            {
                private readonly ModuleConnectionHandler _connectionHandler;

                public StaticModule(string name, ModuleConnectionHandler connectionHandler) :
                    base(name)
                {
                    if (connectionHandler == null) throw new ArgumentNullException("connectionHandler");
                    _connectionHandler = connectionHandler;
                }

                public override ModuleConnectionHandler Initialize(object settings)
                {
                    return _connectionHandler;
                }
            }
        }

        public class Module<T> : Module where T : new()
        {
            public Module(string name) : base(name){}
            public override ICustomTypeDescriptor SettingsDescriptor { get { return TypeDescriptor.GetProvider(typeof(T)).GetTypeDescriptor(typeof(T)); } }
            public override object CreateSettings() { return new T(); }
            public sealed override ModuleConnectionHandler Initialize(object settings) { return Initialize((T)settings); }
            protected virtual ModuleConnectionHandler Initialize(T settings) { return base.Initialize(settings); }
        }

        public class LogModule : Module
        {
            public LogModule(string name) : base(name){}

            public override ModuleConnectionHandler Initialize(object settings)
            {
                return ehub => ehub.OnException((sender, args) =>
                {
                    var exception = args.Exception;
                    if (exception == null || !ExceptionFilterMessage.Send(ehub, this, exception, args.ExceptionContext))
                    {
                        var log = ErrorLog.GetDefault(null);
                        var error = args.CreateError(ehub);
                        var id = log.Log(error);

                        var handler = ehub.Find<ErrorLoggedEvent>();
                        if (handler != null)
                            handler.Fire(this, new ErrorLoggedEventArgs(new ErrorLogEntry(log, id, error)));
                    }
                });
            }
        }

        static class ErrorFilterModule
        {
            // TODO Consolidate duplicate definition and un-nest AssertionHelperContext

            public sealed class AssertionHelperContext
            {
                private readonly object _source;
                private readonly Exception _exception;
                private readonly object _context;
                private Exception _baseException;
                private int _httpStatusCode;
                private bool _statusCodeInitialized;

                public AssertionHelperContext(Exception e, object context) :
                    this(null, e, context) { }

                public AssertionHelperContext(object source, Exception e, object context)
                {
                    Debug.Assert(e != null);

                    _source = source == null ? this : source;
                    _exception = e;
                    _context = context;
                }

                public object FilterSource
                {
                    get { return _source; }
                }

                public Type FilterSourceType
                {
                    get { return _source.GetType(); }
                }

                public AssemblyName FilterSourceAssemblyName
                {
                    get { return FilterSourceType.Assembly.GetName(); }
                }

                public Exception Exception
                {
                    get { return _exception; }
                }

                public Exception BaseException
                {
                    get
                    {
                        if (_baseException == null)
                            _baseException = Exception.GetBaseException();

                        return _baseException;
                    }
                }

                public bool HasHttpStatusCode
                {
                    get { return HttpStatusCode != 0; }
                }

                public int HttpStatusCode
                {
                    get
                    {
                        if (!_statusCodeInitialized)
                        {
                            _statusCodeInitialized = true;

                            var exception = Exception as HttpException;

                            if (exception != null)
                                _httpStatusCode = exception.GetHttpCode();
                        }

                        return _httpStatusCode;
                    }
                }

                public object Context
                {
                    get { return _context; }
                }
            }
        }

        public class FilterModule : Module
        {
            public FilterModule(string name) : base(name) {}

            public override ModuleConnectionHandler Initialize(object settings)
            {
                // TODO Get rid of late binding until filtering configuration has been sorted out

                var config = Elmah.Configuration.GetSubsection("errorFilter");

                if (config == null)
                    return delegate { };

                var assertion = config.GetType().GetProperty("Assertion").GetValue(config, null) /* TODO config.Assertion */;
                return Filter((Func<ErrorFilterModule.AssertionHelperContext, bool>) Delegate.CreateDelegate(typeof(Func<ErrorFilterModule.AssertionHelperContext, bool>), assertion, "Test", false, true) /* TODO assertion.Test */);
            }

            /* TODO? public[1] */ static ModuleConnectionHandler Filter(Func<ErrorFilterModule.AssertionHelperContext, bool> predicate)
            // [1] Make public once we known what to do with ErrorFilterModule.AssertionHelperContext
            {
                return ehub => ehub.OnExceptionFilter((sender, args) =>
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

        static class EventSubscriptionHelper
        {
            public static IDisposable OnException(this ExtensionHub ehub, Action<object, ExceptionEventArgs> handler)
            {
                if (ehub == null) throw new ArgumentNullException("ehub");
                if (handler == null) throw new ArgumentNullException("handler");
                return ehub.Get<ExceptionEvent>().PushHandler(next => (sender, args) => { var result = next(sender, args); handler(sender, args); return result; });
            }

            public static IDisposable OnExceptionFilter(this ExtensionHub ehub, Func<object, ExceptionFilterEventArgs, bool> handler)
            {
                if (ehub == null) throw new ArgumentNullException("ehub");
                if (handler == null) throw new ArgumentNullException("handler");
                return ehub.Get<ExceptionFilterMessage>().PushHandler(next => (sender, args) => next(sender, args) || handler(sender, args));
            }

            public static IDisposable OnErrorMailing(this ExtensionHub ehub, Action<object, ErrorMailEventArgs> handler)
            {
                if (ehub == null) throw new ArgumentNullException("ehub");
                if (handler == null) throw new ArgumentNullException("handler");
                return ehub.Get<ErrorMailEvents.Mailing>().PushHandler(next => (sender, args) => { var result = next(sender, args); handler(sender, args); return result; });
            }

            public static IDisposable OnErrorMailed(this ExtensionHub ehub, Action<object, ErrorMailEventArgs> handler)
            {
                if (ehub == null) throw new ArgumentNullException("ehub");
                if (handler == null) throw new ArgumentNullException("handler");
                return ehub.Get<ErrorMailEvents.Mailed>().PushHandler(next => (sender, args) => { var result = next(sender, args); handler(sender, args); return result; });
            }

            public static IDisposable OnErrorMailDisposing(this ExtensionHub ehub, Action<object, ErrorMailEventArgs> handler)
            {
                if (ehub == null) throw new ArgumentNullException("ehub");
                if (handler == null) throw new ArgumentNullException("handler");
                return ehub.Get<ErrorMailEvents.Disposing>().PushHandler(next => (sender, args) => { var result = next(sender, args); handler(sender, args); return result; });
            }

            public static IDisposable OnErrorInitializing(this ExtensionHub ehub, Action<object, ErrorInitializationContext> handler)
            {
                if (ehub == null) throw new ArgumentNullException("ehub");
                if (handler == null) throw new ArgumentNullException("handler");
                return ehub.Get<ErrorInitialization.Initializing>().PushHandler(next => (sender, args) => { var result = next(sender, args); handler(sender, args); return result; });
            }

            public static IDisposable OnErrorInitialized(this ExtensionHub ehub, Action<object, ErrorInitializationContext> handler)
            {
                if (ehub == null) throw new ArgumentNullException("ehub");
                if (handler == null) throw new ArgumentNullException("handler");
                return ehub.Get<ErrorInitialization.Initialized>().PushHandler(next => (sender, args) => { var result = next(sender, args); handler(sender, args); return result; });
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
            public MailModule(string name) : base(name) {}

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
                return ehub => ehub.OnException((sender, args) =>
                {
                    var exception = args.Exception;
                    if (exception == null || !ExceptionFilterMessage.Send(ehub, this, exception, args.ExceptionContext))
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

                var formatter = Activator.CreateInstance(Type.GetType("Elmah.ErrorMailHtmlFormatter, Elmah.AspNet", true)) /* TODO new ErrorMailHtmlFormatter() */; // TODO CreateErrorFormatter();

                var bodyWriter = new StringWriter();
                ((Action<TextWriter, Error>)Delegate.CreateDelegate(typeof(Action<TextWriter, Error>), formatter, "Format", false, true))/* TODO formatter.Format*/(bodyWriter, error);
                mail.Body = bodyWriter.ToString();

                var mimeType = (string) formatter.GetType().GetProperty("MimeType").GetValue(formatter, null) /* TODO formatter.MimeType */;
                switch (mimeType)
                {
                    case "text/html": mail.IsBodyHtml = true; break;
                    case "text/plain": mail.IsBodyHtml = false; break;

                    default:
                        {
                            throw new ApplicationException(String.Format(
                                "The error mail module does not know how to handle the {1} media type that is created by the {0} formatter.",
                                formatter.GetType().FullName, mimeType));
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
                        var ysodAttachment = CreateHtmlAttachment("YSOD", error.WebHostHtmlMessage);
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

            static Attachment CreateHtmlAttachment(string name, string html)
            {
                Debug.AssertStringNotEmpty(name);
                Debug.AssertStringNotEmpty(html);

                return Attachment.CreateAttachmentFromString(html,
                    name + ".html", Encoding.UTF8, "text/html");
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

    public sealed class ExceptionEvent : Event<ExceptionEventArgs>
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

            handler.Fire(sender, new ExceptionEventArgs(exception, context));
        }
    }

    public sealed class ExceptionEventArgs
    {
        public Exception Exception { get; private set; }
        public object ExceptionContext { get; private set; }

        public ExceptionEventArgs(Exception exception) : 
            this(exception, null) {}

        public ExceptionEventArgs(Exception exception, object context)
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

    public static class ErrorMailEvents
    {
        public sealed class Mailing   : Event<ErrorMailEventArgs> { }
        public sealed class Mailed    : Event<ErrorMailEventArgs> { }
        public sealed class Disposing : Event<ErrorMailEventArgs> { }
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

    public class Event<TInput> : Message<TInput, Unit>
    {
        public void Fire(TInput input)
        {
            Fire(null, input);
        }

        public void Fire(object sender, TInput input)
        {
            base.Send(sender, input);
        }
    }

    public sealed class ExtensionHub
    {
        private readonly Dictionary<Type, object> _messageByType = new Dictionary<Type, object>();

        public T Get<T>() where T : class, new()
        {
            return (Find<T>() ?? (T) (_messageByType[typeof(T)] = new T()));
        }

        public T Find<T>() where T : class
        {
            return (T) _messageByType.Find(typeof(T));
        }

        private static ICollection<InitializedModule> _modules = Array.AsReadOnly(new[]
        {
            InitializedModule.Static("ErrorUserNameInitialization", ErrorInitialization.InitUserName), 
            InitializedModule.Static("ErrorHostNameInitialization", ErrorInitialization.InitHostName), 
            InitializedModule.Static("ErrorWebCollectionsInitialization", ErrorInitialization.InitWebCollections), 
        });

        private static readonly ModuleConnectionHandler[] _zeroModules = new ModuleConnectionHandler[0];

        static ExtensionHub()
        {
            // TODO Move out of here to avoid type initialization from failing
            AppendModules(LoadModules());
        }

        public static InitializedModule[] LoadModules()
        {
            return LoadModules(null);
        }

        public static InitializedModule[] LoadModules(NameValueCollection config)
        {
            var modules = 
                from e in Elmah.Modules.Configuration.Parse(config)
                select e.LoadInitialized();
            
            return modules.ToArray();
        }

        public static ICollection<InitializedModule> Modules
        {
            get { return _modules; }
        }

        public static void AppendModules(params InitializedModule[] modules)
        {
            SetModules(modules, true);
        }

        public static void ResetModules(params InitializedModule[] modules)
        {
            SetModules(modules, false);
        }

        public static void SetModules(InitializedModule[] modules, bool append)
        {
            if (modules == null)
                return;

            while (true)
            {
                var current = _modules;
                var currentCount = append ? current.Count : 0;
                var updated = new InitializedModule[currentCount + modules.Length];
                if (append)
                    current.CopyTo(updated, 0);
                Array.Copy(modules, 0, updated, currentCount, modules.Length);
                // TODO handle duplicates
                if (current == Interlocked.CompareExchange(ref _modules, Array.AsReadOnly(updated), current))
                    break;
                Thread.SpinWait(1); // TODO Use SpinWait[1] when on .NET 4 
            }                       // [1] http://msdn.microsoft.com/en-us/library/system.threading.spinwait.aspx
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
                        module.Connect(self);
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

        public static void InitHostName(ExtensionHub ehub)
        {
            ehub.OnErrorInitializing((_, args) => InitHostName(args));
        }

        private static void InitHostName(ErrorInitializationContext args)
        {
            args.Error.HostName = Environment.MachineName;
        }

        public static void InitUserName(ExtensionHub ehub)
        {
            ehub.OnErrorInitializing((_, args) => args.Error.User = Thread.CurrentPrincipal.Identity.Name ?? String.Empty);
        }

        public static void InitWebCollections(ExtensionHub ehub)
        {
            ehub.OnErrorInitializing((_, args) => InitWebCollections(args));
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
                    var hc = ((HttpApplication) sp.GetService(typeof(HttpApplication))).Context;
                    if (hc != null)
                    {
                        var hcw = new HttpContextWrapper(((HttpApplication)sp.GetService(typeof(HttpApplication))).Context);
                        var webUser = hcw.User;
                        if (webUser != null
                            && (webUser.Identity.Name ?? String.Empty).Length > 0)
                        {
                            error.User = webUser.Identity.Name;
                        }

                        var request = hcw.Request;
                        error.ServerVariables.Add(request.ServerVariables);

                        var qsfc = request.TryGetUnvalidatedCollections((form, qs, cookies) => new
                        {
                            QueryString = qs,
                            Form = form,
                            Cookies = cookies,
                        });

                        error.QueryString.Add(qsfc.QueryString);
                        error.Form.Add(qsfc.Form);
                        error.Cookies.Add(from i in Enumerable.Range(0, qsfc.Cookies.Count)
                                          select qsfc.Cookies[i] into cookie
                                          select cookie.Name.AsKeyTo(cookie.Value));
                    }
                }
            }
        }
    }
}
