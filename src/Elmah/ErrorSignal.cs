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
    using System.Runtime.CompilerServices; // caller info attributes
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
            Raise(e, context, null);
        }

        public void Raise(Exception e, HttpContextBase context, CallerInfo callerInfo)
        {
            if (context == null)
                context = new HttpContextWrapper(HttpContext.Current);

            var handler = Raised;

            if (handler != null)
                handler(this, new ErrorSignalEventArgs(e, context, callerInfo));
        }

        public void RaiseWithCallerInfo(Exception e,
            [CallerMemberName] string callerMember = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            Raise(e, null, new CallerInfo(callerMember, callerFilePath, callerLineNumber));
        }

        public void RaiseWithCallerInfo(Exception e, HttpContextBase context,
            [CallerMemberName] string callerMember = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            Raise(e, context, new CallerInfo(callerMember, callerFilePath, callerLineNumber));
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

                var signal = _signalByApp.Find(application);

                if (signal == null)
                {
                    signal = new ErrorSignal();
                    _signalByApp.Add(application, signal);
                    application.Disposed += OnApplicationDisposed;
                }

                return signal;
            }
        }

        private static void OnApplicationDisposed(object sender, EventArgs e)
        {
            var application = (HttpApplication) sender;

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

        public ErrorSignalEventArgs(Exception e, HttpContextBase context) : 
            this(e, context, null) {}

        public ErrorSignalEventArgs(Exception e, HttpContextBase context, CallerInfo callerInfo)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            _exception = e;
            _context = context;
            CallerInfo = callerInfo ?? CallerInfo.Empty;
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public HttpContextBase Context
        {
            get { return _context; }
        }

        public CallerInfo CallerInfo { get; private set; }

        public override string ToString()
        {
            return Mask.EmptyString(Exception.Message, Exception.GetType().FullName);
        }
    }
}