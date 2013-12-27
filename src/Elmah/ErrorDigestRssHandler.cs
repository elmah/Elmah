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

[assembly: Elmah.Scc("$Id: ErrorDigestRssHandler.cs 923 2011-12-23 22:02:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;
    using System.Xml.Linq;
    using System.Collections.Generic;

    #endregion

    /// <summary>
    /// Renders an RSS feed that is a daily digest of the most recently 
    /// recorded errors in the error log. The feed spans at most 15
    /// days on which errors occurred.
    /// </summary>

    static class ErrorDigestRssHandler
    {
        public static void ProcessRequest(HttpContextBase context)
        {
            var log = ErrorLog.GetDefault(context);

            var request = context.Request;
            var response = context.Response;
            
            response.ContentType = "application/xml";

            var title = string.Format(@"Daily digest of errors in {0} on {1}", 
                                      log.ApplicationName, Environment.MachineName);

            var link = ErrorLogPageFactory.GetRequestUrl(context).GetLeftPart(UriPartial.Authority) + request.ServerVariables["URL"];
            var baseUrl = new Uri(link.TrimEnd('/') + "/");

            var items = GetItems(log, baseUrl, 30, 30).Take(30);
            var rss = RssXml.Rss(title, link, "Daily digest of application errors", items);

            context.Response.Write(XmlText.StripIllegalXmlCharacters(rss.ToString()));
        }
        
        private static IEnumerable<XElement> GetItems(ErrorLog log, Uri baseUrl, int pageSize, int maxPageLimit) 
        {
            Debug.Assert(log != null);
            Debug.Assert(baseUrl != null);
            Debug.Assert(baseUrl.IsAbsoluteUri);
            Debug.Assert(pageSize > 0);

            var runningDay = DateTime.MaxValue;
            var runningErrorCount = 0;
            string title = null;
            DateTime? pubDate = null;
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);

            var source = GetErrors(log, pageSize, (p, e) => new { PageIndex = p, Entry = e });

            foreach (var entry in from item in source.TakeWhile(e => e.PageIndex < maxPageLimit) 
                                   select item.Entry)
            {
                var error = entry.Error;
                var time = error.Time.ToUniversalTime();
                var day = time.Date;

                //
                // If we're dealing with a new day then break out to a 
                // new channel item, finishing off the previous one.
                //

                if (day < runningDay)
                {
                    if (runningErrorCount > 0)
                    {
                        RenderEnd(writer);
                        Debug.Assert(title != null);
                        Debug.Assert(pubDate != null);
                        yield return RssXml.Item(title, sb.ToString(), pubDate.Value);
                    }

                    runningDay = day;
                    runningErrorCount = 0;
                    pubDate = time;
                    title = string.Format("Digest for {0} ({1})", runningDay.ToString("yyyy-MM-dd"), runningDay.ToLongDateString());
                    sb.Length = 0;
                    RenderStart(writer);
                }

                RenderError(writer, entry, baseUrl);
                runningErrorCount++;
            }

            if (runningErrorCount > 0)
            {
                RenderEnd(writer);
                Debug.Assert(title != null);
                Debug.Assert(pubDate != null);
                yield return RssXml.Item(title, sb.ToString(), pubDate.Value);
            }
        }

        private static IEnumerable<T> GetErrors<T>(ErrorLog log, int pageSize, Func<int, ErrorLogEntry, T> resultor)
        {
            Debug.Assert(log != null);
            Debug.Assert(pageSize > 0);
            Debug.Assert(resultor != null);

            var entries = new List<ErrorLogEntry>(pageSize);
            for (var pageIndex = 0; ; pageIndex++)
            {
                log.GetErrors(pageIndex, pageSize, entries);
                if (!entries.Any())
                    break;
                foreach (var entry in entries)
                    yield return resultor(pageIndex, entry);
                entries.Clear();
            }
        }

        // TODO Consider moving the rest to a Razor template

        private static void RenderStart(TextWriter writer) 
        {
            Debug.Assert(writer != null);

            writer.Write("<ul>");
        }

        private static void RenderError(TextWriter writer, ErrorLogEntry entry, Uri baseUrl) 
        {
            Debug.Assert(writer != null);
            Debug.Assert(entry != null);
            Debug.Assert(baseUrl != null);
            Debug.Assert(baseUrl.IsAbsoluteUri);

            var error = entry.Error;
            writer.Write("<li>");

            var errorType = ErrorDisplay.HumaneExceptionErrorType(error);

            if (errorType.Length > 0)
            {
                var abbreviated = errorType.Length < error.Type.Length;
                        
                if (abbreviated)
                    writer.Write("<span title='{0}'>", Html.Encode(error.Type).ToHtmlString());

                writer.Write(Html.Encode(errorType).ToHtmlString());
                        
                if (abbreviated)
                    writer.Write("</span>");

                writer.Write(": ");
            }

            writer.Write("<a href='{0}'>", Html.Encode(baseUrl + "detail?id=" + Uri.EscapeDataString(entry.Id)).ToHtmlString());
            writer.Write(Html.Encode(error.Message).ToHtmlString());
            writer.Write("</a>");

            writer.Write("</li>");
        }

        private static void RenderEnd(TextWriter writer)
        {
            Debug.Assert(writer != null);

            writer.Write("</li>");
            writer.Flush();
        }
    }
}