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

[assembly: Elmah.Scc("$Id: ErrorDetailPage.cs 776 2011-01-12 21:09:24Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Linq;
    using MoreLinq;

    #endregion

    /// <summary>
    /// Renders an HTML page displaying details about an error from the 
    /// error log.
    /// </summary>

    sealed partial class ErrorDetailPage
    {
        public ErrorLog ErrorLog { get; set; }

        static HelperResult MarkupStackTrace(string text)
        {
            Debug.Assert(text != null);

            return new HelperResult(writer =>
            {
                if (writer == null) throw new ArgumentNullException("writer");

                var frames = StackTraceParser.Parse
                (
                    text,
                    (idx, len, txt) => new
                    {
                        Index = idx,
                        End   = idx + len,
                        Html  = txt.Length > 0
                              ? Elmah.Html.Encode(txt).ToHtmlString()
                              : string.Empty,
                    },
                    (t, m) => new
                    {
                        Type   = new { t.Index, t.End, Html = "<span class='st-type'>" + t.Html + "</span>" },
                        Method = new { m.Index, m.End, Html = "<span class='st-method'>" + m.Html + "</span>" }
                    },
                    (t, n) => new
                    {
                        Type = new { t.Index, t.End, Html = "<span class='st-param-type'>" + t.Html + "</span>" },
                        Name = new { n.Index, n.End, Html = "<span class='st-param-name'>" + n.Html + "</span>" }
                    },
                    (p, ps) => new { List = p, Parameters = ps.ToArray() },
                    (f, l) => new
                    {
                        File = f.Html.Length > 0
                             ? new { f.Index, f.End, Html = "<span class='st-file'>" + f.Html + "</span>" }
                             : null,
                        Line = l.Html.Length > 0
                             ? new { l.Index, l.End, Html = "<span class='st-line'>" + l.Html + "</span>" }
                             : null,
                    },
                    (f, tm, p, fl) =>
                        from tokens in new[]
                        {
                            new[]
                            {
                                new { f.Index, End = f.Index, Html = "<span class='st-frame'>" },
                                tm.Type,
                                tm.Method,
                                new { p.List.Index, End = p.List.Index, Html = "<span class='params'>" },
                            },
                            from pe in p.Parameters
                            from e in new[] { pe.Type, pe.Name }
                            select e,
                            new[]
                            {
                                new { Index = p.List.End, p.List.End, Html = "</span>" },
                                fl.File,
                                fl.Line,
                                new { Index = f.End, f.End, Html = "</span>" },
                            },
                        }
                        from token in tokens
                        where token != null
                        select token
                );

                var markups =
                    from token in Enumerable.Repeat(new { Index = 0, End = 0, Html = string.Empty }, 1)
                                            .Concat(from tokens in frames from token in tokens select token)
                                            .Pairwise((prev, curr) => new { Previous = prev, Current = curr })
                    from m in new object[]
                    {
                        text.Substring(token.Previous.End, token.Current.Index - token.Previous.End),
                        Elmah.Html.Raw(token.Current.Html)
                    }
                    select Elmah.Html.Encode(m).ToHtmlString() into m
                    where m.Length > 0
                    select m;

                writer.Write(markups.ToDelimitedString(string.Empty));
            });
        }
    }
}
