#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      James Driscoll, mailto:jamesdriscoll@btinternet.com
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
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    #endregion

    public static class StyleSheetHelper
    {
        public static string StylesheetHash { get; private set; }
        public static readonly string[] StylesheetResourceNames = new[] {"Bootstrap.css", "ErrorLog.css"};

        static StyleSheetHelper()
        {
            CalculateHash();
        }

        private static void CalculateHash()
        {
            var memoryStream = new MemoryStream();
            foreach (var resourceName in StylesheetResourceNames)
                ManifestResourceHelper.WriteResourceToStream(memoryStream, resourceName);
            
            var md5 = new MD5CryptoServiceProvider();
            var hash = md5.ComputeHash(memoryStream);

            StylesheetHash = hash.Select(b => b.ToString("x2"))
                                 .ToDelimitedString(null);
        }
    }
}