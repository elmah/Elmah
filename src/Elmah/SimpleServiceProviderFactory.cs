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

[assembly: Elmah.Scc("$Id: SimpleServiceProviderFactory.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;

    using IDictionary = System.Collections.IDictionary;

    #endregion

    /// <summary>
    /// A simple factory for creating instances of types specified in a 
    /// section of the configuration file.
    /// </summary>
    
    internal sealed class SimpleServiceProviderFactory
    {
        public static object CreateFromConfigSection(string sectionName)
        {
            Debug.AssertStringNotEmpty(sectionName);

            //
            // Get the configuration section with the settings.
            //
            
            IDictionary config = (IDictionary) Configuration.GetSection(sectionName);

            if (config == null)
                return null;

            //
            // We modify the settings by removing items as we consume 
            // them so make a copy here.
            //

            config = (IDictionary) ((ICloneable) config).Clone();

            //
            // Get the type specification of the service provider.
            //

            string typeSpec = Mask.NullString((string) config["type"]);
            
            if (typeSpec.Length == 0)
                return null;

            config.Remove("type");

            //
            // Locate, create and return the service provider object.
            //

            Type type = Type.GetType(typeSpec, true);
            return Activator.CreateInstance(type, new object[] { config });
        }

        private SimpleServiceProviderFactory() {}
    }
}
