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
    using System.Web;

    using Stream = System.IO.Stream;
    using Encoding = System.Text.Encoding;

    #endregion

    /// <summary>
    /// Reads a resource from the assembly manifest and returns its contents
    /// as the response entity.
    /// </summary>

    internal sealed class ManifestResourceHandler : IHttpHandler
    {
        private readonly string _resourceName;
        private readonly string _contentType;
        private readonly Encoding _responseEncoding;

        public ManifestResourceHandler(string resourceName, string contentType) :
            this(resourceName, contentType, null) {}

        public ManifestResourceHandler(string resourceName, string contentType, Encoding responseEncoding)
        {
            Debug.AssertStringNotEmpty(resourceName);
            Debug.AssertStringNotEmpty(contentType);

            _resourceName = resourceName;
            _contentType = contentType;
            _responseEncoding = responseEncoding;
        }

        public void ProcessRequest(HttpContext context)
        {
            //
            // Set the response headers for indicating the content type 
            // and encoding (if specified).
            //

            HttpResponse response = context.Response;
            response.ContentType = _contentType;

            if (_responseEncoding != null)
                response.ContentEncoding = _responseEncoding;

            ManifestResourceHelper.WriteResourceToStream(response.OutputStream, _resourceName);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
