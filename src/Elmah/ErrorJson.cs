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

[assembly: Elmah.Scc("$Id: ErrorJson.cs 607 2009-05-27 23:47:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.IO;
    using System.Linq;
    using NameValueCollection = System.Collections.Specialized.NameValueCollection;

    #endregion

    /// <summary>
    /// Responsible for primarily encoding the JSON representation of
    /// <see cref="Error"/> objects.
    /// </summary>

    public static class ErrorJson
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
            if (error == null) throw new ArgumentNullException("error");

            var writer = new StringWriter();
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
            if (error == null) throw new ArgumentNullException("error");            
            if (writer == null) throw new ArgumentNullException("writer");

            EncodeEnclosed(error, new JsonTextWriter(writer));
        }

        private static void EncodeEnclosed(Error error, JsonTextWriter writer)
        {
            Debug.Assert(error != null);
            Debug.Assert(writer != null);

            writer.Object();
            EncodeMembers(error, writer);
            writer.Pop();
        }

        public static void EncodeMembers(Error error, JsonTextWriter writer)
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

            if (string.IsNullOrEmpty(value))
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

            var depth = writer.Depth;

            //
            // For each key, we get all associated values and loop through
            // twice. The first time round, we count strings that are 
            // neither null nor empty. If none are found then the key is 
            // skipped. Otherwise, second time round, we encode
            // strings that are neither null nor empty. If only such string
            // exists for a key then it is written directly, otherwise
            // multiple strings are naturally wrapped in an array.
            //

            var items = from i in Enumerable.Range(0, collection.Count)
                        let values = collection.GetValues(i)
                        where values != null && values.Length > 0
                        let some = // Neither null nor empty
                            from v in values
                            where !string.IsNullOrEmpty(v)
                            select v
                        let nom = some.Take(2).Count()
                        where nom > 0
                        select new
                        {
                            Key = collection.GetKey(i), 
                            IsArray = nom > 1, 
                            Values = some,
                        };
            
            foreach (var item in items)
            {
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

                writer.Member(item.Key ?? string.Empty);

                if (item.IsArray)
                    writer.Array(); // Wrap multiples in an array

                foreach (var value in item.Values)
                    writer.String(value);

                if (item.IsArray) 
                    writer.Pop();   // Close multiples array
            }

            //
            // If we are deeper than when we began then the collection was
            // started so we terminate it here.
            //

            if (writer.Depth > depth)
                writer.Pop();
        }
    }
}
