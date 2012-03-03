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

[assembly: Elmah.Scc("$Id: ErrorRssHandler.cs 909 2011-12-18 17:33:23Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web;
    using ContentSyndication;

    using ArrayList = System.Collections.ArrayList;

    #endregion

    /// <summary>
    /// Renders a XML using the RSS 0.91 vocabulary that displays, at most,
    /// the 15 most recent errors recorded in the error log.
    /// </summary>

    internal sealed class ErrorRssHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/xml";

            //
            // Get the last set of errors for this application.
            //

            const int pageSize = 15;
            ArrayList errorEntryList = new ArrayList(pageSize);
            ErrorLog log = ErrorLog.GetDefault(context);
            log.GetErrors(0, pageSize, errorEntryList);

            //
            // We'll be emitting RSS vesion 0.91.
            //

            RichSiteSummary rss = new RichSiteSummary();
            rss.version = "0.91";

            //
            // Set up the RSS channel.
            //
            
            Channel channel = new Channel();
            string hostName = Environment.TryGetMachineName(context);
            channel.title = "Error log of " + log.ApplicationName 
                          + (hostName.Length > 0 ? " on " + hostName : null);
            channel.description = "Log of recent errors";
            channel.language = "en";
            channel.link = ErrorLogPageFactory.GetRequestUrl(context).GetLeftPart(UriPartial.Authority) + 
                context.Request.ServerVariables["URL"];

            rss.channel = channel;

            //
            // For each error, build a simple channel item. Only the title, 
            // description, link and pubDate fields are populated.
            //

            channel.item = new Item[errorEntryList.Count];

            for (int index = 0; index < errorEntryList.Count; index++)
            {
                ErrorLogEntry errorEntry = (ErrorLogEntry) errorEntryList[index];
                Error error = errorEntry.Error;

                Item item = new Item();

                item.title = error.Message;
                item.description = "An error of type " + error.Type + " occurred. " + error.Message;
                item.link = channel.link + "/detail?id=" + HttpUtility.UrlEncode(errorEntry.Id);
                item.pubDate = error.Time.ToUniversalTime().ToString("r");

                channel.item[index] = item;
            }

            //
            // Stream out the RSS XML.
            //

            context.Response.Write(XmlText.StripIllegalXmlCharacters(XmlSerializer.Serialize(rss)));
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
