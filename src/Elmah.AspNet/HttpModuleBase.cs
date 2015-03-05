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

[assembly: Elmah.Scc("$Id: HttpModuleBase.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web;

    #endregion

    /// <summary>
    /// Provides an abstract base class for <see cref="IHttpModule"/> that
    /// supports discovery from within partial trust environments.
    /// </summary>

    public abstract class HttpModuleBase : IHttpModule
    {
        void IHttpModule.Init(HttpApplication context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (SupportDiscoverability)
                HttpModuleRegistry.RegisterInPartialTrust(context, this);

            OnInit(context);
        }

        void IHttpModule.Dispose()
        {
            OnDispose();
        }

        /// <summary>
        /// Determines whether the module will be registered for discovery
        /// in partial trust environments or not.
        /// </summary>

        protected virtual bool SupportDiscoverability
        {
            get { return false; }
        }

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>
        
        protected virtual void OnInit(HttpApplication application) {}

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module.
        /// </summary>

        protected virtual void OnDispose() {}
    }
}