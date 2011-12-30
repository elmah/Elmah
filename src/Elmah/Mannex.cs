#region License, Terms and Author(s)
//
// Mannex - Extension methods for .NET
// Copyright (c) 2009 Atif Aziz. All rights reserved.
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

namespace Mannex.Collections.Generic
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>

    static partial class DictionaryExtensions
    {
        /// <summary>
        /// Finds the value for a key, returning the default value for 
        /// <typeparamref name="TKey"/> if the key is not present.
        /// </summary>

        [DebuggerStepThrough]
        public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            return Find(dict, key, default(TValue));
        }

        /// <summary>
        /// Finds the value for a key, returning a given default value for 
        /// <typeparamref name="TKey"/> if the key is not present.
        /// </summary>

        [DebuggerStepThrough]
        public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue @default)
        {
            if (dict == null) throw new ArgumentNullException("dict");
            TValue value;
            return dict.TryGetValue(key, out value) ? value : @default;
        }
    }
}

namespace Mannex.Collections.Generic
{
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods for pairing keys and values as 
    /// <see cref="KeyValuePair{TKey,TValue}"/>.
    /// </summary>

    static partial class PairingExtensions
    {
        /// <summary>
        /// Pairs a value with a key.
        /// </summary>

        public static KeyValuePair<TKey, TValue> AsKeyTo<TKey, TValue>(this TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }
}

namespace Mannex.Collections.Specialized
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Generic;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="NameValueCollection"/>.
    /// </summary>

    static partial class NameValueCollectionExtensions
    {
        /// <summary>
        /// Create a <see cref="NameValueCollection"/> from a sequence of
        /// <see cref="KeyValuePair{String,String}"/>.
        /// </summary>

        public static NameValueCollection ToNameValueCollection(
            this IEnumerable<KeyValuePair<string, string>> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            var collection = CreateCollection(source as ICollection<KeyValuePair<string, string>>);
            collection.Add(source);
            return collection;
        }

        /// <summary>
        /// Create a <see cref="NameValueCollection"/> from an <see cref="IEnumerable{T}"/>
        /// given a function to select the name and value of each <typeparamref name="T"/>
        /// in the source sequence.
        /// </summary>

        public static NameValueCollection ToNameValueCollection<T>(
            this IEnumerable<T> source,
            Func<T, string> nameSelector,
            Func<T, IEnumerable<string>> valuesSelector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (nameSelector == null) throw new ArgumentNullException("nameSelector");
            if (valuesSelector == null) throw new ArgumentNullException("valuesSelector");

            var collection = CreateCollection(source as ICollection<T>);
            collection.Add(source, nameSelector, valuesSelector);
            return collection;
        }

        /// <summary>
        /// Adds items from an <see cref="IEnumerable{T}"/>,
        /// given a function to select the name and value of each <typeparamref name="T"/>
        /// in the source sequence.
        /// </summary>

        public static void Add<T>(
            this NameValueCollection collection,
            IEnumerable<T> source,
            Func<T, string> nameSelector,
            Func<T, IEnumerable<string>> valuesSelector)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (source == null) throw new ArgumentNullException("source");
            if (nameSelector == null) throw new ArgumentNullException("nameSelector");
            if (valuesSelector == null) throw new ArgumentNullException("valuesSelector");

            var items = from item in source
                        from value in valuesSelector(item)
                        select nameSelector(item).AsKeyTo(value);

            collection.Add(items);
        }

        /// <summary>
        /// Adds items from a sequence of 
        /// <see cref="KeyValuePair{String,String}"/>.
        /// </summary>

        public static void Add(
            this NameValueCollection collection,
            IEnumerable<KeyValuePair<string, string>> source)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (source == null) throw new ArgumentNullException("source");

            foreach (var item in source)
                collection.Add(item.Key, item.Value);
        }

        private static NameValueCollection CreateCollection<T>(ICollection<T> collection)
        {
            return collection != null
                 ? new NameValueCollection(collection.Count)
                 : new NameValueCollection();
        }
    }
}

namespace Mannex
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Text.RegularExpressions;
    using IO;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="string"/>.
    /// </summary>

    static partial class StringExtensions
    {
        /// <summary>
        /// Masks an empty string with a given mask such that the result
        /// is never an empty string. If the input string is null or
        /// empty then it is masked, otherwise the original is returned.
        /// </summary>
        /// <remarks>
        /// Use this method to guarantee that you never get an empty
        /// string. Bear in mind, however, that if the mask itself is an 
        /// empty string then this method could yield an empty string!
        /// </remarks>

        [DebuggerStepThrough]
        public static string MaskEmpty(this string str, string mask)
        {
            return !string.IsNullOrEmpty(str) ? str : mask;
        }

        /// <summary>
        /// Returns a section of a string from a give starting point on.
        /// </summary>
        /// <remarks>
        /// If <paramref name="start"/> is negative, it is  treated as 
        /// <c>length</c> + <paramref name="start" /> where <c>length</c> 
        /// is the length of the string. If <paramref name="start"/>
        /// is greater or equal to the length of the string then 
        /// no characters are copied to the new string.
        /// </remarks>

        [DebuggerStepThrough]
        public static string Slice(this string str, int start)
        {
            return Slice(str, start, null);
        }

        /// <summary>
        /// Returns a section of a string.
        /// </summary>
        /// <remarks>
        /// This method copies up to, but not including, the element
        /// indicated by <paramref name="end"/>. If <paramref name="start"/> 
        /// is negative, it is  treated as <c>length</c> + <paramref name="start" /> 
        /// where <c>length</c> is the length of the string. If 
        /// <paramref name="end"/> is negative, it is treated as <c>length</c> + 
        /// <paramref name="end"/> where <c>length</c> is the length of the
        /// string. If <paramref name="end"/> occurs before <paramref name="start"/>, 
        /// no characters are copied to the new string.
        /// </remarks>

        [DebuggerStepThrough]
        public static string Slice(this string str, int start, int? end)
        {
            if (str == null) throw new ArgumentNullException("str");
            return SliceImpl(str, start, end ?? str.Length);
        }

        private static string SliceImpl(this string str, int start, int end)
        {
            if (str == null) throw new ArgumentNullException("str");
            var length = str.Length;

            if (start < 0)
            {
                start = length + start;
                if (start < 0)
                    start = 0;
            }
            else
            {
                if (start > length)
                    start = length;
            }

            if (end < 0)
            {
                end = length + end;
                if (end < 0)
                    end = 0;
            }
            else
            {
                if (end > length)
                    end = length;
            }

            var sliceLength = end - start;

            return sliceLength > 0 ?
                str.Substring(start, sliceLength) : string.Empty;
        }
        
        /// <summary>
        /// Embeds string into <paramref name="target"/>, using {0} 
        /// within <paramref name="target"/> as the point of embedding.
        /// </summary>

        public static string Embed(this string str, string target)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (target == null) throw new ArgumentNullException("target");
            return string.Format(target, str);
        }

        /// <summary>
        /// Wraps string between two other string where the first
        /// indicates the left side and the second indicates the
        /// right side.
        /// </summary>

        public static string Wrap(this string str, string lhs, string rhs)
        {
            if (str == null) throw new ArgumentNullException("str");
            return lhs + str + rhs;
        }

        /// <summary>
        /// Enquotes string with <paramref name="quote"/>, escaping occurences
        /// of <paramref name="quote"/> itself with <paramref name="escape"/>.
        /// </summary>

        public static string Quote(this string str, string quote, string escape)
        {
            if (str == null) throw new ArgumentNullException("str");
            StringBuilder sb = null;
            var start = 0;
            int index;
            while ((index = str.IndexOf(quote, start)) >= 0)
            {
                if (sb == null)
                    sb = new StringBuilder(str.Length + 10).Append(quote);
                sb.Append(str, start, index - start);
                sb.Append(escape);
                start = index + quote.Length;
            }
            return sb != null 
                 ? sb.Append(str, start, str.Length - start).Append(quote).ToString() 
                 : str.Wrap(quote, quote);
        }

        /// <summary>
        /// Format string using <paramref name="args"/> as sources for
        /// replacements and a function, <paramref name="binder"/>, that
        /// determines how to bind and resolve replacement tokens.
        /// </summary>

        public static string FormatWith(this string format, 
            Func<string, object[], IFormatProvider, string> binder, params object[] args)
        {
            return format.FormatWith(null, binder, args);
        }

        /// <summary>
        /// Format string using <paramref name="args"/> as sources for
        /// replacements and a function, <paramref name="binder"/>, that
        /// determines how to bind and resolve replacement tokens. In 
        /// addition, <paramref name="provider"/> is used for cultural
        /// formatting.
        /// </summary>

        public static string FormatWith(this string format,
            IFormatProvider provider, Func<string, object[], IFormatProvider, string> binder, params object[] args)
        {
            if (format == null) throw new ArgumentNullException("format");
            if (binder == null) throw new ArgumentNullException("binder");

            Debug.Assert(binder != null);

            var result = new StringBuilder(format.Length * 2);
            var token = new StringBuilder();

            var e = format.GetEnumerator();
            while (e.MoveNext())
            {
                var ch = e.Current;
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

        /// <summary>
        /// Splits a string into a pair using a specified character to 
        /// separate the two.
        /// </summary>
        /// <remarks>
        /// Neither half in the resulting pair is ever <c>null</c>.
        /// </remarks>

        public static T Split<T>(this string str, char separator, Func<string, string, T> resultFunc)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (resultFunc == null) throw new ArgumentNullException("resultFunc");
            return SplitRemoving(str, str.IndexOf(separator), 1, resultFunc);
        }

        /// <summary>
        /// Splits a string into three parts using any of a specified set of 
        /// characters to separate the three.
        /// </summary>
        /// <remarks>
        /// None of the resulting parts is ever <c>null</c>.
        /// </remarks>
        
        public static T Split<T>(this string str, char separator, Func<string, string, string, T> resultFunc)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (resultFunc == null) throw new ArgumentNullException("resultFunc");
            return str.Split(separator, (a, rest) => rest.Split(separator, (b, c) => resultFunc(a, b, c)));
        }

        /// <summary>
        /// Splits a string into four parts using any of a specified set of 
        /// characters to separate the four.
        /// </summary>
        /// <remarks>
        /// None of the resulting parts is ever <c>null</c>.
        /// </remarks>

        public static T Split<T>(this string str, char separator, Func<string, string, string, string, T> resultFunc)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (resultFunc == null) throw new ArgumentNullException("resultFunc");
            return str.Split(separator, (a, b, rest) => rest.Split(separator, (c, d) => resultFunc(a, b, c, d)));
        }

        /// <summary>
        /// Splits a string into a pair using any of a specified set of 
        /// characters to separate the two.
        /// </summary>
        /// <remarks>
        /// Neither half in the resulting pair is ever <c>null</c>.
        /// </remarks>

        public static T Split<T>(this string str, char[] separators, Func<string, string, T> resultFunc)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (resultFunc == null) throw new ArgumentNullException("resultFunc");

            return separators == null || separators.Length == 0
                 ? resultFunc(str, string.Empty)
                 : SplitRemoving(str, str.IndexOfAny(separators), 1, resultFunc);
        }

        /// <summary>
        /// Splits a string into three parts using any of a specified set of 
        /// characters to separate the three.
        /// </summary>
        /// <remarks>
        /// None of the resulting parts is ever <c>null</c>.
        /// </remarks>

        public static T Split<T>(this string str, char[] separators, Func<string, string, string, T> resultFunc)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (resultFunc == null) throw new ArgumentNullException("resultFunc");
            return str.Split(separators, (a, rest) => rest.Split(separators, (b, c) => resultFunc(a, b, c)));
        }

        /// <summary>
        /// Splits a string into four parts using any of a specified set of 
        /// characters to separate the four.
        /// </summary>
        /// <remarks>
        /// None of the resulting parts is ever <c>null</c>.
        /// </remarks>

        public static T Split<T>(this string str, char[] separators, Func<string, string, string, string, T> resultFunc)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (resultFunc == null) throw new ArgumentNullException("resultFunc");
            return str.Split(separators, (a, b, rest) => rest.Split(separators, (c, d) => resultFunc(a, b, c, d)));
        }

        /// <summary>
        /// Splits a string into a pair by removing a portion of the string.
        /// </summary>
        /// <remarks>
        /// Neither half in the resulting pair is ever <c>null</c>.
        /// </remarks>
        
        private static T SplitRemoving<T>(string str, int index, int count, Func<string, string, T> resultFunc)
        {
            Debug.Assert(str != null);
            Debug.Assert(count > 0);
            Debug.Assert(resultFunc != null);

            var a = index < 0 
                  ? str 
                  : str.Substring(0, index);
            
            var b = index < 0 || index + 1 >= str.Length 
                  ? string.Empty 
                  : str.Substring(index + count);
            
            return resultFunc(a, b);
        }

        /// <summary>
        /// Splits string into lines where a line is terminated 
        /// by CR and LF, or just CR or just LF.
        /// </summary>
        /// <remarks>
        /// This method uses deferred exection.
        /// </remarks>

        public static IEnumerable<string> SplitIntoLines(this string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            return SplitIntoLinesImpl(str);
        }
 
        private static IEnumerable<string> SplitIntoLinesImpl(string str)
        {
            using (var reader = str.Read())
            foreach (var line in reader.ReadLines())
                yield return line;
        }

        /// <summary>
        /// Collapses all sequences of white space (as deifned by Unicode 
        /// and identified by <see cref="char.IsWhiteSpace(char)"/>) to a 
        /// single space and trims all leading and trailing white space.
        /// </summary>

        public static string NormalizeWhiteSpace(this string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            return Regex.Replace(str, @"\s+", " ").Trim();
        }
    }
}

namespace Mannex.Text.RegularExpressions
{
    #region Imports

    using System;
    using System.Text.RegularExpressions;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="Match"/> that help with regular 
    /// expression matching.
    /// </summary>

    static partial class MatchExtensions
    {
        private static readonly Func<Match, string, int, Group> NamedBinder = (match, name, num) => match.Groups[name];
        private static readonly Func<Match, string, int, Group> NumberBinder = (match, name, num) => match.Groups[num];

        /// <summary>
        /// Binds match group names to corresponding parameter names of a
        /// method responsible for creating the result of the match.
        /// </summary>
        /// <remarks>
        /// <paramref name="resultSelector"/> is called to generate a 
        /// result irrespective of whether the match was successful or not.
        /// </remarks>

        public static TResult Bind<TResult>(this Match match,
            Func<Group, TResult> resultSelector)
        {
            return Bind(match, NamedBinder, resultSelector);
        }

        /// <summary>
        /// Binds match group numbers to corresponding parameter positions 
        /// of a method responsible for creating the result of the match.
        /// </summary>
        /// <remarks>
        /// <paramref name="resultSelector"/> is called to generate a 
        /// result irrespective of whether the match was successful or not.
        /// </remarks>

        public static TResult BindNum<TResult>(this Match match,
            Func<Group, TResult> resultSelector)
        {
            return Bind(match, NumberBinder, resultSelector);
        }

        /// <summary>
        /// Binds match group names to corresponding parameter names of a
        /// method responsible for creating the result of the match.
        /// </summary>
        /// <remarks>
        /// <paramref name="resultSelector"/> is called to generate a 
        /// result irrespective of whether the match was successful or not.
        /// </remarks>

        public static TResult Bind<TResult>(this Match match,
            Func<Group, Group, TResult> resultSelector)
        {
            return Bind(match, NamedBinder, resultSelector);
        }

        /// <summary>
        /// Binds match group numbers to corresponding parameter positions 
        /// of a method responsible for creating the result of the match.
        /// </summary>
        /// <remarks>
        /// <paramref name="resultSelector"/> is called to generate a 
        /// result irrespective of whether the match was successful or not.
        /// </remarks>

        public static TResult BindNum<TResult>(this Match match,
            Func<Group, Group, TResult> resultSelector)
        {
            return Bind(match, NumberBinder, resultSelector);
        }

        private static TResult Bind<TResult>(Match match, 
            Func<Match, string, int, Group> groupSelector, 
            Func<Group, Group, TResult> resultSelector)
        {
            return Bind(match, groupSelector, groupSelector, resultSelector);
        }

        /// <summary>
        /// Binds match group names to corresponding parameter names of a
        /// method responsible for creating the result of the match.
        /// </summary>
        /// <remarks>
        /// <paramref name="resultSelector"/> is called to generate a 
        /// result irrespective of whether the match was successful or not.
        /// </remarks>

        public static TResult Bind<TResult>(this Match match,
            Func<Group, Group, Group, TResult> resultSelector)
        {
            return Bind(match, NamedBinder, resultSelector);
        }

        /// <summary>
        /// Binds match group numbers to corresponding parameter positions 
        /// of a method responsible for creating the result of the match.
        /// </summary>
        /// <remarks>
        /// <paramref name="resultSelector"/> is called to generate a 
        /// result irrespective of whether the match was successful or not.
        /// </remarks>
        
        public static TResult BindNum<TResult>(this Match match,
            Func<Group, Group, Group, TResult> resultSelector)
        {
            return Bind(match, NumberBinder, resultSelector);
        }

        private static TResult Bind<TResult>(Match match,
            Func<Match, string, int, Group> groupSelector,
            Func<Group, Group, Group, TResult> resultSelector)
        {
            return Bind(match, groupSelector, groupSelector, groupSelector, resultSelector);
        }

        private static TResult Bind<T1, TResult>(Match match,
            Func<Match, string, int, T1> selector,
            Func<T1, TResult> resultSelector)
        {
            return Bind<T1, object, object, object, TResult>(match,
                       selector, null, null, null,
                       resultSelector, null, null, null);
        }

        private static TResult Bind<T1, T2, TResult>(Match match,
            Func<Match, string, int, T1> selector1,
            Func<Match, string, int, T2> selector2,
            Func<T1, T2, TResult> resultSelector)
        {
            return Bind<T1, T2, object, object, TResult>(match,
                       selector1, selector2, null, null,
                       null, resultSelector, null, null);
        }

        private static TResult Bind<T1, T2, T3, TResult>(Match match,
            Func<Match, string, int, T1> selector1,
            Func<Match, string, int, T2> selector2,
            Func<Match, string, int, T3> selector3,
            Func<T1, T2, T3, TResult> resultSelector)
        {
            return Bind<T1, T2, T3, object, TResult>(match,
                       selector1, selector2, selector3, null,
                       null, null, resultSelector, null);
        }

        internal static TResult Bind<T1, T2, T3, T4, TResult>(
            Match match,
            Func<Match, string, int, T1> s1,
            Func<Match, string, int, T2> s2,
            Func<Match, string, int, T3> s3,
            Func<Match, string, int, T4> s4,
            Func<T1, TResult> r1,
            Func<T1, T2, TResult> r2,
            Func<T1, T2, T3, TResult> r3,
            Func<T1, T2, T3, T4, TResult> r4)
        {
            if (match == null) throw new ArgumentNullException("match");

            var d = r1 ?? r2 ?? r3 ?? (Delegate)r4;
            if (d == null)
                throw new ArgumentNullException("resultSelector");

            var ps = d.Method.GetParameters();
            var count = ps.Length;

            if (s1 == null) throw new ArgumentNullException(count == 1 ? "selector" : "selector1");
            if (count > 1 && s2 == null) throw new ArgumentNullException("selector2");
            if (count > 2 && s3 == null) throw new ArgumentNullException("selector3");
            if (count > 3 && s4 == null) throw new ArgumentNullException("selector4");

            return r1 != null
                 ? r1(s1(match, ps[0].Name, ps[0].Position + 1))
                 
                 : r2 != null
                 ? r2(s1(match, ps[0].Name, ps[0].Position + 1),
                      s2(match, ps[1].Name, ps[1].Position + 1))

                 : r3 != null
                 ? r3(s1(match, ps[0].Name, ps[0].Position + 1),
                      s2(match, ps[1].Name, ps[1].Position + 1),
                      s3(match, ps[2].Name, ps[2].Position + 1))
                 
                 : r4(s1(match, ps[0].Name, ps[0].Position + 1),
                      s2(match, ps[1].Name, ps[1].Position + 1),
                      s3(match, ps[2].Name, ps[2].Position + 1),
                      s4(match, ps[3].Name, ps[3].Position + 1));
        }
    }
}

namespace Mannex.Text.RegularExpressions
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="string"/> that help with regular 
    /// expression matching.
    /// </summary>

    static partial class StringExtensions
    {
        /// <summary>
        /// Indicates whether the string contains a match for the regular 
        /// expression pattern specified as an argument.
        /// </summary>

        public static bool IsMatch(this string str, string pattern)
        {
            return str.IsMatch(pattern, RegexOptions.None);
        }

        /// <summary>
        /// Indicates whether the string contains a match for the regular 
        /// expression pattern specified as an argument  along with 
        /// matching options.
        /// </summary>

        public static bool IsMatch(this string str, string pattern, RegexOptions options)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (pattern == null) throw new ArgumentNullException("pattern");
            return Regex.IsMatch(str, pattern, options);
        }

        /// <summary>
        /// Searches string for an occurrence of the regular expression 
        /// specified as an argument.
        /// </summary>

        public static Match Match(this string str, string pattern)
        {
            return str.Match(pattern, RegexOptions.None);
        }

        /// <summary>
        /// Searches string for an occurrence of the regular expression 
        /// specified as an argument along with matching options.
        /// </summary>

        public static Match Match(this string str, string pattern, RegexOptions options)
        {
            return str.Match(pattern, options, m => m);
        }

        /// <summary>
        /// Searches string for an occurrence of the regular expression 
        /// specified as an argument along with matching options.
        /// </summary>

        public static T Match<T>(this string str, string pattern, Func<Match, T> selector)
        {
            return str.Match(pattern, RegexOptions.None, selector);
        }

        /// <summary>
        /// Searches string for an occurrence of the regular expression 
        /// specified as an argument along with matching options.
        /// </summary>

        public static T Match<T>(this string str, string pattern, RegexOptions options, Func<Match, T> selector)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (pattern == null) throw new ArgumentNullException("pattern");
            if (selector == null) throw new ArgumentNullException("selector");
            return selector(Regex.Match(str, pattern, options));
        }

        /// <summary>
        /// Searches the specified input string for all occurrences of the 
        /// regular expression specified as an argument.
        /// </summary>
        /// <remarks>
        /// This method uses deferred execution semantics.
        /// </remarks>

        public static IEnumerable<Match> Matches(this string str, string pattern)
        {
            return str.Matches(pattern, RegexOptions.None);
        }

        /// <summary>
        /// Searches the specified input string for all occurrences of the 
        /// regular expression specified as an argument along with matching
        /// options.
        /// </summary>
        /// <remarks>
        /// This method uses deferred execution semantics.
        /// </remarks>

        public static IEnumerable<Match> Matches(this string str, string pattern, RegexOptions options)
        {
            return str.Matches(pattern, options, m => m);
        }

        /// <summary>
        /// Searches the specified input string for all occurrences of the 
        /// regular expression specified as an argument along with matching
        /// options.
        /// </summary>
        /// <remarks>
        /// This method uses deferred execution semantics.
        /// </remarks>

        public static IEnumerable<T> Matches<T>(this string str, string pattern, Func<Match, T> selector)
        {
            return str.Matches(pattern, RegexOptions.None, selector);
        }

        /// <summary>
        /// Searches the specified input string for all occurrences of the 
        /// regular expression specified as an argument along with matching
        /// options.
        /// </summary>
        /// <remarks>
        /// This method uses deferred execution semantics.
        /// </remarks>

        public static IEnumerable<T> Matches<T>(this string str, string pattern, RegexOptions options, Func<Match, T> selector)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (pattern == null) throw new ArgumentNullException("pattern");
            if (selector == null) throw new ArgumentNullException("selector");
            return MatchesImpl(str, pattern, options, selector);
        }

        private static IEnumerable<T> MatchesImpl<T>(string str, string pattern, RegexOptions options, Func<Match, T> selector)
        {
            var match = str.Match(pattern, options);
            while (match.Success)
            {
                yield return selector(match);
                match = match.NextMatch();
            }
        }
    }
}

namespace Mannex.IO
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="string"/>.
    /// </summary>

    static partial class StringExtensions
    {
        private static readonly char[] _badFileNameChars;
        private static readonly string _badFileNameCharsPattern;
        private static readonly char[] _badPathChars;
        private static readonly string _badPathCharsPattern;

        static StringExtensions()
        {
            _badFileNameChars = Path.GetInvalidFileNameChars();
            _badFileNameCharsPattern = Patternize(_badFileNameChars);
            _badPathChars = Path.GetInvalidPathChars();
            _badPathCharsPattern = Patternize(_badPathChars);
        }

        private static string Patternize(IEnumerable<char> chars)
        {
            Debug.Assert(chars != null);
            return "[" 
                 + string.Join(string.Empty, chars.Select(ch => Regex.Escape(ch.ToString())).ToArray())
                 + "]";
        }

        /// <summary>
        /// Makes the content of the string safe for use as a file name
        /// by replacing all invalid characters, those returned by
        /// <see cref="Path.GetInvalidFileNameChars"/>, with an underscore.
        /// </summary>
        /// <remarks>
        /// This method is not guaranteed to replace the complete set of 
        /// characters that are invalid in file and directory names.
        /// The full set of invalid characters can vary by file system.
        /// </remarks>

        public static string ToFileNameSafe(this string str)
        {
            return ToFileNameSafe(str, null);
        }

        /// <summary>
        /// Makes the content of the string safe for use as a file name
        /// by replacing all invalid characters, those returned by
        /// <see cref="Path.GetInvalidFileNameChars"/>, with 
        /// <paramref name="replacement"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <paramref name="replacement"/> string itself cannot 
        /// carry any invalid file name characters. If 
        /// <paramref name="replacement"/> is <c>null</c> or empty
        /// then it assumes the value of an underscore.</para>
        /// <para>
        /// This method is not guaranteed to replace the complete set of 
        /// characters that are invalid in file and directory names.
        /// The full set of invalid characters can vary by file system.
        /// </para>
        /// </remarks>

        public static string ToFileNameSafe(this string str, string replacement)
        {
            return SanitizePathComponent(str, 
                (replacement ?? string.Empty).MaskEmpty("_"), 
                _badFileNameChars, _badFileNameCharsPattern);
        }

        /// <summary>
        /// Makes the content of the string safe for use as a file name
        /// by replacing all invalid characters, those returned by
        /// <see cref="Path.GetInvalidPathChars"/>, with an underscore.
        /// </summary>
        /// <remarks>
        /// This method is not guaranteed to replace the complete set of 
        /// characters that are invalid in file and directory names.
        /// The full set of invalid characters can vary by file system.
        /// </remarks>

        public static string ToPathNameSafe(this string str)
        {
            return ToPathNameSafe(str, null);
        }

        /// <summary>
        /// Makes the content of the string safe for use as a file name
        /// by replacing all invalid characters, those returned by
        /// <see cref="Path.GetInvalidPathChars"/>, with 
        /// <paramref name="replacement"/>.
        /// </summary>
        /// <remarks>
        /// The <paramref name="replacement"/> string itself cannot 
        /// carry any invalid file name characters. If 
        /// <paramref name="replacement"/> is <c>null</c> or empty
        /// then it assumes the value of an underscore.
        /// <para>
        /// This method is not guaranteed to replace the complete set of 
        /// characters that are invalid in file and directory names.
        /// The full set of invalid characters can vary by file system.
        /// </para>
        /// </remarks>

        public static string ToPathNameSafe(this string str, string replacement)
        {
            return SanitizePathComponent(str,
                (replacement ?? string.Empty).MaskEmpty("_"),
                _badPathChars, _badPathCharsPattern);
        }

        private static string SanitizePathComponent(string str, string replacement, char[] badChars, string badPattern)
        {
            Debug.Assert(replacement != null);
            if (str == null) throw new ArgumentNullException("str");
            if (str.Length == 0) throw new ArgumentException(null, "str");
            if (replacement.IndexOfAny(badChars) >= 0) throw new ArgumentException(null, "replacement");
            return Regex.Replace(str, badPattern, replacement);
        }

        /// <summary>
        /// Returns a <see cref="TextReader"/> for reading string.
        /// </summary>

        public static TextReader Read(this string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            return new StringReader(str);
        }
    }
}

namespace Mannex.IO
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.IO;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="TextReader"/>.
    /// </summary>

    static partial class TextReaderExtensions
    {
        /// <summary>
        /// Reads all lines from reader using deferred semantics.
        /// </summary>

        public static IEnumerable<string> ReadLines(this TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            return ReadLinesImpl(reader);
        }

        private static IEnumerable<string> ReadLinesImpl(this TextReader reader)
        {
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                yield return line;
        }
    }
}