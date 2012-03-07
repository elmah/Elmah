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

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Threading;
    using System.Web;
    using System.Xml;

    using XmlReader = System.Xml.XmlReader;
    using XmlWriter = System.Xml.XmlWriter;
    using Thread = System.Threading.Thread;
    using NameValueCollection = System.Collections.Specialized.NameValueCollection;
    using XmlConvert = System.Xml.XmlConvert;
    using WriteState = System.Xml.WriteState;

    #endregion

    /// <summary>
    /// Responsible for primarily encoding the JSON representation of
    /// <see cref="Error"/> objects.
    /// </summary>

    [ Serializable ]
    public sealed class ErrorJson
    {
        /// <summary>
        /// Encodes the default JSON representation of an <see cref="Error"/> 
        /// object to a string.
        /// </summary>
        /// <remarks>
        /// Only properties and collection entires with non-null
        /// and non-empty strings are emitted.
        /// </remarks>

        public static string EncodeString(Error error)
        {
            if (error == null) 
                throw new ArgumentNullException("error");

            StringWriter writer = new StringWriter();
            Encode(error, writer);
            return writer.ToString();
        }

        /// <summary>
        /// Encodes the default JSON representation of an <see cref="Error"/> 
        /// object to a <see cref="TextWriter" />.
        /// </summary>
        /// <remarks>
        /// Only properties and collection entires with non-null
        /// and non-empty strings are emitted.
        /// </remarks>

        public static void Encode(Error error, TextWriter writer)
        {
            if (error == null) 
                throw new ArgumentNullException("error");
            
            if (writer == null)
                throw new ArgumentNullException("writer");

            EncodeEnclosed(error, new JsonTextWriter(writer));
        }

        private static void EncodeEnclosed(Error error, JsonTextWriter writer)
        {
            Debug.Assert(error != null);
            Debug.Assert(writer != null);

            writer.Object();
            Encode(error, writer);
            writer.Pop();
        }

        internal static void Encode(Error error, JsonTextWriter writer)
        {
            Debug.Assert(error != null);
            Debug.Assert(writer != null);

            Member(writer, "application", error.ApplicationName);
            Member(writer, "host", error.HostName);
            Member(writer, "type", error.Type);
            Member(writer, "message", error.Message);
            Member(writer, "source", error.Source);
            Member(writer, "detail", error.Detail);
            Member(writer, "user", error.User);
            Member(writer, "time", error.Time, DateTime.MinValue);
            Member(writer, "statusCode", error.StatusCode, 0);
            Member(writer, "webHostHtmlMessage", error.WebHostHtmlMessage);
            Member(writer, "serverVariables", error.ServerVariables);
            Member(writer, "queryString", error.QueryString);
            Member(writer, "form", error.Form);
            Member(writer, "cookies", error.Cookies);
        }

        private static void Member(JsonTextWriter writer, string name, int value, int defaultValue)
        {
            if (value == defaultValue)
                return;

            writer.Member(name).Number(value);
        }

        private static void Member(JsonTextWriter writer, string name, DateTime value, DateTime defaultValue)
        {
            if (value == defaultValue)
                return;

            writer.Member(name).String(value);
        }

        private static void Member(JsonTextWriter writer, string name, string value)
        {
            Debug.Assert(writer != null);
            Debug.AssertStringNotEmpty(name);

            if (value == null || value.Length == 0)
                return;

            writer.Member(name).String(value);
        }

        private static void Member(JsonTextWriter writer, string name, NameValueCollection collection)
        {
            Debug.Assert(writer != null);
            Debug.AssertStringNotEmpty(name);

            //
            // Bail out early if the collection is null or empty.
            //

            if (collection == null || collection.Count == 0) 
                return;

            //
            // Save the depth, which we'll use to lazily emit the collection.
            // That is, if we find that there is nothing useful in it, then
            // we could simply avoid emitting anything.
            //

            int depth = writer.Depth;

            //
            // For each key, we get all associated values and loop through
            // twice. The first time round, we count strings that are 
            // neither null nor empty. If none are found then the key is 
            // skipped. Otherwise, second time round, we encode
            // strings that are neither null nor empty. If only such string
            // exists for a key then it is written directly, otherwise
            // multiple strings are naturally wrapped in an array.
            //

            foreach (string key in collection.Keys)
            {
                string[] values = collection.GetValues(key);

                if (values == null || values.Length == 0)
                    continue;

                int count = 0; // Strings neither null nor empty.

                for (int i = 0; i < values.Length; i++)
                {
                    string value = values[i];
                    if (value != null && value.Length > 0)
                        count++;
                }

                if (count == 0) // None?
                    continue;   // Skip key

                //
                // There is at least one value so now we emit the key.
                // Before doing that, we check if the collection member
                // was ever started. If not, this would be a good time.
                //

                if (depth == writer.Depth)
                {
                    writer.Member(name);
                    writer.Object();
                }

                writer.Member(key);

                if (count > 1)
                    writer.Array(); // Wrap multiples in an array

                for (int i = 0; i < values.Length; i++)
                {
                    string value = values[i];
                    if (value != null && value.Length > 0)
                        writer.String(value);
                }

                if (count > 1) 
                    writer.Pop();   // Close multiples array
            }

            //
            // If we are deeper than when we began then the collection was
            // started so we terminate it here.
            //

            if (writer.Depth > depth)
                writer.Pop();
        }

        private ErrorJson()
        {
            throw new NotSupportedException();
        }
    }
}
