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

[assembly: Elmah.Scc("$Id: ManifestResourceHandler.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Net.Mime;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Owin;
    using Encoding = System.Text.Encoding;

    #endregion

    /// <summary>
    /// Reads a resource from the assembly manifest and returns its contents
    /// as the response entity.
    /// </summary>

    static class ManifestResourceHandler
    {
        public static Task ProcessRequest(IOwinContext context, string resourceName, string mediaType)
        {
            return ProcessRequest(context, resourceName, mediaType, null);
        }

        public static Task ProcessRequest(IOwinContext context, string resourceName, string mediaType, Encoding responseEncoding)
        {
            return ProcessRequest(context, new[] { resourceName }, mediaType, responseEncoding, false);
        }

        public static Task ProcessRequest(IOwinContext context, IEnumerable<string> resourceNames, string mediaType)
        {
            return ProcessRequest(context, resourceNames, mediaType, null, false);
        }

        public static Task ProcessRequest(IOwinContext context, IEnumerable<string> resourceNames, string mediaType, Encoding responseEncoding, bool cacheResponse)
        {
            Debug.Assert(resourceNames != null);
            Debug.AssertStringNotEmpty(mediaType);

            //
            // Set the response headers for indicating the content type 
            // and encoding (if specified).
            //

            var response = context.Response;

            var contentType = new ContentType
            {
                MediaType = mediaType, 
                CharSet   = responseEncoding != null 
                            ? responseEncoding.WebName 
                            : null
            };
            response.ContentType = contentType.ToString();

            if (cacheResponse)
            {
                response.Expires = DateTimeOffset.Now.AddYears(5);
                response.Headers["Cache-Control"] = "public";
            }

            foreach (var resourceName in resourceNames)
                ManifestResourceHelper.WriteResourceToStream(response.Body, typeof(ManifestResourceHandler), resourceName);

            return CompletedTask.Return();
        }
    }
}
