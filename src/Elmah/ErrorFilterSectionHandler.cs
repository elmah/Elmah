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

[assembly: Elmah.Scc("$Id: ErrorFilterSectionHandler.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Configuration;
    using System.Xml;
    using Elmah.Assertions;

    #endregion

    /// <summary>
    /// Handler for the &lt;errorFilter&gt; section of the
    /// configuration file.
    /// </summary>

    internal sealed class ErrorFilterSectionHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            if (section == null)
                throw new ArgumentNullException("section");
            
            //
            // Either inherit the incoming parent configuration (for example
            // from the machine configuration file) or start with a fresh new
            // one.
            //

            ErrorFilterConfiguration config;

            if (parent != null)
            {
                ErrorFilterConfiguration parentConfig = (ErrorFilterConfiguration) parent;
                config = (ErrorFilterConfiguration) ((ICloneable) parentConfig).Clone();
            }    
            else
            {
                config = new ErrorFilterConfiguration();
            }

            //
            // Take the first child of <test> and turn it into the
            // assertion.
            //

            XmlElement assertionNode = (XmlElement) section.SelectSingleNode("test/*");

            if (assertionNode != null)
                config.SetAssertion(AssertionFactory.Create(assertionNode));

            return config;
        }
    }
}
