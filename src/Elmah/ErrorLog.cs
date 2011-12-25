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

[assembly: Elmah.Scc("$Id: ErrorLog.cs 776 2011-01-12 21:09:24Z azizatif $")]

namespace Elmah
{
    #region Imports
    
    using System;
    using System.Web;
    using System.Collections.Generic;

    #endregion

    /// <summary>
    /// Represents an error log capable of storing and retrieving errors
    /// generated in an ASP.NET Web application.
    /// </summary>

    public abstract class ErrorLog
    {
        private string _appName;
        private bool _appNameInitialized;
        private static readonly object _contextKey = new object();

        /// <summary>
        /// Logs an error in log for the application.
        /// </summary>
        
        public abstract string Log(Error error);

        /// <summary>
        /// When overridden in a subclass, begins an asynchronous version 
        /// of <see cref="Log"/>.
        /// </summary>

        public virtual IAsyncResult BeginLog(Error error, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginSyncImpl(asyncCallback, asyncState, new LogHandler(Log), error);
        }

        /// <summary>
        /// When overridden in a subclass, ends an asynchronous version 
        /// of <see cref="Log"/>.
        /// </summary>

        public virtual string EndLog(IAsyncResult asyncResult)
        {
            return (string) EndSyncImpl(asyncResult);
        }

        private delegate string LogHandler(Error error);

        /// <summary>
        /// Retrieves a single application error from log given its 
        /// identifier, or null if it does not exist.
        /// </summary>

        public abstract ErrorLogEntry GetError(string id);

        /// <summary>
        /// When overridden in a subclass, begins an asynchronous version 
        /// of <see cref="GetError"/>.
        /// </summary>

        public virtual IAsyncResult BeginGetError(string id, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginSyncImpl(asyncCallback, asyncState, new GetErrorHandler(GetError), id);
        }

        /// <summary>
        /// When overridden in a subclass, ends an asynchronous version 
        /// of <see cref="GetError"/>.
        /// </summary>

        public virtual ErrorLogEntry EndGetError(IAsyncResult asyncResult)
        {
            return (ErrorLogEntry) EndSyncImpl(asyncResult);
        }

        private delegate ErrorLogEntry GetErrorHandler(string id);

        /// <summary>
        /// Retrieves a page of application errors from the log in 
        /// descending order of logged time.
        /// </summary>

        public abstract int GetErrors(int pageIndex, int pageSize, IList<ErrorLogEntry> errorEntryList);

        /// <summary>
        /// When overridden in a subclass, begins an asynchronous version 
        /// of <see cref="GetErrors"/>.
        /// </summary>

        public virtual IAsyncResult BeginGetErrors(int pageIndex, int pageSize, IList<ErrorLogEntry> errorEntryList, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginSyncImpl(asyncCallback, asyncState, new GetErrorsHandler(GetErrors), pageIndex, pageSize, errorEntryList);
        }

        /// <summary>
        /// When overridden in a subclass, ends an asynchronous version 
        /// of <see cref="GetErrors"/>.
        /// </summary>
        
        public virtual int EndGetErrors(IAsyncResult asyncResult)
        {
            return (int) EndSyncImpl(asyncResult);
        }

        private delegate int GetErrorsHandler(int pageIndex, int pageSize, IList<ErrorLogEntry> errorEntryList);

        /// <summary>
        /// Get the name of this log.
        /// </summary>

        public virtual string Name
        {
            get { return GetType().Name; }   
        }

        /// <summary>
        /// Gets the name of the application to which the log is scoped.
        /// </summary>
        
        public string ApplicationName
        {
            get { return _appName ?? string.Empty; }
            
            set
            {
                if (_appNameInitialized)
                    throw new InvalidOperationException("The application name cannot be reset once initialized.");

                _appName = value;
                _appNameInitialized = (value ?? string.Empty).Length > 0;
            }
        }

        /// <summary>
        /// Gets the default error log implementation specified in the 
        /// configuration file, or the in-memory log implemention if
        /// none is configured.
        /// </summary>

        public static ErrorLog GetDefault(HttpContextBase context)
        {
            return (ErrorLog) ServiceCenter.GetService(context, typeof(ErrorLog));
        }

        internal static ErrorLog GetDefaultImpl(HttpContextBase context)
        {
            ErrorLog log;

            if (context != null)
            {
                log = (ErrorLog) context.Items[_contextKey];

                if (log != null)
                    return log;
            }

            //
            // Determine the default store type from the configuration and 
            // create an instance of it.
            //
            // If no object got created (probably because the right 
            // configuration settings are missing) then default to 
            // the in-memory log implementation.
            //

            log = (ErrorLog) SimpleServiceProviderFactory.CreateFromConfigSection(Configuration.GroupSlash + "errorLog") 
                  ?? new MemoryErrorLog();

            if (context != null)
            {
                //
                // Infer the application name from the context if it has not
                // been initialized so far.
                //

                if (log.ApplicationName.Length == 0)
                    log.ApplicationName = InferApplicationName(context);

                //
                // Save into the context if context is there so retrieval is
                // quick next time.
                //

                context.Items[_contextKey] = log;
            }

            return log;
        }

        private static string InferApplicationName(HttpContextBase context)
        {
            Debug.Assert(context != null);

            //
            // Setup the application name (ASP.NET 2.0 or later).
            //

            string appName = null;

            if (context.Request != null)
            {
                //
                // ASP.NET 2.0 returns a different and more cryptic value
                // for HttpRuntime.AppDomainAppId comared to previous 
                // versions. Also HttpRuntime.AppDomainAppId is not available
                // in partial trust environments. However, the APPL_MD_PATH
                // server variable yields the same value as 
                // HttpRuntime.AppDomainAppId did previously so we try to
                // get to it over here for compatibility reasons (otherwise
                // folks upgrading to this version of ELMAH could find their
                // error log empty due to change in application name.
                //

                appName = context.Request.ServerVariables["APPL_MD_PATH"];
            }

            if (string.IsNullOrEmpty(appName))
            {
                //
                // Still no luck? Try HttpRuntime.AppDomainAppVirtualPath,
                // which is available even under partial trust.
                //

                appName = HttpRuntime.AppDomainAppVirtualPath;
            }

            return Mask.EmptyString(appName, "/");
        }

        //
        // The following two methods are helpers that provide boilerplate 
        // implementations for implementing asnychronous BeginXXXX and 
        // EndXXXX methods over a default synchronous implementation.
        //

        private static IAsyncResult BeginSyncImpl(AsyncCallback asyncCallback, object asyncState, Delegate syncImpl, params object[] args)
        {
            Debug.Assert(syncImpl != null);

            SynchronousAsyncResult asyncResult;
            var syncMethodName = syncImpl.Method.Name;

            try
            {
                asyncResult = SynchronousAsyncResult.OnSuccess(syncMethodName, asyncState, 
                    syncImpl.DynamicInvoke(args));
            }
            catch (Exception e)
            {
                asyncResult = SynchronousAsyncResult.OnFailure(syncMethodName, asyncState, e);
            }

            if (asyncCallback != null)
                asyncCallback(asyncResult);

            return asyncResult;
        }

        private static object EndSyncImpl(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            var syncResult = asyncResult as SynchronousAsyncResult;

            if (syncResult == null)
                throw new ArgumentException("IAsyncResult object did not come from the corresponding async method on this type.", "asyncResult");

            //
            // IMPORTANT! The End method on SynchronousAsyncResult will 
            // throw an exception if that's what Log did when 
            // BeginLog called it. The unforunate side effect of this is
            // the stack trace information for the exception is lost and 
            // reset to this point. There seems to be a basic failure in the 
            // framework to accommodate for this case more generally. One 
            // could handle this through a custom exception that wraps the 
            // original exception, but this assumes that an invocation will 
            // only throw an exception of that custom type.
            //

            return syncResult.End();
        }
    }
}
