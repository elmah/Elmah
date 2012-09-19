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

namespace Mannex
{
    using System;

    /// <summary>
    /// Extension methods for <see cref="ICloneable"/> objects.
    /// </summary>

    static partial class ICloneableExtensions
    {
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>

        public static T CloneObject<T>(this T source) where T : class, ICloneable
        {
            if (source == null) throw new ArgumentNullException("source");
            return (T)source.Clone();
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

        /// <summary>
        /// Retrieves left, middle and right substrings from this instance
        /// given the character position and length of the middle substring.
        /// </summary>
        /// <returns>
        /// Returns a zero-base, single-dimension, array of three elements 
        /// containing the left, middle and right substrings, respectively.
        /// </returns>
        /// <remarks>
        /// This function never returns <c>null</c> for any of the 
        /// substrings. For example, even when <paramref name="index"/> is
        /// zero, the first substring will be an empty string, but not null.
        /// </remarks>

        public static string[] Substrings(this string str, int index, int length)
        {
            return Substrings(str, index, length, (left, mid, right) => new[] { left, mid, right });
        }

        /// <summary>
        /// Retrieves left, middle and right substrings from this instance
        /// given the character position and length of the middle substring.
        /// An additional parameter specifies a function that is used to
        /// project the final result.
        /// </summary>
        /// <remarks>
        /// This function never supplies <c>null</c> for any of the 
        /// substrings. For example, even when <paramref name="index"/> is
        /// zero, the first substring will be an empty string, but not 
        /// <c>null</c>.
        /// </remarks>

        public static T Substrings<T>(this string str, int index, int length, Func<string, string, string, T> resultor)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (resultor == null) throw new ArgumentNullException("resultor");

            return resultor(str.Substring(0, index),
                            str.Substring(index, length),
                            str.Substring(index + length));
        }
    }
}

namespace Mannex.IO
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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

        // Concat derived from StackOverflow answer[1] by Rex Morgan[2].
        //
        // [1] http://stackoverflow.com/a/2925722/6682
        // [2] http://www.rexmorgan.net/

        /// <summary>
        /// Returns a new <see cref="TextReader"/> that represents the
        /// concatenated content of one or more supplied 
        /// <see cref="TextReader"/> objects.
        /// </summary>
        /// <remarks>
        /// If any of the <see cref="TextReader"/> objects is <c>null</c>
        /// then it is treated as being empty; no exception is thrown.
        /// </remarks>

        public static TextReader Concat(this TextReader first, IEnumerable<TextReader> others)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (others == null) throw new ArgumentNullException("others");
            return Concat(first, others.ToArray());
        }

        /// <summary>
        /// Returns a new <see cref="TextReader"/> that represents the
        /// concatenated content of one or more supplied 
        /// <see cref="TextReader"/> objects.
        /// </summary>
        /// <remarks>
        /// If any of the <see cref="TextReader"/> objects is <c>null</c>
        /// then it is treated as being empty; no exception is thrown.
        /// </remarks>

        public static TextReader Concat(this TextReader first, params TextReader[] others)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (others == null) throw new ArgumentNullException("others");
            return new ChainedTextReader(new[] { first }.Concat(others));
        }

        sealed class ChainedTextReader : TextReader
        {
            private TextReader[] _readers;

            public ChainedTextReader(IEnumerable<TextReader> readers)
            {
                if (readers == null) throw new ArgumentNullException("readers");

                _readers = readers.Select(r => r ?? Null)
                    /*sentinel */ .Concat(new TextReader[] { null })
                                  .ToArray();
            }

            private TextReader GetReader()
            {
                if (_readers == null) throw new ObjectDisposedException(null);
                return _readers[0];
            }

            public override int Peek()
            {
                var reader = GetReader();
                return reader == null ? -1 : reader.Peek();
            }

            public override int Read()
            {
                while (true)
                {
                    var reader = GetReader();
                    if (reader == null)
                        return -1;
                    var ch = reader.Read();
                    if (ch >= 0)
                        return ch;
                    _readers.Rotate();
                }
            }

            public override int Read(char[] buffer, int index, int count)
            {
                while (true)
                {
                    var reader = GetReader();
                    if (reader == null)
                        return 0;
                    var read = reader.Read(buffer, index, count);
                    if (read > 0)
                        return read;
                    _readers.Rotate();
                }
            }

            public override void Close()
            {
                OnDisposeOrClose(r => r.Close());
            } 
        
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                OnDisposeOrClose(r => r.Dispose());
            }

            void OnDisposeOrClose(Action<TextReader> action)
            {
                if (_readers == null)
                    return;
                foreach (var reader in _readers.Where(reader => reader != null))
                    action(reader);
                _readers = null;
            }
        }
    }
}

namespace Mannex
{
    #region Imports

    using System;
    using System.Text;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="Array"/> sub-types.
    /// </summary>

    static partial class ArrayExtensions
    {
        /// <summary>
        /// Formats bytes in hexadecimal format, appending to the 
        /// supplied <see cref="StringBuilder"/>.
        /// </summary>
        
        public static string ToHex(this byte[] buffer)
        {
            return ToHex(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Formats bytes in hexadecimal format, appending to the 
        /// supplied <see cref="StringBuilder"/>.
        /// </summary>
        
        public static string ToHex(this byte[] buffer, int index, int count)
        {
            return ToHex(buffer, index, count, null).ToString();
        }

        /// <summary>
        /// Formats bytes in hexadecimal format, appending to the 
        /// supplied <see cref="StringBuilder"/>.
        /// </summary>
        
        public static StringBuilder ToHex(this byte[] buffer, int index, int count, StringBuilder sb)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (index < 0 || index > buffer.Length) throw new ArgumentOutOfRangeException("index");
            if (index + count > buffer.Length) throw new ArgumentOutOfRangeException("count");
            
            if (sb == null)
                sb = new StringBuilder(count * 2);

            for (var i = index; i < index + count; i++)
            {
                const string hexdigits = "0123456789abcdef";
                var b = buffer[i];
                sb.Append(hexdigits[b/16]);
                sb.Append(hexdigits[b%16]);
            }

            return sb;
        }

        /// <summary>
        /// Rotates the elements of the array (in-place) such that all 
        /// elements are shifted left by one position, with the exception of 
        /// the first element which assumes the last position in the array.
        /// </summary>
        
        public static void Rotate<T>(this T[] array)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (array.Length == 0) return;
            var first = array[0];
            Array.Copy(array, 1, array, 0, array.Length - 1);
            array[array.Length - 1] = first;
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