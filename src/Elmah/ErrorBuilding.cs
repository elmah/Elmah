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
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Reflection;
    using System.Security.Principal;
    using System.Threading;
    using System.Web;
    using System.Xml;

    #endregion

    public class ExtensionInvocationEventArgs : EventArgs
    {
        private bool _isHandled;
        private readonly object _payload;
        private object _result;

        public ExtensionInvocationEventArgs() : 
            this(null) {}

        public ExtensionInvocationEventArgs(object payload)
        {
            _payload = payload;
        }

        public bool IsHandled
        {
            get { return _isHandled; }
            set { _isHandled = value; }
        }

        public object Payload { get { return _payload; } }

        public object Result
        {
            get { return _result; }
            set { _result = value; }
        }
    }

    public delegate void ExtensionInvocationEventHandler(object sender, ExtensionInvocationEventArgs args);

    public class Extension
    {
        public event ExtensionInvocationEventHandler Invoked;

        public virtual void Invoke(object sender, ExtensionInvocationEventArgs args)
        {
            if (args.IsHandled) // Rare but possible
                return;
            ExtensionInvocationEventHandler handler = Invoked;
            if (handler == null) 
                return;
            Invoke(handler.GetInvocationList(), sender, args);
        }

        private static void Invoke(Delegate[] handlers, object sender, ExtensionInvocationEventArgs args)
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
            _name = name != null && name.Length > 0 
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
            new ExtensionConnectionHandler(InitUserName), 
            new ExtensionConnectionHandler(InitHostName), 
            new ExtensionConnectionHandler(InitWebCollections), 
        });

        private static readonly ExtensionConnectionHandler[] _zeroCustomizations = new ExtensionConnectionHandler[0];

        static ExtensionContainer()
        {
            AppendCustomizations(LoadCustomizations());
        }

        public static ExtensionConnectionHandler[] LoadCustomizations()
        {
            IDictionary config = (IDictionary)Configuration.GetSubsection("errorInitializers");
            return config != null ? LoadCustomizations(config) : _zeroCustomizations;
        }

        public static ExtensionConnectionHandler[] LoadCustomizations(IDictionary config)
        {
            if (config == null) throw new ArgumentNullException("config");

            ArrayList customizations = new ArrayList(config.Count);

            IDictionaryEnumerator e = config.GetEnumerator();
            while (e.MoveNext())
            {
                XmlQualifiedName xqn = (XmlQualifiedName)e.Key;

                if (0 == string.CompareOrdinal(xqn.Namespace, "elmah"))
                    continue;

                string assemblyName, ns;

                if (!Assertions.AssertionFactory.DecodeClrTypeNamespaceFromXmlNamespace(xqn.Namespace, out ns, out assemblyName) ||
                    ns.Length > 0)
                {
                    throw new Exception(string.Format("Error decoding CLR type namespace and assembly from the XML namespace '{0}'.", xqn.Namespace));
                }

                Assembly assembly = Assembly.Load(assemblyName);
                Type type = assembly.GetType(ns + ".ErrorInitialization", /* throwOnError */ true);
                ExtensionSetupHandler handler = (ExtensionSetupHandler)Delegate.CreateDelegate(typeof(ExtensionSetupHandler), type, xqn.Name, true, /* throwOnBindFailure */ false);
                // TODO Null handler handling
                NameValueCollection settings = (NameValueCollection)e.Value;
                customizations.Add(handler(settings));
            }

            return (ExtensionConnectionHandler[])customizations.ToArray(typeof(ExtensionConnectionHandler));
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
                ICollection customizations = Customizations;
                ExtensionContainer self = _dependency == customizations
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
            Extension handler = this["ErrorInitializing"];
            if (handler != null) handler.Invoke(this, new ExtensionInvocationEventArgs(args));
        }

        private void OnErrorInitialized(ErrorInitializationEventArgs args)
        {
            Debug.Assert(args != null);
            Extension handler = this["ErrorInitialized"];
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
            return new ExtensionConnectionHandler(InitHostName);
        }

        public static void InitHostName(ExtensionContainer container)
        {
            container["ErrorInitializing"].Invoked += new ExtensionInvocationEventHandler(InitHostName);
        }

        private static void InitHostName(object sender, ExtensionInvocationEventArgs args)
        {
            InitHostName((ErrorInitializationEventArgs) args.Payload);
        }

        private static void InitHostName(ErrorInitializationEventArgs args)
        {
            var context = args.Context as HttpContext;
            args.Error.HostName = Environment.TryGetMachineName(context != null ? new HttpContextWrapper(context) : null);
        }

        public static void InitUserName(ExtensionContainer container)
        {
            container["ErrorInitializing"].Invoked += new ExtensionInvocationEventHandler(InitUserName);
        }

        private static void InitUserName(object sender, ExtensionInvocationEventArgs args)
        {
            ((ErrorInitializationEventArgs) args.Payload).Error.User = Thread.CurrentPrincipal.Identity.Name ?? string.Empty;
        }

        public static void InitWebCollections(ExtensionContainer container)
        {
            container["ErrorInitializing"].Invoked += new ExtensionInvocationEventHandler(InitWebCollections);
        }

        public static void InitWebCollections(object sender, ExtensionInvocationEventArgs args)
        {
            InitWebCollections((ErrorInitializationEventArgs) args.Payload);
        }

        private static void InitWebCollections(ErrorInitializationEventArgs args)
        {
            Error error = args.Error;
            Exception e = error.Exception;

            //
            // If this is an HTTP exception, then get the status code
            // and detailed HTML message provided by the host.
            //

            HttpException httpException = e as HttpException;

            if (httpException != null)
            {
                error.StatusCode = httpException.GetHttpCode();
                error.WebHostHtmlMessage = httpException.GetHtmlErrorMessage() ?? string.Empty;
            }

            //
            // If the HTTP context is available, then capture the
            // collections that represent the state request.
            //

            if (args.Context != null)
            {
                IServiceProvider sp = args.Context as IServiceProvider;
                if (sp != null)
                {
                    HttpContext hc = ((HttpApplication)sp.GetService(typeof(HttpApplication))).Context;
                    if (hc != null)
                    {
                        IPrincipal webUser = hc.User;
                        if (webUser != null
                            && (webUser.Identity.Name ?? string.Empty).Length > 0)
                        {
                            error.User = webUser.Identity.Name;
                        }

                        HttpRequest request = hc.Request;

                        error.ServerVariables.Add(request.ServerVariables);
                        error.QueryString.Add(request.QueryString);
                        error.Form.Add(request.Form);
                        error.Cookies.Add(CopyCollection(request.Cookies));
                    }
                }
            }
        }

        private static NameValueCollection CopyCollection(HttpCookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return null;

            NameValueCollection copy = new NameValueCollection(cookies.Count);

            for (int i = 0; i < cookies.Count; i++)
            {
                HttpCookie cookie = cookies[i];

                //
                // NOTE: We drop the Path and Domain properties of the 
                // cookie for sake of simplicity.
                //

                copy.Add(cookie.Name, cookie.Value);
            }

            return copy;
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

    public delegate void ErrorInitializationEventHandler(object sender, ErrorInitializationEventArgs args);
    public delegate void ErrorInitializationSetupHandler(ErrorInitialization initialization);

    internal delegate ErrorInitializationSetupHandler ErrorInitializationSetupHandlerFactory(NameValueCollection settings);

    public sealed class ErrorInitialization
    {
        public event ErrorInitializationEventHandler ErrorInitializing = new ErrorInitializationEventHandler(OnErrorNopInit);
        public event ErrorInitializationEventHandler ErrorInitialized = new ErrorInitializationEventHandler(OnErrorNopInit);

        private static object _customizations = ArrayList.ReadOnly(new ErrorInitializationSetupHandler[]
        {
            new ErrorInitializationSetupHandler(InitUserName), 
            new ErrorInitializationSetupHandler(InitHostName), 
            new ErrorInitializationSetupHandler(InitWebCollections), 
        });

        private static readonly ErrorInitializationSetupHandler[] _zeroCustomizations = new ErrorInitializationSetupHandler[0];

        static ErrorInitialization()
        {
            AppendCustomizations(LoadCustomizations());
        }

        public static ErrorInitializationSetupHandler[] LoadCustomizations()
        {
            IDictionary config = (IDictionary) Configuration.GetSubsection("errorInitializers");
            return config != null ? LoadCustomizations(config) : _zeroCustomizations;
        }

        public static ErrorInitializationSetupHandler[] LoadCustomizations(IDictionary config)
        {
            if (config == null) throw new ArgumentNullException("config");

            ArrayList customizations = new ArrayList(config.Count);

            IDictionaryEnumerator e = config.GetEnumerator();
            while (e.MoveNext())
            {
                XmlQualifiedName xqn = (XmlQualifiedName) e.Key;

                if (0 == string.CompareOrdinal(xqn.Namespace, "elmah"))
                    continue;

                string assemblyName, ns;

                if (!Assertions.AssertionFactory.DecodeClrTypeNamespaceFromXmlNamespace(xqn.Namespace, out ns, out assemblyName) ||
                    ns.Length > 0)
                {
                    throw new Exception(string.Format("Error decoding CLR type namespace and assembly from the XML namespace '{0}'.", xqn.Namespace));
                }

                Assembly assembly = Assembly.Load(assemblyName);
                Type type = assembly.GetType(ns + ".ErrorInitialization", /* throwOnError */ true);
                ErrorInitializationSetupHandlerFactory handler = (ErrorInitializationSetupHandlerFactory)Delegate.CreateDelegate(typeof(ErrorInitializationSetupHandlerFactory), type, xqn.Name, true, /* throwOnBindFailure */ false);
                // TODO Null handler handling
                NameValueCollection settings = (NameValueCollection) e.Value;
                customizations.Add(handler(settings));
            }

            return (ErrorInitializationSetupHandler[]) customizations.ToArray(typeof(ErrorInitializationSetupHandler));
        }

        public static ICollection Customizations
        {
            get { return (ICollection) _customizations; }
        }

        public static void AppendCustomizations(params ErrorInitializationSetupHandler[] customizations)
        {
            SetCustomizations(customizations, true);
        }

        public static void ResetCustomizations(params ErrorInitializationSetupHandler[] customizations)
        {
            SetCustomizations(customizations, false);
        }

        public static void SetCustomizations(ErrorInitializationSetupHandler[] customizations, bool append)
        {
            if (customizations == null)
                return;

            ICollection current;
            ErrorInitializationSetupHandler[] updated;
            do
            {
                current = (ICollection) _customizations;
                int currentCount = append ? current.Count : 0;
                updated = new ErrorInitializationSetupHandler[currentCount + customizations.Length];
                if (append)
                    current.CopyTo(updated, 0);
                Array.Copy(customizations, 0, updated, currentCount, customizations.Length);
                // TODO handle duplicates
            }
            while (current != Interlocked.CompareExchange(ref _customizations, ArrayList.ReadOnly(updated), current));
        }

        [ThreadStatic] static ErrorInitialization _thread;
        [ThreadStatic] static object _dependency;

        public static ErrorInitialization Default
        {
            get
            {
                ICollection customizations = Customizations;
                ErrorInitialization self = _dependency == customizations
                                         ? _thread : null;
                if (self == null)
                {
                    self = new ErrorInitialization();
                    _dependency = customizations;
                    foreach (ErrorInitializationSetupHandler customization in customizations)
                        customization(self);
                    _thread = self;
                }
                return self;
            }
        }

        private void OnErrorInitializing(ErrorInitializationEventArgs args)
        {
            Debug.Assert(args != null);
            ErrorInitializationEventHandler handler = ErrorInitializing;
            if (handler != null) handler(this, args);
        }

        private void OnErrorInitialized(ErrorInitializationEventArgs args)
        {
            Debug.Assert(args != null);
            ErrorInitializationEventHandler handler = ErrorInitialized;
            if (handler != null) handler(this, args);
        }

        public void Initialize(Error error, object context)
        {
            OnErrorInitializing(new ErrorInitializationEventArgs(error, context));
            OnErrorInitialized(new ErrorInitializationEventArgs(error, context));
        }

        internal static void OnErrorNopInit(object sender, ErrorInitializationEventArgs e) { /* NOP */ }

        public static ErrorInitializationSetupHandler InitHostName(NameValueCollection settings)
        {
            return new ErrorInitializationSetupHandler(InitHostName);
        }

        public static void InitHostName(ErrorInitialization initialization)
        {
            initialization.ErrorInitializing += new ErrorInitializationEventHandler(InitHostName);
        }

        private static void InitHostName(object sender, ErrorInitializationEventArgs args)
        {
            var context = args.Context as HttpContext;
            args.Error.HostName = Environment.TryGetMachineName(context != null ? new HttpContextWrapper(context) : null);
        }

        public static void InitUserName(ErrorInitialization initialization)
        {
            initialization.ErrorInitializing += new ErrorInitializationEventHandler(InitUserName);
        }

        private static void InitUserName(object sender, ErrorInitializationEventArgs args)
        {
            args.Error.User = Thread.CurrentPrincipal.Identity.Name ?? string.Empty;
        }

        public static void InitWebCollections(ErrorInitialization initialization)
        {
            initialization.ErrorInitializing += new ErrorInitializationEventHandler(InitWebCollections);
        }

        public static void InitWebCollections(object sender, ErrorInitializationEventArgs args)
        {
            Error error = args.Error;
            Exception e = error.Exception;

            //
            // If this is an HTTP exception, then get the status code
            // and detailed HTML message provided by the host.
            //

            HttpException httpException = e as HttpException;

            if (httpException != null)
            {
                error.StatusCode = httpException.GetHttpCode();
                error.WebHostHtmlMessage = httpException.GetHtmlErrorMessage() ?? string.Empty;
            }

            //
            // If the HTTP context is available, then capture the
            // collections that represent the state request.
            //

            if (args.Context != null)
            {
                IServiceProvider sp = args.Context as IServiceProvider;
                if (sp != null)
                {
                    HttpContext hc = ((HttpApplication) sp.GetService(typeof(HttpApplication))).Context;
                    if (hc != null)
                    {
                        IPrincipal webUser = hc.User;
                        if (webUser != null
                            && (webUser.Identity.Name ?? string.Empty).Length > 0)
                        {
                            error.User = webUser.Identity.Name;
                        }

                        HttpRequest request = hc.Request;

                        error.ServerVariables.Add(request.ServerVariables);
                        error.QueryString.Add(request.QueryString);
                        error.Form.Add(request.Form);
                        error.Cookies.Add(CopyCollection(request.Cookies));
                    }
                }
            }
        }

        private static NameValueCollection CopyCollection(HttpCookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return null;

            NameValueCollection copy = new NameValueCollection(cookies.Count);

            for (int i = 0; i < cookies.Count; i++)
            {
                HttpCookie cookie = cookies[i];

                //
                // NOTE: We drop the Path and Domain properties of the 
                // cookie for sake of simplicity.
                //

                copy.Add(cookie.Name, cookie.Value);
            }

            return copy;
        }
    }

    sealed class ErrorInitializationSectionHandler : DictionarySectionHandler
    {
        /*
        public override object Create(object parent, object configContext, XmlNode section)
        {
            IDictionary dictionary;
            
            if (parent != null)
            {
                dictionary = CreateDictionary(null);
                foreach (NameValueCollection item in (IEnumerable)parent)
                    dictionary.Add(CombinedKey(item["type"], item["method"]), item);
                parent = dictionary;
            }

            dictionary = (IDictionary) base.Create(parent, configContext, section);

            NameValueCollection[] collections = new NameValueCollection[dictionary.Count];
            dictionary.Values.CopyTo(collections, 0);
            return ArrayList.ReadOnly(collections);
        }

        protected override object GetKey(XmlNode node)
        {
            return CombinedKey(GetKey(node, "type"), GetKey(node, "method"));
        }

        private static string CombinedKey(string key1, string key2)
        {
            return key1 + "::" + key2;
        }

        private static string GetKey(XmlNode node, string keyName)
        {
            XmlAttribute keyAttribute = node.Attributes[keyName];
            string key = keyAttribute == null ? null : keyAttribute.Value;
            if (key == null || key.Length == 0)
                throw new ConfigurationException("Missing entry key.", node);
            return key;
        }

        protected override object GetValue(XmlNode node)
        {
            NameValueCollection collection = null;

            foreach (XmlAttribute attribute in node.Attributes)
            {
                if (attribute.NamespaceURI.Length > 0
                    || 0 == string.CompareOrdinal(attribute.Name, "type")
                    || 0 == string.CompareOrdinal(attribute.Name, "method"))
                {
                    continue;
                }

                if (collection == null)
                    collection = new NameValueCollection();
                collection.Add(attribute.Name, attribute.Value);
            }

            return collection != null
                 ? collection.Freeze()
                 : NameValueCollection.Empty;
        }*/

        private static readonly char[] _separator = new char[] { ':' };

        protected override IDictionary CreateDictionary(object parent)
        {
            return base.CreateDictionary(parent != null ? parent : CreateDefaultDictionary());
        }

        private IDictionary CreateDefaultDictionary()
        {
            IDictionary dictionary = base.CreateDictionary(null);
            foreach (ErrorInitializationSetupHandler handler in ErrorInitialization.Customizations)
                dictionary.Add(new XmlQualifiedName(handler.Method.Name), "elmah");
            return dictionary;
        }

        protected override object GetKey(XmlNode node)
        {
            string key = (string) base.GetKey(node);
            string[] pair = key.Split(_separator, 2);
            string prefix = pair.Length > 1 ? pair[0] : string.Empty;
            string localName = pair[pair.Length > 1 ? 1 : 0];
            string ns = prefix.Length > 0 ? node.GetNamespaceOfPrefix(prefix) : null;
            return new XmlQualifiedName(localName, ns);
        }

        protected override object GetValue(XmlNode node)
        {
            NameValueCollection collection = null;

            foreach (XmlAttribute attribute in node.Attributes)
            {
                if (attribute.NamespaceURI.Length > 0
                    || 0 == string.CompareOrdinal(attribute.Name, "key"))
                {
                    continue;
                }

                if (collection == null)
                    collection = new NameValueCollection();
                collection.Add(attribute.Name, attribute.Value);
            }

            return collection != null
                 ? collection.Freeze()
                 : NameValueCollection.Empty;
        }

        sealed class NameValueCollection : System.Collections.Specialized.NameValueCollection
        {
            public static readonly NameValueCollection Empty;

            static NameValueCollection()
            {
                Empty = new NameValueCollection();
                Empty.IsReadOnly = true;
            }

            public new bool IsReadOnly
            {
                get { return base.IsReadOnly; }
                set { base.IsReadOnly = value; }
            }

            public NameValueCollection Freeze()
            {
                IsReadOnly = true;
                return this;
            }
        }
    }
}
