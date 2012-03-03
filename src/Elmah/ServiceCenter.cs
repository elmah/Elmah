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

[assembly: Elmah.Scc("$Id: ServiceCenter.cs 698 2010-01-09 19:20:54Z azizatif $")]

namespace Elmah
{
    #region Imports
    
    using System;

    #endregion

    /// <summary>
    /// A delegate to an implementation that returns an <see cref="IServiceProvider"/>
    /// object based on a given context.
    /// </summary>

    public delegate IServiceProvider ServiceProviderQueryHandler(object context);

    /// <summary>
    /// Central point for locating arbitrary services.
    /// </summary>

    public sealed class ServiceCenter
    {
        /// <summary>
        /// The default and factory-supplied implementation of 
        /// <see cref="ServiceProviderQueryHandler"/>.
        /// </summary>

        public static readonly ServiceProviderQueryHandler Default;

        private static ServiceProviderQueryHandler _current;

        static ServiceCenter()
        {
            _current = Default = new ServiceProviderQueryHandler(CreateServiceContainer);
        }

        private static IServiceProvider CreateServiceContainer(object context)
        {
            return new ServiceContainer(context);
        }

        /// <summary>
        /// The current <see cref="ServiceProviderQueryHandler"/> implementation
        /// in effect.
        /// </summary>

        public static ServiceProviderQueryHandler Current
        {
            get { return _current; }
            
            set
            {
                if (value == null) 
                    throw new ArgumentNullException("value");
                _current = value;
            }
        }

        /// <summary>
        /// Attempts to locate a service of a given type based on a given context.
        /// If the service is not available, a null reference is returned.
        /// </summary>

        public static object FindService(object context, Type serviceType)
        {
            if (serviceType == null) 
                throw new ArgumentNullException("serviceType");
            
            return GetServiceProvider(context).GetService(serviceType);
        }

        /// <summary>
        /// Gets a service of a given type based on a given context.
        /// If the service is not available, an exception is thrown.
        /// </summary>

        public static object GetService(object context, Type serviceType)
        {
            object service = FindService(context, serviceType);
            
            if (service == null)
                throw new Exception(string.Format("Service of the type {0} is not available.", serviceType));
            
            return service;
        }

        /// <summary>
        /// Gets an <see cref="IServiceProvider"/> object based on a 
        /// supplied context and which can be used to request further
        /// services.
        /// </summary>

        public static IServiceProvider GetServiceProvider(object context)
        {
            IServiceProvider sp = Current(context);
            
            if (sp == null)
                throw new Exception("Service provider not available.");
            
            return sp;
        }

        private ServiceCenter() {}
    }
}
