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

[assembly: Elmah.Scc("$Id: ErrorHtmlPage.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web.UI;

    #endregion

    /// <summary>
    /// Renders an HTML page displaying the detailed host-generated (ASP.NET)
    /// HTML recorded for an error from the error log.
    /// </summary>
    
    internal sealed class ErrorHtmlPage : ErrorPageBase
    {
        protected override void Render(HtmlTextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            //
            // Retrieve the ID of the error to display and read it from 
            // the log.
            //

            string errorId = Mask.NullString(this.Request.QueryString["id"]);

            if (errorId.Length == 0)
                return;

            ErrorLogEntry errorEntry = this.ErrorLog.GetError(errorId);

            if (errorEntry == null)
            {
                // TODO: Send error response entity
                Response.Status = HttpStatus.NotFound.ToString();
                return;
            }

            //
            // If we have a host (ASP.NET) formatted HTML message 
            // for the error then just stream it out as our response.
            //

            if (errorEntry.Error.WebHostHtmlMessage.Length == 0)
                return;

            writer.Write(errorEntry.Error.WebHostHtmlMessage);
        }
    }
}
