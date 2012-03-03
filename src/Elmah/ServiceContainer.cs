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

[assembly: Elmah.Scc("$Id: ServiceContainer.cs 701 2010-01-12 07:14:05Z azizatif $")]

namespace Elmah
{
    #region Imports
    
    using System;
    using System.Web;

    #endregion

    internal sealed class ServiceContainer : IServiceProvider
    {
        private readonly object _context;

        public ServiceContainer(object context)
        {
            // NOTE: context is allowed to be null

            _context = context;
        }

        public object GetService(Type serviceType)
        {
            return serviceType == typeof(ErrorLog) 
                 ? ErrorLog.GetDefaultImpl(_context as HttpContext) 
                 : null;
        }
    }
}
