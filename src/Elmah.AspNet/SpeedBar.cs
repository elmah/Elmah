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

[assembly: Elmah.Scc("$Id: SpeedBar.cs 776 2011-01-12 21:09:24Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;

    #endregion

    class SpeedBar
    {
        public static readonly ItemTemplate Home = new ItemTemplate("Errors", "List of logged errors", "{0}");
        public static readonly ItemTemplate RssFeed = new ItemTemplate("RSS Feed", "RSS feed of recent errors", "{0}/rss");
        public static readonly ItemTemplate RssDigestFeed = new ItemTemplate("RSS Digest", "RSS feed of errors within recent days", "{0}/digestrss");
        public static readonly ItemTemplate DownloadLog = new ItemTemplate("Download Log", "Download the entire log as CSV", "{0}/download");
        public static readonly FormattedItem Help = new FormattedItem("Help", "Documentation, discussions, issues and more", "https://elmah.github.io/");
        public static readonly ItemTemplate About = new ItemTemplate("About", "Information about this version and build", "{0}/about");

        public static HelperResult Render(params FormattedItem[] items)
        {
            return new HelperResult(writer =>
            {
                if (writer == null) throw new ArgumentNullException("writer");

                if (items == null || items.Length == 0)
                    return;

                writer.Write("<ul id='SpeedList' class='nav'>");
                foreach (var item in items)
                {
                    writer.Write("<li>");
                    foreach (var part in item.Render())
                        writer.Write(Html.Encode(part).ToHtmlString());
                    writer.Write("</li>");
                }
                writer.Write("</ul>");
            });
        }

        [ Serializable ]
        public abstract class Item
        {
            protected Item(string text, string title, string href)
            {
                Text = text ?? string.Empty;
                Title = title ?? string.Empty;
                Href = href ?? string.Empty;
            }

            public string Text { get; private set; }
            public string Title { get; private set; }
            public string Href { get; private set; }

            public override string ToString() { return Text; }
        }

        [ Serializable ]
        public sealed class ItemTemplate : Item
        {
            public ItemTemplate(string text, string title, string href) : 
                base(text, title, href) {}

            public FormattedItem Format(string url)
            {
                return new FormattedItem(Text, Title, string.Format(Href, url));
            }
        }

        [ Serializable ]
        public sealed class FormattedItem : Item
        {
            public FormattedItem(string text, string title, string href) : 
                base(text, title, href) {}

            public IEnumerable<object> Render()
            {
                yield return Html.Raw(@"<a ");
                yield return Html.Raw(@" href='");
                yield return Href;
                yield return Html.Raw(@"' title='");
                yield return Title;
                yield return Html.Raw(@"'>");
                yield return Text;
                yield return Html.Raw(@"</a>");
            }
        }
    }
}