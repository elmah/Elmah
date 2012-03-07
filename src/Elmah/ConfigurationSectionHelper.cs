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

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Configuration;
    using System.Xml;

    #endregion

    /// <summary>
    /// Helper class for handling values in configuration sections.
    /// </summary>

    internal sealed class ConfigurationSectionHelper
    {
        public static string GetValueAsString(XmlAttribute attribute)
        {
            return GetValueAsString(attribute, string.Empty);
        }

        public static string GetValueAsString(XmlAttribute attribute, string defaultValue)
        {
            if (attribute == null)
                return defaultValue;
            
            return Mask.EmptyString(attribute.Value, defaultValue);
        }

        public static bool GetValueAsBoolean(XmlAttribute attribute)
        {
            //
            // If the attribute is absent, then always assume the default value
            // of false. Not allowing the default value to be parameterized
            // maintains a consisent policy and makes it easier for the user to
            // remember that all boolean options default to false if not
            // specified.
            //

            if (attribute == null)
                return false;

            try
            {
                return XmlConvert.ToBoolean(attribute.Value);
            }
            catch (FormatException e)
            {
                throw new ConfigurationException(string.Format("Error in parsing the '{0}' attribute of the '{1}' element as a boolean value. Use either 1, 0, true or false (latter two being case-sensitive).", attribute.Name, attribute.OwnerElement.Name), e, attribute);
            }
        }
        
        private ConfigurationSectionHelper()
        {
            throw new NotSupportedException();
        }
    }
}