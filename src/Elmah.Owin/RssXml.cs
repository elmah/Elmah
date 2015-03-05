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

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Xml.Linq;

    #endregion

    static class RssXml
    {
        public static XElement Rss(string title, string link, string description, IEnumerable<XElement> items)
        {
            return
                new XElement("rss",
                    new XAttribute("version", "0.91"), // http://backend.userland.com/rss091
                    new XElement("channel",
                        new XElement("title", title),
                        new XElement("link", link),
                        new XElement("description", description),
                        new XElement("language", "en"),
                        items));
        }

        public static XElement Item(string title, string description, DateTime pubDate)
        {
            return Item(title, description, pubDate, null);
        }

        public static XElement Item(string title, string description, DateTime pubDate, string link)
        {
            return
                new XElement("item",
                    new XElement("title", title),
                    new XElement("description", description),
                    new XElement("pubDate", pubDate.ToUniversalTime().ToString("r", DateTimeFormatInfo.InvariantInfo)),
                    link != null ? new XElement("link", link) : null);
        }
    }
}