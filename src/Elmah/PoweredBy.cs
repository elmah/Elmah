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

[assembly: Elmah.Scc("$Id: PoweredBy.cs 574 2009-05-11 14:49:34Z azizatif $")]

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
                string cacheKey = GetType().FullName;

                //
                // If cache is available then check if the version 
                // information is already residing in there.
                //

                if (this.Cache != null)
                    _about = (AboutSet) this.Cache[cacheKey];

                //
                // Not found in the cache? Go out and get the version 
                // information of the assembly housing this component.
                //
                
                if (_about == null)
                {
                    //
                    // NOTE: The assembly information is picked up from the 
                    // applied attributes rather that the more convenient
                    // FileVersionInfo because the latter required elevated
                    // permissions and may throw a security exception if
                    // called from a partially trusted environment, such as
                    // the medium trust level in ASP.NET.
                    //
                    
                    AboutSet about = new AboutSet();                    
                    Assembly assembly = this.GetType().Assembly;
                    about.Version = assembly.GetName().Version;
                    
                    AssemblyFileVersionAttribute version = (AssemblyFileVersionAttribute) Attribute.GetCustomAttribute(assembly, typeof(AssemblyFileVersionAttribute));
                    
                    if (version != null)
                        about.FileVersion = new Version(version.Version);

                    AssemblyProductAttribute product = (AssemblyProductAttribute) Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute));
                    
                    if (product != null)
                        about.Product = product.Product;

                    AssemblyCopyrightAttribute copyright = (AssemblyCopyrightAttribute) Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute));
                    
                    if (copyright != null)
                        about.Copyright = copyright.Copyright;
                    
                    //
                    // Cache for next time if the cache is available.
                    //

                    if (this.Cache != null)
                    {
                        this.Cache.Add(cacheKey, about,
                            /* absoluteExpiration */ null, Cache.NoAbsoluteExpiration,
                            TimeSpan.FromMinutes(2), CacheItemPriority.Normal, null);
                    }
                    
                    _about = about;
                }

                return _about;
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

        [ Serializable ]
        private sealed class AboutSet
        {
            private string _product;
            private Version _version;
            private Version _fileVersion;
            private string _copyright;

            public string Product
            {
                get { return Mask.NullString(_product); }
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
                get { return Mask.NullString(_copyright); }
                set { _copyright = value; }
            }
        }
    }
}
