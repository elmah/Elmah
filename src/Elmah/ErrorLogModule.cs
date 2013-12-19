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

[assembly: Elmah.Scc("$Id: ErrorLogModule.cs 923 2011-12-23 22:02:10Z azizatif $")]

namespace Elmah
{
    #region Imports
    
    using System;
    using System.Web;
    
    using Trace = System.Diagnostics.Trace;

    #endregion

    /// <summary>
    /// HTTP module implementation that logs unhandled exceptions in an
    /// ASP.NET Web application to an error log.
    /// </summary>
    
    public class ErrorLogModule : HttpModuleBase, IExceptionFiltering
    {
        public event ExceptionFilterEventHandler Filtering;
        public event ErrorLoggedEventHandler Logged;

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>
        
        protected override void OnInit(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");
            
            application.Error += OnError;
            ErrorSignal.Get(application).Raised += OnErrorSignaled;
        }

        /// <summary>
        /// Gets the <see cref="ErrorLog"/> instance to which the module
        /// will log exceptions.
        /// </summary>
        
        protected virtual ErrorLog GetErrorLog(HttpContextBase context)
        {
            return ErrorLog.GetDefault(context);
        }

        /// <summary>
        /// The handler called when an unhandled exception bubbles up to 
        /// the module.
        /// </summary>

        protected virtual void OnError(object sender, EventArgs args)
        {
            var application = (HttpApplication) sender;
            LogException(application.Server.GetLastError(), new HttpContextWrapper(application.Context));
        }

        /// <summary>
        /// The handler called when an exception is explicitly signaled.
        /// </summary>

        protected virtual void OnErrorSignaled(object sender, ErrorSignalEventArgs args)
        {
            using (args.Exception.TryScopeCallerInfo(args.CallerInfo))
                LogException(args.Exception, args.Context);
        }

        /// <summary>
        /// Logs an exception and its context to the error log.
        /// </summary>

        protected virtual void LogException(Exception e, HttpContextBase context)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            //
            // Fire an event to check if listeners want to filter out
            // logging of the uncaught exception.
            //

            var args = new ExceptionFilterEventArgs(e, context);
            OnFiltering(args);
            
            if (args.Dismissed)
                return;
            
            //
            // Log away...
            //

            ErrorLogEntry entry = null;

            try
            {
                var error = new Error(e, context);
                var log = GetErrorLog(context);
                error.ApplicationName = log.ApplicationName;
                var id = log.Log(error);
                entry = new ErrorLogEntry(log, id, error);
            }
            catch (Exception localException)
            {
                //
                // IMPORTANT! We swallow any exception raised during the 
                // logging and send them out to the trace . The idea 
                // here is that logging of exceptions by itself should not 
                // be  critical to the overall operation of the application.
                // The bad thing is that we catch ANY kind of exception, 
                // even system ones and potentially let them slip by.
                //

                Trace.WriteLine(localException);
            }

            if (entry != null)
                OnLogged(new ErrorLoggedEventArgs(entry));
        }

        /// <summary>
        /// Raises the <see cref="Logged"/> event.
        /// </summary>

        protected virtual void OnLogged(ErrorLoggedEventArgs args)
        {
            var handler = Logged;

            if (handler != null)
                handler(this, args);
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
        /// Determines whether the module will be registered for discovery
        /// in partial trust environments or not.
        /// </summary>

        protected override bool SupportDiscoverability
        {
            get { return true; }
        }
    }

    public delegate void ErrorLoggedEventHandler(object sender, ErrorLoggedEventArgs args);

    [ Serializable ]
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
}
