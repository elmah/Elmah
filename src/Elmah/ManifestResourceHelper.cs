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

[assembly: Elmah.Scc("$Id: ManifestResourceHelper.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.IO;

    #endregion

    public static class ManifestResourceHelper
    {
        public static void WriteResourceToStream(Stream outputStream, Type type, string resourceName)
        {
            if (outputStream == null) throw new ArgumentNullException("outputStream");
            if (resourceName == null) throw new ArgumentNullException("resourceName");
            if (resourceName.Length == 0) throw new ArgumentException(null, "resourceName");

            var thisType = type ?? typeof(ManifestResourceHelper);
            var thisAssembly = thisType.Assembly;

            using (var inputStream = thisAssembly.GetManifestResourceStream(thisType, resourceName))
            {
                if (inputStream == null)
                {
                    throw new Exception(string.Format(
                        @"Resource named {0}.{1} not found in assembly {2}.", 
                        thisType.Namespace, resourceName, thisAssembly));
                }

                var buffer = new byte[Math.Min(inputStream.Length, 4096)];
                var readLength = inputStream.Read(buffer, 0, buffer.Length);
                while (readLength > 0)
                {
                    outputStream.Write(buffer, 0, readLength);
                    readLength = inputStream.Read(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
