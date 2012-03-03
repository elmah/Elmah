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

[assembly: Elmah.Scc("$Id: StringFormatter.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Globalization;
    using System.Text;
    using System.Web;

    #endregion

    public delegate string StringFormatTokenBindingHandler(string token, object[] args, IFormatProvider provider);

    /// <summary>
    /// Helper class for formatting templated strings with supplied replacements.
    /// </summary>

    public sealed class StringFormatter
    {
        public static readonly StringFormatTokenBindingHandler DefaultTokenBinder = new StringFormatTokenBindingHandler(BindFormatToken);

        /// <summary>
        /// Replaces each format item in a specified string with the text 
        /// equivalent of a corresponding object's value. 
        /// </summary>

        public static string Format(string format, params object[] args)
        {
            return Format(format, null, null, args);
        }

        public static string Format(string format, IFormatProvider provider, params object[] args)
        {
            return Format(format, provider, null, args);
        }

        public static string Format(string format, StringFormatTokenBindingHandler binder, params object[] args)
        {
            return Format(format, null, binder, args);
        }

        public static string Format(string format,
            IFormatProvider provider, StringFormatTokenBindingHandler binder, params object[] args)
        {
            return FormatImpl(format, provider, binder != null ? binder : DefaultTokenBinder, args);
        }

        private static string FormatImpl(string format,
            IFormatProvider provider, StringFormatTokenBindingHandler binder, params object[] args)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            Debug.Assert(binder != null);

            //
            // Following is a slightly modified version of the parser from
            // Henri Weichers that was presented at:
            // http://haacked.com/archive/2009/01/14/named-formats-redux.aspx
            // See also the following comment about the modifications: 
            // http://haacked.com/archive/2009/01/14/named-formats-redux.aspx#70485
            //

            StringBuilder result = new StringBuilder(format.Length * 2);
            StringBuilder token = new StringBuilder();

            CharEnumerator e = format.GetEnumerator();
            while (e.MoveNext())
            {
                char ch = e.Current;
                if (ch == '{')
                {
                    while (true)
                    {
                        if (!e.MoveNext())
                            throw new FormatException();

                        ch = e.Current;
                        if (ch == '}')
                        {
                            if (token.Length == 0)
                                throw new FormatException();

                            result.Append(binder(token.ToString(), args, provider));
                            token.Length = 0;
                            break;
                        }
                        if (ch == '{')
                        {
                            result.Append(ch);
                            break;
                        }
                        token.Append(ch);
                    }
                }
                else if (ch == '}')
                {
                    if (!e.MoveNext() || e.Current != '}')
                        throw new FormatException();
                    result.Append('}');
                }
                else
                {
                    result.Append(ch);
                }
            }

            return result.ToString();
        }

        public static string BindFormatToken(string token, object[] args, IFormatProvider provider)
        {
            if (token == null)
                throw new ArgumentNullException("token");
            if (token.Length == 0)
                throw new ArgumentException("Format token cannot be an empty string.", "token");
            if (args == null)
                throw new ArgumentNullException("args");
            if (args.Length == 0)
                throw new ArgumentException("Missing replacement arguments.", "args");

            object source = args[0];
            int dotIndex = token.IndexOf('.');
            int sourceIndex;
            if (dotIndex > 0 && 0 <= (sourceIndex = TryParseUnsignedInteger(token.Substring(0, dotIndex))))
            {
                source = args[sourceIndex];
                token = token.Substring(dotIndex + 1);
            }

            string format = string.Empty;

            int colonIndex = token.IndexOf(':');
            if (colonIndex > 0)
            {
                format = "{0:" + token.Substring(colonIndex + 1) + "}";
                token = token.Substring(0, colonIndex);
            }

            if ((sourceIndex = TryParseUnsignedInteger(token)) >= 0)
            {
                source = args[sourceIndex];
                token = null;
            }

            object result;

            try
            {
                result = DataBinder.Eval(source, token);
                if (result == null)
                    result = string.Empty;
            }
            catch (HttpException e) // Map silly exception type from DataBinder.Eval
            {
                throw new FormatException(e.Message, e);
            }

            return Mask.NullString(format).Length > 0
                 ? string.Format(provider, format, result)
                 : result.ToString();
        }

        public static int TryParseUnsignedInteger(string str)
        {
#if !NET_1_1 && !NET_1_0
            int result;
            return int.TryParse(str, NumberStyles.None, CultureInfo.InvariantCulture, out result) 
                 ? result : -1;
#else
            return System.Text.RegularExpressions.Regex.IsMatch(str, "^[0-9]+$")
                 ? int.Parse(str, NumberStyles.None, CultureInfo.InvariantCulture)
                 : -1;
#endif
        }

        private StringFormatter() { }
    }
}