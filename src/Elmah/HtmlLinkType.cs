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

[assembly: Elmah.Scc("$Id: HtmlLinkType.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    /// <summary>
    /// User agents, search engines, etc. may interpret and use these link 
    /// types in a variety of ways. For example, user agents may provide 
    /// access to linked documents through a navigation bar.
    /// </summary>
    /// <remarks>
    /// See <a href="http://www.w3.org/TR/html401/types.html#type-links">6.12 Link types</a>
    /// for more information.
    /// </remarks>
    
    internal sealed class HtmlLinkType
    {
        // Designates  substitute  versions  for the document in which the
        // link occurs. When used together with the lang attribute, it
        // implies  a  translated  version  of  the  document.  When  used
        // together  with  the  media  attribute, it implies a version
        // designed for a different medium (or media).

        public const string Alternate = "alternate";

        // Refers   to  an  external  style  sheet.  See  the  section  on
        // external  style  sheets  for details. This is used together
        // with  the  link  type "Alternate" for user-selectable alternate
        // style sheets.

        public const string Stylesheet = "stylesheet";

        // Refers to the first document in a collection of documents. This
        // link  type tells search engines which document is considered by
        // the author to be the starting point of the collection.

        public const string Start = "start";

        // Refers  to the next document in a linear sequence of documents.
        // User  agents  may  choose  to  preload  the "next" document, to
        // reduce the perceived load time.

        public const string Next = "next";

        // Refers  to  the  previous  document  in  an  ordered  series of
        // documents.   Some   user   agents   also  support  the  synonym
        // "Previous".

        public const string Prev = "prev";

        // Refers  to a document serving as a table of contents. Some user
        // agents also support the synonym ToC (from "Table of Contents").

        public const string Contents = "contents";

        // Refers  to  a  document  providing  an  index  for  the current
        // document.

        public const string Index = "index";

        // Refers to a document providing a glossary of terms that pertain
        // to the current document.

        public const string Glossary = "glossary";

        // Refers to a copyright statement for the current document.

        public const string Copyright = "copyright";

        // Refers  to  a  document serving as a chapter in a collection of
        // documents.

        public const string Chapter = "chapter";

        // Refers  to  a  document serving as a section in a collection of
        // documents.

        public const string Section = "section";

        // Refers to a document serving as a subsection in a collection of
        // documents.

        public const string Subsection = "subsection";

        // Refers  to a document serving as an appendix in a collection of
        // documents.

        public const string Appendix = "appendix";

        // Refers  to a document offering help (more information, links to
        //  other sources information, etc.)

        public const string Help = "help";

        // Refers to a bookmark. A bookmark is a link to a key entry point
        // within  an  extended  document.  The title attribute may be
        // used,  for  example,  to  label the bookmark. Note that several
        // bookmarks may be defined in each document.

        public const string Bookmark = "bookmark";

        private HtmlLinkType() {}
    }
}
