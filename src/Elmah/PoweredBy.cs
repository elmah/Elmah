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
    using System.Web.UI;
    using System.Web.UI.WebControls;

    using Assembly = System.Reflection.Assembly;
    using HttpUtility = System.Web.HttpUtility;
    using Cache = System.Web.Caching.Cache;
    using CacheItemPriority = System.Web.Caching.CacheItemPriority;
    using HttpRuntime = System.Web.HttpRuntime;

    #endregion

    /// <summary>
    /// Displays a "Powered-by ELMAH" message that also contains the assembly
    /// file version informatin and copyright notice.
    /// </summary>

    public sealed class PoweredBy : WebControl
    {
        private AboutSet _about;

        /// <summary>
        /// Renders the contents of the control into the specified writer.
        /// </summary>

        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            //
            // Write out the assembly title, version number, copyright and
            // license.
            //

            AboutSet about = this.About;

            writer.Write("Powered by ");
            writer.AddAttribute(HtmlTextWriterAttribute.Href, "http://elmah.googlecode.com/");
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            HttpUtility.HtmlEncode(Mask.EmptyString(about.Product, "(product)"), writer);
            writer.RenderEndTag();
            writer.Write(", version ");

            string version = about.GetFileVersionString();
            
            if (version.Length == 0)
                version = about.GetVersionString();

            HttpUtility.HtmlEncode(Mask.EmptyString(version, "?.?.?.?"), writer);

#if DEBUG
            writer.Write(" (" + Build.Configuration + ")");
#endif
            
            writer.Write(". ");
            
            string copyright = about.Copyright;
            
            if (copyright.Length > 0)
            {
                HttpUtility.HtmlEncode(copyright, writer);
                writer.Write(' ');
            }

            writer.Write("Licensed under ");
            writer.AddAttribute(HtmlTextWriterAttribute.Href, "http://www.apache.org/licenses/LICENSE-2.0");
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            writer.Write("Apache License, Version 2.0");
            writer.RenderEndTag();
            writer.Write(". ");
        }

        private AboutSet About
        {
            get
            {
                return _about ?? (_about = GetAbout(Cache, (version, fileVersion, product, copyright) => new AboutSet
                {
                    Version     = version,
                    FileVersion = fileVersion,
                    Product     = product,
                    Copyright   = copyright,
                }));
            }
        }

        private Cache Cache
        {
            get
            {
                //
                // Get the cache from the container page, or failing that, 
                // from the runtime. The Page property can be null
                // if the control has not been added to a page's controls
                // hierarchy.
                //

                return this.Page != null? this.Page.Cache : HttpRuntime.Cache;
            }
        }

        internal static T GetAbout<T>(Cache cache, Func<Version, Version, string, string, T> selector)
        {
            var cacheKey = typeof(PoweredBy).FullName;

            //
            // If cache is available then check if the version 
            // information is already residing in there.
            //

            if (cache != null)
            {
                Debug.Assert(cacheKey != null);
                var tuple = (object[]) cache[cacheKey];
                if (tuple != null)
                    return selector((Version) tuple[0], (Version) tuple[1], (string) tuple[2], (string) tuple[3]);
            }

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

            if (cache != null)
            {
                cache.Add(cacheKey, 
                    new object[] { version, fileVersion, product, copyright, },
                    /* absoluteExpiration */ null, Cache.NoAbsoluteExpiration,
                    TimeSpan.FromMinutes(2), CacheItemPriority.Normal, null);
            }

            return selector(version, fileVersion, product, copyright);
        }

        [ Serializable ]
        private sealed class AboutSet
        {
            private string _product;
            private Version _version;
            private Version _fileVersion;
            private string _copyright;

            public string Product
            {
                get { return _product ?? string.Empty; }
                set { _product = value; }
            }

            public Version Version
            {
                get { return _version; }
                set { _version = value; }
            }

            public string GetVersionString()
            {
                return _version != null ? _version.ToString() : string.Empty;
            }

            public Version FileVersion
            {
                get { return _fileVersion; }
                set { _fileVersion = value; }
            }

            public string GetFileVersionString()
            {
                return _fileVersion != null ? _fileVersion.ToString() : string.Empty;
            }

            public string Copyright
            {
                get { return _copyright ?? string.Empty; }
                set { _copyright = value; }
            }
        }
    }
}
