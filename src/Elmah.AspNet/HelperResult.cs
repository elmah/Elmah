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

namespace Elmah
{
    #region Imports

    using System;
    using System.Globalization;
    using System.IO;

    #endregion

    /// <summary>
    /// Represents the result of a helper action as an HTML-encoded string.
    /// </summary>

    // See http://msdn.microsoft.com/en-us/library/system.web.webpages.helperresult.aspx

    sealed class HelperResult : IHtmlString
    {
        private readonly Action<TextWriter> _action;

        public HelperResult(Action<TextWriter> action)
        {
            if (action == null) throw new ArgumentNullException("action");
            _action = action;
        }

        public string ToHtmlString()
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }

        public override string ToString()
        {
            return ToHtmlString();
        }
        
        public void WriteTo(TextWriter writer)
        {
            _action(writer);
        }
    }
}