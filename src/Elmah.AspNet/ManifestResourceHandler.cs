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
    using System.Web;
    using Encoding = System.Text.Encoding;

    #endregion

    /// <summary>
    /// Reads a resource from the assembly manifest and returns its contents
    /// as the response entity.
    /// </summary>

    static class ManifestResourceHandler
    {
        public static Action<HttpContextBase> Create(string resourceName, string mediaType)
        {
            return Create(resourceName, mediaType, null);
        }

        public static Action<HttpContextBase> Create(string resourceName, string mediaType, Encoding responseEncoding)
        {
            return Create(new[] { resourceName }, mediaType, responseEncoding, false);
        }

        public static Action<HttpContextBase> Create(IEnumerable<string> resourceNames, string mediaType)
        {
            return Create(resourceNames, mediaType, null, false);
        }

        public static Action<HttpContextBase> Create(IEnumerable<string> resourceNames, string mediaType, Encoding responseEncoding, bool cacheResponse)
        {
            Debug.Assert(resourceNames != null);
            Debug.AssertStringNotEmpty(mediaType);

            return context =>
            {
                //
                // Set the response headers for indicating the content type 
                // and encoding (if specified).
                //

                var response = context.Response;
                response.ContentType = mediaType;

                if (cacheResponse)
                {
                    response.Cache.SetCacheability(HttpCacheability.Public);
                    response.Cache.SetExpires(DateTime.MaxValue);
                }

                if (responseEncoding != null)
                    response.ContentEncoding = responseEncoding;

                foreach (var resourceName in resourceNames)
                    ManifestResourceHelper.WriteResourceToStream(response.OutputStream, typeof(ManifestResourceHandler), resourceName);
            };
        }
    }
}
