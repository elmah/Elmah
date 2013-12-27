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
    using Microsoft.Security.Application;

    // http://msdn.microsoft.com/en-us/library/system.web.ihtmlstring.aspx

    interface IHtmlString
    {
        string ToHtmlString();
    }

    sealed class HtmlString : IHtmlString
    {
        readonly string _html;
        public HtmlString(string html) { _html = html ?? string.Empty; }
        public string ToHtmlString() { return _html; }
        public override string ToString() { return ToHtmlString(); }
    }

    static class Html
    {
        public static readonly IHtmlString Empty = new HtmlString(string.Empty);

        public static IHtmlString Raw(string input)
        {
            return string.IsNullOrEmpty(input) ? Empty : new HtmlString(input);
        }

        public static IHtmlString Encode(object input)
        {
            IHtmlString html;
            return null != (html = input as IHtmlString)
                 ? html
                 : input == null
                 ? Empty
                 : Raw(Encoder.HtmlEncode(input.ToString()));
        }
    }
}