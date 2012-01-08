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

[assembly: Elmah.Scc("$Id: ErrorHtmlPage.cs 640 2009-06-01 17:22:02Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web;
    using System.Web.UI;

    #endregion

    /// <summary>
    /// Renders an HTML page displaying the detailed host-generated (ASP.NET)
    /// HTML recorded for an error from the error log.
    /// </summary>
    
    internal sealed class ErrorHtmlPage : IHttpHandler
    {
        public ErrorLog ErrorLog { get; set; }

        public void ProcessRequest(HttpContextBase context)
        {
            if (context == null) throw new ArgumentNullException("context");

            //
            // Retrieve the ID of the error to display and read it from 
            // the log.
            //

            var errorId = context.Request.QueryString["id"] ?? string.Empty;

            if (errorId.Length == 0)
                return;

            var log = ErrorLog ?? ErrorLog.GetDefault(context);
            var errorEntry = log.GetError(errorId);

            var response = context.Response;

            if (errorEntry == null)
            {
                // TODO: Send error response entity
                response.Status = HttpStatus.NotFound.ToString();
                return;
            }

            //
            // If we have a host (ASP.NET) formatted HTML message 
            // for the error then just stream it out as our response.
            //

            if (errorEntry.Error.WebHostHtmlMessage.Length == 0)
                return;

            response.Write(errorEntry.Error.WebHostHtmlMessage);
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            ProcessRequest(new HttpContextWrapper(context));
        }

        bool IHttpHandler.IsReusable { get { return true; } }
    }
}
