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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Web;
    using System.Xml;
    using Mannex.Collections.Generic;
    using Mannex.Collections.Specialized;

    #endregion

    public class ExtensionInvocationEventArgs : EventArgs
    {
        private readonly object _payload;

        public ExtensionInvocationEventArgs() : 
            this(null) {}

        public ExtensionInvocationEventArgs(object payload)
        {
            _payload = payload;
        }

        public bool IsHandled { get; set; }
        public object Payload { get { return _payload; } }
        public object Result { get; set; }
    }

    public delegate void ExtensionInvocationEventHandler(object sender, ExtensionInvocationEventArgs args);

    public class Extension
    {
        public event ExtensionInvocationEventHandler Invoked;

        public virtual void Invoke(object sender, ExtensionInvocationEventArgs args)
        {
            if (args.IsHandled) // Rare but possible
                return;
            var handler = Invoked;
            if (handler == null) 
                return;
            Invoke(handler.GetInvocationList(), sender, args);
        }

        private static void Invoke(IEnumerable<Delegate> handlers, object sender, ExtensionInvocationEventArgs args)
        {
            Debug.Assert(handlers != null);
            Debug.Assert(args != null);
            Debug.Assert(!args.IsHandled);

            foreach (ExtensionInvocationEventHandler handler in handlers)
            {
                handler(sender, args);
                if (args.IsHandled)
                    return;
            }
        }
    }

    [ Serializable ]
    public sealed class ExtensionClass
    {
        private readonly string _name;

        public ExtensionClass() : 
            this(null) {}

        public ExtensionClass(string name)
        {
            _name = !string.IsNullOrEmpty(name) 
                  ? name 
                  : Guid.NewGuid().ToString();
        }

        public string Name { get { return _name; } }

        public bool Equals(ExtensionClass other)
        {
            if (other == null) return false;
            return other == this || 0 == string.CompareOrdinal(other.Name, Name);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ExtensionClass);
        }

        public override int GetHashCode() { return Name.GetHashCode(); }

        public static bool operator ==(ExtensionClass left, ExtensionClass right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ExtensionClass left, ExtensionClass right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public delegate void ExtensionConnectionHandler(ExtensionContainer container);
    public delegate ExtensionConnectionHandler ExtensionSetupHandler(NameValueCollection settings);

    public sealed class ExtensionContainer
    {
        private readonly Hashtable _extensions = new Hashtable();

        public Extension this[object key]
        {
            get
            {
                return (Extension) _extensions[key];
            }
        }

        private static object _customizations = ArrayList.ReadOnly(new ExtensionConnectionHandler[]
        {
            InitUserName, 
            InitHostName, 
            InitWebCollections, 
        });

        private static readonly ExtensionConnectionHandler[] _zeroCustomizations = new ExtensionConnectionHandler[0];

        static ExtensionContainer()
        {
            AppendCustomizations(LoadCustomizations());
        }

        public static ExtensionConnectionHandler[] LoadCustomizations()
        {
            var config = (IDictionary) Configuration.GetSubsection("errorInitializers");
            return config != null ? LoadCustomizations(config) : _zeroCustomizations;
        }

        public static ExtensionConnectionHandler[] LoadCustomizations(IDictionary config)
        {
            if (config == null) throw new ArgumentNullException("config");

            var customizations = new List<ExtensionConnectionHandler>(config.Count);

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

        public static ICollection Customizations
        {
            get { return (ICollection)_customizations; }
        }

        public static void AppendCustomizations(params ExtensionConnectionHandler[] customizations)
        {
            SetCustomizations(customizations, true);
        }

        public static void ResetCustomizations(params ExtensionConnectionHandler[] customizations)
        {
            SetCustomizations(customizations, false);
        }

        public static void SetCustomizations(ExtensionConnectionHandler[] customizations, bool append)
        {
            if (customizations == null)
                return;

            ICollection current;
            ExtensionConnectionHandler[] updated;
            do
            {
                current = (ICollection)_customizations;
                int currentCount = append ? current.Count : 0;
                updated = new ExtensionConnectionHandler[currentCount + customizations.Length];
                if (append)
                    current.CopyTo(updated, 0);
                Array.Copy(customizations, 0, updated, currentCount, customizations.Length);
                // TODO handle duplicates
            }
            while (current != Interlocked.CompareExchange(ref _customizations, ArrayList.ReadOnly(updated), current));
        }

        [ThreadStatic] static ExtensionContainer _thread;
        [ThreadStatic] static object _dependency;

        public static ExtensionContainer Default
        {
            get
            {
                var customizations = Customizations;
                var self = _dependency == customizations
                         ? _thread : null;
                if (self == null)
                {
                    self = new ExtensionContainer();
                    _dependency = customizations;
                    foreach (ExtensionConnectionHandler customization in customizations)
                        customization(self);
                    _thread = self;
                }
                return self;
            }
        }

        private void OnErrorInitializing(ErrorInitializationEventArgs args)
        {
            Debug.Assert(args != null);
            var handler = this["ErrorInitializing"];
            if (handler != null) handler.Invoke(this, new ExtensionInvocationEventArgs(args));
        }

        private void OnErrorInitialized(ErrorInitializationEventArgs args)
        {
            Debug.Assert(args != null);
            var handler = this["ErrorInitialized"];
            if (handler != null) handler.Invoke(this, new ExtensionInvocationEventArgs(args));
        }

        public void Initialize(Error error, object context)
        {
            OnErrorInitializing(new ErrorInitializationEventArgs(error, context));
            OnErrorInitialized(new ErrorInitializationEventArgs(error, context));
        }

        internal static void OnErrorNopInit(object sender, ErrorInitializationEventArgs e) { /* NOP */ }

        public static ExtensionConnectionHandler InitHostName(NameValueCollection settings)
        {
            return InitHostName;
        }

        public static void InitHostName(ExtensionContainer container)
        {
            container["ErrorInitializing"].Invoked += (_, args) => InitHostName((ErrorInitializationEventArgs) args.Payload);
        }

        private static void InitHostName(ErrorInitializationEventArgs args)
        {
            var context = args.Context as HttpContext;
            args.Error.HostName = Environment.TryGetMachineName(context != null ? new HttpContextWrapper(context) : null);
        }

        public static void InitUserName(ExtensionContainer container)
        {
            container["ErrorInitializing"].Invoked += (_, args) => 
            {
                ((ErrorInitializationEventArgs) args.Payload).Error.User = Thread.CurrentPrincipal.Identity.Name ?? string.Empty;
            };
        }

        public static void InitWebCollections(ExtensionContainer container)
        {
            container["ErrorInitializing"].Invoked += (_, args) => InitWebCollections((ErrorInitializationEventArgs) args.Payload);
        }

        private static void InitWebCollections(ErrorInitializationEventArgs args)
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
                error.WebHostHtmlMessage = Error.TryGetHtmlErrorMessage(httpException) ?? string.Empty;
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
                            && (webUser.Identity.Name ?? string.Empty).Length > 0)
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

    public class ErrorInitializationEventArgs : EventArgs
    {
        private readonly Error _error;
        private readonly object _context;

        public ErrorInitializationEventArgs(Error error) : 
            this(error, null) {}

        public ErrorInitializationEventArgs(Error error, object context)
        {
            if (error == null) throw new ArgumentNullException("error");
            _error = error;
            _context = context;
        }

        public Error Error { get { return _error; } }
        public object Context { get { return _context; } }
    }
}
