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

[assembly: Elmah.Scc("$Id: PoweredBy.cs 640 2009-06-01 17:22:02Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Reflection;

    #endregion

    /// <summary>
    /// Displays a "Powered-by ELMAH" message that also contains the assembly
    /// file version informatin and copyright notice.
    /// </summary>

    partial class PoweredBy
    {
#if NET_3_5 || NET_4_0
        static readonly WeakReference CachedAboutRef = new WeakReference(null);
#else
        static readonly WeakReference<object[]> CachedAboutRef = new WeakReference<object[]>(null);
#endif

        static object[] CachedAbout
        {
            get
            {
#if NET_3_5 || NET_4_0
                return (object[]) CachedAboutRef.Target;
#else
                object[] tuple;
                return CachedAboutRef.TryGetTarget(out tuple) ? tuple : null;
#endif
            }
            set
            {
#if NET_3_5 || NET_4_0
                CachedAboutRef.Target = value;
#else
                CachedAboutRef.SetTarget(value);
#endif
                
            }
        }

        static T GetAbout<T>(Func<Version, Version, string, string, T> selector)
        {
            var tuple = CachedAbout;
            if (tuple != null)
                return selector((Version) tuple[0], (Version) tuple[1], (string) tuple[2], (string) tuple[3]);

            //
            // Not found in the cache? Go out and get the version 
            // information of the assembly housing this component.
            //

            //
            // NOTE: The assembly information is picked up from the 
            // applied attributes rather that the more convenient
            // FileVersionInfo because the latter required elevated
            // permissions and may throw a security exception if
            // called from a partially trusted environment, such as
            // the medium trust level in ASP.NET.
            //

            var assembly = typeof(ErrorLog).Assembly;

            var attributes = new
            {
                Version   = (AssemblyFileVersionAttribute) Attribute.GetCustomAttribute(assembly, typeof(AssemblyFileVersionAttribute)),
                Product   = (AssemblyProductAttribute)     Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute)),
                Copyright = (AssemblyCopyrightAttribute)   Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute)),
            };

            var version     = assembly.GetName().Version;
            var fileVersion = attributes.Version != null ? new Version(attributes.Version.Version) : null;
            var product     = attributes.Product != null ? attributes.Product.Product : null;
            var copyright   = attributes.Copyright != null ? attributes.Copyright.Copyright : null;

            //
            // Cache for next time if the cache is available.
            //

            CachedAbout = new object[] {version, fileVersion, product, copyright };

            return selector(version, fileVersion, product, copyright);
        }
    }
}
