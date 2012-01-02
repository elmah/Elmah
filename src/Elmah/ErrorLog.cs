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
            return Apm.BeginSync(asyncCallback, asyncState, this, "Log", () => Log(error));
        }

        /// <summary>
        /// When overridden in a subclass, ends an asynchronous version 
        /// of <see cref="Log"/>.
        /// </summary>

        public virtual string EndLog(IAsyncResult asyncResult)
        {
            return AsyncResult<string>.End(asyncResult, this, "Log");
        }

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
            return Apm.BeginSync(asyncCallback, asyncState, this, "GetError", () => GetError(id));
        }

        /// <summary>
        /// When overridden in a subclass, ends an asynchronous version 
        /// of <see cref="GetError"/>.
        /// </summary>

        public virtual ErrorLogEntry EndGetError(IAsyncResult asyncResult)
        {
            return AsyncResult<ErrorLogEntry>.End(asyncResult, this, "GetError");
        }

        /// <summary>
        /// Retrieves a page of application errors from the log in 
        /// descending order of logged time.
        /// </summary>

        public abstract int GetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList);

        /// <summary>
        /// When overridden in a subclass, begins an asynchronous version 
        /// of <see cref="GetErrors"/>.
        /// </summary>

        public virtual IAsyncResult BeginGetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList, AsyncCallback asyncCallback, object asyncState)
        {
            return Apm.BeginSync(asyncCallback, asyncState, this, "GetErrors", () => GetErrors(pageIndex, pageSize, errorEntryList));
        }

        /// <summary>
        /// When overridden in a subclass, ends an asynchronous version 
        /// of <see cref="GetErrors"/>.
        /// </summary>
        
        public virtual int EndGetErrors(IAsyncResult asyncResult)
        {
            return AsyncResult<int>.End(asyncResult, this, "GetErrors");
        }

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
    }
}
