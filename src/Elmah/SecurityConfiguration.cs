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

[assembly: Elmah.Scc("$Id: SecurityConfiguration.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections;
    using System.Globalization;

    #endregion

    [ Serializable ]
    internal sealed class SecurityConfiguration
    {
        public static readonly SecurityConfiguration Default;

        private readonly bool _allowRemoteAccess;

        private static readonly string[] _trues = new string[] { "true", "yes", "on", "1" };

        static SecurityConfiguration()
        {
            Default = new SecurityConfiguration((IDictionary) Configuration.GetSubsection("security"));
        }
        
        public SecurityConfiguration(IDictionary options)
        {
            _allowRemoteAccess = GetBoolean(options, "allowRemoteAccess");
        }
        
        public bool AllowRemoteAccess
        {
            get { return _allowRemoteAccess; }
        }

        private static bool GetBoolean(IDictionary options, string name)
        {
            string str = GetString(options, name).Trim().ToLower(CultureInfo.InvariantCulture);
            return Boolean.TrueString.Equals(StringTranslation.Translate(Boolean.TrueString, str, _trues));
        }

        private static string GetString(IDictionary options, string name)
        {
            Debug.Assert(name != null);

            if (options == null)
                return string.Empty;

            object value = options[name];

            if (value == null)
                return string.Empty;

            return Mask.NullString(value.ToString());
        }
    }
}