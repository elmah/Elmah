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
    using System.Linq;
    using System.Text.RegularExpressions;

    #endregion

    static class StackTraceParser
    {
        static readonly Regex _regex = new Regex(@"
            ^
            \s*
            \w+ \s+ 
            (?<frame>
                (?<type> .+ ) \.
                (?<method> .+? ) \s*
                (?<params>  \( ( \s* \)
                               |        (?<pt> .+?) \s+ (?<pn> .+?) 
                                 (, \s* (?<pt> .+?) \s+ (?<pn> .+?) )* \) ) )
                ( \s+
                    ( # Microsoft .NET stack traces
                    \w+ \s+ 
                    (?<file> [a-z] \: .+? ) 
                    \: \w+ \s+ 
                    (?<line> [0-9]+ ) \p{P}?  
                    | # Mono stack traces
                    \[0x[0-9a-f]+\] \s+ \w+ \s+ 
                    <(?<file> [^>]+ )>
                    :(?<line> [0-9]+ )
                    )
                )?
            )
            \s* 
            $",
            RegexOptions.IgnoreCase
            | RegexOptions.Multiline
            | RegexOptions.ExplicitCapture
            | RegexOptions.CultureInvariant
            | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Compiled);

        public static IEnumerable<TFrame> Parse<TToken, TMethod, TParameters, TParameter, TSourceLocation, TFrame>(
            string text, 
            Func<int, int, string, TToken> tokenSelector,
            Func<TToken, TToken, TMethod> methodSelector,
            Func<TToken, TToken, TParameter> parameterSelector,
            Func<TToken, IEnumerable<TParameter>, TParameters> parametersSelector,
            Func<TToken, TToken, TSourceLocation> sourceLocationSelector,
            Func<TToken, TMethod, TParameters, TSourceLocation, TFrame> selector)
        {
            return from Match m in _regex.Matches(text)
                   select m.Groups into groups
                   let pt = groups["pt"].Captures
                   let pn = groups["pn"].Captures
                   select selector(Token(groups["frame"], tokenSelector),
                                   methodSelector(
                                       Token(groups["type"], tokenSelector), 
                                       Token(groups["method"], tokenSelector)), 
                                   parametersSelector(
                                       Token(groups["params"], tokenSelector), 
                                       from i in Enumerable.Range(0, pt.Count)
                                       select parameterSelector(Token(pt[i], tokenSelector), 
                                                                Token(pn[i], tokenSelector))),
                                   sourceLocationSelector(Token(groups["file"], tokenSelector), 
                                                          Token(groups["line"], tokenSelector)));
        }

        static T Token<T>(Capture capture, Func<int, int, string, T> tokenSelector)
        {
            return tokenSelector(capture.Index, capture.Length, capture.Value);
        }
    }
}