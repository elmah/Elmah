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

[assembly: Elmah.Scc("$Id: FixIIS5xWildcardMappingModule.cs 602 2009-05-27 23:28:19Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web;

    #endregion

    /// <summary>
    /// HTTP module that resolves issues in ELMAH when wilcard mapping
    /// is implemented in IIS 5.x.
    /// </summary>
    /// <remarks>
    /// See <a href="http://groups.google.com/group/elmah/browse_thread/thread/c22b85ace3812da1">Elmah 
    /// with existing wildcard mapping</a> for more information behind the 
    /// reason for this module.
    /// </remarks>

    public sealed class FixIIS5xWildcardMappingModule : IHttpModule
    {
        //
        // Mainly cribbed from an idea at http://forums.asp.net/t/1113541.aspx.
        //

        private string _handlerPathWithForwardSlash;
        private int _handlerPathLength;

        private static string GetHandlerPath()
        {
            System.Web.Configuration.HttpHandlersSection handlersSection = System.Configuration.ConfigurationManager.GetSection("system.web/httpHandlers") as System.Web.Configuration.HttpHandlersSection;
            string elmahHandlerTypeName = typeof(ErrorLogPageFactory).AssemblyQualifiedName;
            foreach (System.Web.Configuration.HttpHandlerAction handlerAction in handlersSection.Handlers)
                if (elmahHandlerTypeName.IndexOf(handlerAction.Type) == 0)
                    return handlerAction.Path;

            return null;
        }

        public void Init(HttpApplication context)
        {
            string handlerPath = GetHandlerPath();

            //
            // Only set things up if we've found the handler path
            //

            if (handlerPath != null && handlerPath.Length > 0)
            {
                _handlerPathWithForwardSlash = handlerPath;
                if (_handlerPathWithForwardSlash[_handlerPathWithForwardSlash.Length - 1] != '/')
                    _handlerPathWithForwardSlash += "/";

                _handlerPathLength = _handlerPathWithForwardSlash.Length -1;

                //
                // IIS 5.x with Wildcard mapping can't find the required
                // "elmah.axd" handler, so we need to intercept it
                // (which must happen when the request begins)
                // and then rewrite the path so that the handler is found.
                //

                context.BeginRequest += new EventHandler(OnBeginRequest);
            }
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication) sender;
            HttpContextBase context = new HttpContextWrapper(app.Context);
            string path = context.Request.Path;

            //
            // Check to see if we are dealing with a request for the "elmah.axd" handler
            // and if so, we need to rewrite the path!
            //

            int handlerPosition = path.IndexOf(_handlerPathWithForwardSlash, StringComparison.OrdinalIgnoreCase);

            if (handlerPosition >= 0)
                context.RewritePath(
                    path.Substring(0, handlerPosition + _handlerPathLength),
                    path.Substring(handlerPosition + _handlerPathLength),
                    context.Request.QueryString.ToString());
        }

        public void Dispose() { /* NOP */ }
    }
}
