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

[assembly: Elmah.Scc("$Id: MsAjaxDeltaErrorLogModule.cs 602 2009-05-27 23:28:19Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web;
    using System.Web.UI;

    #endregion

    /// <summary>
    /// Module to log unhandled exceptions during a delta-update
    /// request issued by the client when a page uses the UpdatePanel
    /// introduced with ASP.NET 2.0 AJAX Extensions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This module is ONLY required when dealing with v1.0.x.x of System.Web.Extensions.dll
    /// (i.e. the downloadable version to extend v2.0 of the .Net Framework)
    /// </para>
    /// <para>
    /// Using it with v3.5 of System.Web.Extensions.dll (which shipped as part of v3.5 of the 
    /// .Net Framework) will result in a duplication of errors.
    /// </para>
    /// <para>
    /// This is because MS have changed the implementation of 
    /// System.Web.UI.PageRequestManager.OnPageError
    /// </para>
    /// <para>
    /// In v1.0.x.x, the code performs a brutal <code>Response.End();</code> in an attempt to
    /// "tidy up"! This means that the error will not bubble up to the Application.Error 
    /// handlers, so Elmah is unable to catch them.
    /// </para>
    /// <para>
    /// In v3.5, this is handled much more gracefully, allowing Elmah to do its thing without
    /// the need for this module!
    /// </para>
    /// </remarks>

    public class MsAjaxDeltaErrorLogModule : IHttpModule
    {
        public virtual void Init(HttpApplication context)
        {
            context.PostMapRequestHandler += OnPostMapRequestHandler;
        }

        public virtual void Dispose() { /* NOP */ }

        private void OnPostMapRequestHandler(object sender, EventArgs args)
        {
            HttpContextBase context = new HttpContextWrapper(((HttpApplication)sender).Context);

            if (!IsAsyncPostBackRequest(context.Request))
                return;

            Page page = context.Handler as Page;

            if (page == null)
                return;

            page.Error += OnPageError;
        }

        protected virtual void OnPageError(object sender, EventArgs args)
        {
            Page page = (Page) sender;
            Exception exception = page.Server.GetLastError();

            if (exception == null)
                return;

            HttpContextBase context = new HttpContextWrapper(HttpContext.Current);
            LogException(exception, context);
        }

        /// <summary>
        /// Logs an exception and its context to the error log.
        /// </summary>

        protected virtual void LogException(Exception e, HttpContextBase context)
        {
            ErrorSignal.FromContext(context).Raise(e, context);
        }

        protected virtual bool IsAsyncPostBackRequest(HttpRequestBase request)
        {
            if (request == null) 
                throw new ArgumentNullException("request");
            
            string[] values = request.Headers.GetValues("X-MicrosoftAjax");

            if (values == null || values.Length == 0)
                return false;

            foreach (string value in values)
            {
                if (string.Compare(value, "Delta=true", StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

            return false;
        }
    }
}
