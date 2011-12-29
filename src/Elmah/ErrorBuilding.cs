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
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Web;
    using System.Xml;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;
    using IDictionary = System.Collections.IDictionary;

    #endregion

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
        public event EventFiringHandler<T> Fired;

        public virtual void Invoke(object sender, EventFiringEventArgs<T> args)
        {
            if (args.IsHandled) // Rare but possible
                return;
            var handler = Fired;
            if (handler == null) 
                return;
            Invoke(handler.GetInvocationList(), sender, args);
        }

        private static void Invoke(IEnumerable<Delegate> handlers, object sender, EventFiringEventArgs<T> args)
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
            var config = (IDictionary) Configuration.GetSubsection("errorInitializers");
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
            if (handler != null) handler.Invoke(/* TODO sender */ null, new EventFiringEventArgs<ErrorInitializationContext>(args));
        }

        private static void OnErrorInitialized(EventStation extensions, ErrorInitializationContext args)
        {
            Debug.Assert(args != null);
            var handler = extensions.Find<Initialized>();
            if (handler != null) handler.Invoke(/* TODO sender */ null, new EventFiringEventArgs<ErrorInitializationContext>(args));
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
            container.Get<Initializing>().Fired += (_, args) => InitHostName(args.Payload);
        }

        private static void InitHostName(ErrorInitializationContext args)
        {
            var context = args.Context as HttpContext;
            args.Error.HostName = Environment.TryGetMachineName(context != null ? new HttpContextWrapper(context) : null);
        }

        public static void InitUserName(EventStation container)
        {
            container.Get<Initializing>().Fired += (_, args) =>
            {
                args.Payload.Error.User = Thread.CurrentPrincipal.Identity.Name ?? String.Empty;
            };
        }

        public static void InitWebCollections(EventStation container)
        {
            container.Get<Initializing>().Fired += (_, args) => InitWebCollections(args.Payload);
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
