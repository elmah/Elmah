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

    public class ManifestResourceHelper
    {
        public static void WriteResourceToStream(Stream outputStream, string resourceName)
        {
            //
            // Grab the resource stream from the manifest.
            //

            Type thisType = typeof(ManifestResourceHelper);

            using (Stream inputStream = thisType.Assembly.GetManifestResourceStream(thisType, resourceName))
            {

                //
                // Allocate a buffer for reading the stream. The maximum size
                // of this buffer is fixed to 4 KB.
                //

                byte[] buffer = new byte[Math.Min(inputStream.Length, 4096)];

                //
                // Finally, write out the bytes!
                //

                int readLength = inputStream.Read(buffer, 0, buffer.Length);

                while (readLength > 0)
                {
                    outputStream.Write(buffer, 0, readLength);
                    readLength = inputStream.Read(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
