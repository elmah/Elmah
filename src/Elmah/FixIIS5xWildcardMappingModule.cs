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

[assembly: Elmah.Scc("$Id: FixIIS5xWildcardMappingModule.cs 566 2009-05-11 10:37:10Z azizatif $")]

//
// this module is not currently available for .Net 1.0
// if someone can get the context.RewritePath line working in 1.0, 
// then it can be used in 1.0 as well!!
//

#if !NET_1_0
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

#if !NET_1_1
        private static string GetHandlerPath()
        {
            System.Web.Configuration.HttpHandlersSection handlersSection = System.Configuration.ConfigurationManager.GetSection("system.web/httpHandlers") as System.Web.Configuration.HttpHandlersSection;
            string elmahHandlerTypeName = typeof(ErrorLogPageFactory).AssemblyQualifiedName;
            foreach (System.Web.Configuration.HttpHandlerAction handlerAction in handlersSection.Handlers)
                if (elmahHandlerTypeName.IndexOf(handlerAction.Type) == 0)
                    return handlerAction.Path;

            return null;
        }
#else
        private const string DefaultHandlerPath = "elmah.axd";
        private static string GetHandlerPath()
        {
            System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
            try
            {
                //
                // Try and load the web.config file
                //

                string webConfigFile = HttpContext.Current.Server.MapPath(HttpContext.Current.Request.ApplicationPath + "/web.config");
                xml.Load(webConfigFile);
            }
            catch (Exception)
            {
                //
                // There were issues loading web.config, so let's assume the default 
                // 

                return DefaultHandlerPath;
            }

            //
            // We are looking for the Elmah handler...
            // So we need to look in...
            // <configuration>
            //   <system.web>
            //     <httpHandlers>
            //       <add type="*ErrorLogPageFactory*" path="****" />
            // We use contains for the ErrorLogPageFactory so that we pick up all variations here
            // And we pull out the path node, as that contains what we want!
            //

            System.Xml.XmlNode node = xml.SelectSingleNode("/configuration/system.web/httpHandlers/add[contains(@type, 'ErrorLogPageFactory')]/@path");
            if (node != null)
                return node.InnerText;

            return null;
        }
#endif
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

#if NET_1_1
                //
                // Convert to lower case as we will be comparing against that later
                //

                _handlerPathWithForwardSlash = _handlerPathWithForwardSlash.ToLower();
#endif
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
            HttpApplication app = sender as HttpApplication;
            HttpContext context = app.Context;
            string path = context.Request.Path;

            //
            // Check to see if we are dealing with a request for the "elmah.axd" handler
            // and if so, we need to rewrite the path!
            //

#if !NET_1_1
            int handlerPosition = path.IndexOf(_handlerPathWithForwardSlash, StringComparison.OrdinalIgnoreCase);
#else
            int handlerPosition = path.ToLower().IndexOf(_handlerPathWithForwardSlash);
#endif
            if (handlerPosition >= 0)
                context.RewritePath(
                    path.Substring(0, handlerPosition + _handlerPathLength),
                    path.Substring(handlerPosition + _handlerPathLength),
                    context.Request.QueryString.ToString());
        }

        public void Dispose() { /* NOP */ }
    }
}
#endif
