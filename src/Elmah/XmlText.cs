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

[assembly: Elmah.Scc("$Id: XmlText.cs 726 2010-10-03 19:16:13Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System.Text.RegularExpressions;

    #endregion

    /// <summary>
    /// XML 1.0 services.
    /// </summary>
    
    internal sealed class XmlText
    {
        private XmlText() { }

        /// <summary>
        /// Replaces illegal XML characters with a question mark (?).
        /// </summary>
        /// <remarks>
        /// Only strips illegal characters as per XML 1.0, not 1.1. 
        /// See section <a href="http://www.w3.org/TR/2006/REC-xml-20060816/#charsets">2.2 Characters</a>
        /// of <a href="http://www.w3.org/TR/2006/REC-xml-20060816">Extensible Markup Language (XML) 1.0 (Fourth Edition)</a>.
        /// </remarks>
        
        public static string StripIllegalXmlCharacters(string xml)
        {
            return StripIllegalXmlCharacters(xml, null);
        }

        /// <summary>
        /// Replaces illegal XML characters with a replacement string,
        /// with the default being a question mark (?) if the replacement
        /// is null reference.
        /// </summary>
        /// <remarks>
        /// Only strips illegal characters as per XML 1.0, not 1.1. 
        /// See section <a href="http://www.w3.org/TR/2006/REC-xml-20060816/#charsets">2.2 Characters</a>
        /// of <a href="http://www.w3.org/TR/2006/REC-xml-20060816">Extensible Markup Language (XML) 1.0 (Fourth Edition)</a>.
        /// </remarks>
        
        public static string StripIllegalXmlCharacters(string xml, string replacement)
        {
            // TODO Consider expanding illegal character set to XML 1.1
            
            const string pattern = @"&#x(0{0,3}[0-8BCEF]|0{0,2}1[0-F]|D[89A-F][0-9A-F]{2}|FFF[EF]);";
            const RegexOptions options = RegexOptions.IgnoreCase 
                                       | RegexOptions.CultureInvariant;
            return Regex.Replace(xml, pattern, replacement != null ? replacement : "?", options);
        }
    }
}