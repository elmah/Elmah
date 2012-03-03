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

[assembly: Elmah.Scc("$Id: SpeedBar.cs 745 2010-11-25 00:14:43Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web;
    using System.Web.UI;

    #endregion

    internal sealed class SpeedBar
    {
        public static readonly ItemTemplate Home = new ItemTemplate("Errors", "List of logged errors", "{0}");
        public static readonly ItemTemplate RssFeed = new ItemTemplate("RSS Feed", "RSS feed of recent errors", "{0}/rss");
        public static readonly ItemTemplate RssDigestFeed = new ItemTemplate("RSS Digest", "RSS feed of errors within recent days", "{0}/digestrss");
        public static readonly ItemTemplate DownloadLog = new ItemTemplate("Download Log", "Download the entire log as CSV", "{0}/download");
        public static readonly FormattedItem Help = new FormattedItem("Help", "Documentation, discussions, issues and more", "http://elmah.googlecode.com/");
        public static readonly ItemTemplate About = new ItemTemplate("About", "Information about this version and build", "{0}/about");

        public static void Render(HtmlTextWriter writer, params FormattedItem[] items)
        {
            Debug.Assert(writer != null);

            if (items == null || items.Length == 0)
                return;

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "SpeedList");
            writer.RenderBeginTag(HtmlTextWriterTag.Ul);

            foreach (FormattedItem item in items)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                item.Render(writer);
                writer.RenderEndTag( /* li */);
            }

            writer.RenderEndTag( /* ul */);
        }

        private SpeedBar() {}

        [ Serializable ]
        public abstract class Item
        {
            private readonly string _text;
            private readonly string _title;
            private readonly string _href;

            public Item(string text, string title, string href)
            {
                _text = Mask.NullString(text);
                _title = Mask.NullString(title);
                _href = Mask.NullString(href);
            }

            public string Text  { get { return _text; } }
            public string Title { get { return _title; } }
            public string Href  { get { return _href; } }

            public override string ToString()
            {
                return Text;
            }
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

            public void Render(HtmlTextWriter writer)
            {
                Debug.Assert(writer != null);

                writer.AddAttribute(HtmlTextWriterAttribute.Href, Href);
                writer.AddAttribute(HtmlTextWriterAttribute.Title, Title);
                writer.RenderBeginTag(HtmlTextWriterTag.A);
                HttpUtility.HtmlEncode(Text, writer);
                writer.RenderEndTag( /* a */);
            }
        }
    }
}