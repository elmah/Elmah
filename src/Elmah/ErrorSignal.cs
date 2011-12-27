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

[assembly: Elmah.Scc("$Id: ErrorSignal.cs 633 2009-05-30 01:58:12Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Web;
    using Mannex.Collections.Generic;

    #endregion

    public sealed class ErrorSignal
    {
        public event ErrorSignalEventHandler Raised;

        private static Dictionary<HttpApplication, ErrorSignal> _signalByApp;
        private static readonly object _lock = new object();

        public void Raise(Exception e)
        {
            Raise(e, null);
        }

        public void Raise(Exception e, HttpContextBase context)
        {
            if (context == null)
                context = new HttpContextWrapper(HttpContext.Current);

            ErrorSignalEventHandler handler = Raised;

            if (handler != null)
                handler(this, new ErrorSignalEventArgs(e, context));
        }

        public static ErrorSignal FromCurrentContext()
        {
            return FromContext(new HttpContextWrapper(HttpContext.Current));
        }

        [Obsolete("Use the FromContext(HttpContextBase) overload instead.")]
        public static ErrorSignal FromContext(HttpContext context)
        {
            return FromContext(new HttpContextWrapper(context));
        }

        public static ErrorSignal FromContext(HttpContextBase context)
        {
            if (context == null) 
                throw new ArgumentNullException("context");

            return Get(context.ApplicationInstance);
        }

        public static ErrorSignal Get(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");

            lock (_lock)
            {
                //
                // Allocate map of object per application on demand.
                //

                if (_signalByApp == null)
                    _signalByApp = new Dictionary<HttpApplication, ErrorSignal>();

                //
                // Get the list of modules fot the application. If this is
                // the first registration for the supplied application object
                // then setup a new and empty list.
                //

                ErrorSignal signal = _signalByApp.Find(application);

                if (signal == null)
                {
                    signal = new ErrorSignal();
                    _signalByApp.Add(application, signal);
                    application.Disposed += new EventHandler(OnApplicationDisposed);
                }

                return signal;
            }
        }

        private static void OnApplicationDisposed(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication) sender;

            lock (_lock)
            {
                if (_signalByApp == null)
                    return;

                _signalByApp.Remove(application);
                
                if (_signalByApp.Count == 0)
                    _signalByApp = null;
            }
        }
    }

    public delegate void ErrorSignalEventHandler(object sender, ErrorSignalEventArgs args);

    [ Serializable ]
    public sealed class ErrorSignalEventArgs : EventArgs
    {
        private readonly Exception _exception;
        [ NonSerialized ]
        private readonly HttpContextBase _context;

        public ErrorSignalEventArgs(Exception e, HttpContextBase context)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            _exception = e;
            _context = context;
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public HttpContextBase Context
        {
            get { return _context; }
        }

        public override string ToString()
        {
            return Mask.EmptyString(Exception.Message, Exception.GetType().FullName);
        }
    }
}