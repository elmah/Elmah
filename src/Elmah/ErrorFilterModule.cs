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

[assembly: Elmah.Scc("$Id: ErrorFilterModule.cs 593 2009-05-27 14:05:43Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Web;
    using Elmah.Assertions;

    #endregion

    /// <summary>
    /// HTTP module implementation that logs unhandled exceptions in an
    /// ASP.NET Web application to an error log.
    /// </summary>
    
    public class ErrorFilterModule : IHttpModule
    {
        private IAssertion _assertion = StaticAssertion.False;
        
        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>

        public virtual void Init(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");
            
            ErrorFilterConfiguration config = (ErrorFilterConfiguration) Configuration.GetSubsection("errorFilter");
            
            if (config == null)
                return;
            
            _assertion = config.Assertion;

            foreach (IHttpModule module in HttpModuleRegistry.GetModules(application))
            {
                IExceptionFiltering filtering = module as IExceptionFiltering;

                if (filtering != null)
                    filtering.Filtering += new ExceptionFilterEventHandler(OnErrorModuleFiltering);
            }
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module.
        /// </summary>
        
        public virtual void Dispose()
        {
        }

        public virtual IAssertion Assertion
        {
            get { return _assertion; }
        }

        protected virtual void OnErrorModuleFiltering(object sender, ExceptionFilterEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");
            
            if (args.Exception == null)
                throw new ArgumentException(null, "args");

            try
            {
                if (Assertion.Test(new AssertionHelperContext(sender, args.Exception, args.Context)))
                    args.Dismiss();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                throw;
            }
        }

        public sealed class AssertionHelperContext
        {
            private readonly object _source;
            private readonly Exception _exception;
            private readonly object _context;
            private Exception _baseException;
            private int _httpStatusCode;
            private bool _statusCodeInitialized;

            public AssertionHelperContext(Exception e, object context) :
                this(null, e, context) {}

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
                        
                        HttpException exception = Exception as HttpException;

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
}
