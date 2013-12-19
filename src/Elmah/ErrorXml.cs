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

[assembly: Elmah.Scc("$Id: ErrorXml.cs 776 2011-01-12 21:09:24Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.IO;
    using System.Xml;
    
    using NameValueCollection = System.Collections.Specialized.NameValueCollection;

    #endregion

    /// <summary>
    /// Responsible for encoding and decoding the XML representation of
    /// an <see cref="Error"/> object.
    /// </summary>

    public static class ErrorXml
    {
        /// <summary>
        /// Decodes an <see cref="Error"/> object from its default XML 
        /// representation.
        /// </summary>

        public static Error DecodeString(string xml)
        {
            using (var sr = new StringReader(xml))
            using (var reader = XmlReader.Create(sr))
            {
                if (!reader.IsStartElement("error"))
                    throw new ApplicationException("The error XML is not in the expected format.");

                return Decode(reader);
            }
        }

        /// <summary>
        /// Decodes an <see cref="Error"/> object from its XML representation.
        /// </summary>

        public static Error Decode(XmlReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (!reader.IsStartElement()) throw new ArgumentException("Reader is not positioned at the start of an element.", "reader");

            //
            // Read out the attributes that contain the simple
            // typed state.
            //

            var error = new Error();
            ReadXmlAttributes(reader, error);

            //
            // Move past the element. If it's not empty, then
            // read also the inner XML that contains complex
            // types like collections.
            //

            var isEmpty = reader.IsEmptyElement;
            reader.Read();

            if (!isEmpty)
            {
                ReadInnerXml(reader, error);
                while (reader.NodeType != XmlNodeType.EndElement)
                    reader.Skip();
                reader.ReadEndElement();
            }

            return error;
        }

        /// <summary>
        /// Reads the error data in XML attributes.
        /// </summary>
        
        private static void ReadXmlAttributes(XmlReader reader, Error error)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (!reader.IsStartElement()) throw new ArgumentException("Reader is not positioned at the start of an element.", "reader");

            error.ApplicationName = reader.GetAttribute("application");
            error.HostName = reader.GetAttribute("host");
            error.Type = reader.GetAttribute("type");
            error.Message = reader.GetAttribute("message");
            error.Source = reader.GetAttribute("source");
            error.Detail = reader.GetAttribute("detail");
            error.User = reader.GetAttribute("user");
            var timeString = reader.GetAttribute("time") ?? string.Empty;
            error.Time = timeString.Length == 0 ? new DateTime() : XmlConvert.ToDateTime(timeString);
            var statusCodeString = reader.GetAttribute("statusCode") ?? string.Empty;
            error.StatusCode = statusCodeString.Length == 0 ? 0 : XmlConvert.ToInt32(statusCodeString);
            error.WebHostHtmlMessage = reader.GetAttribute("webHostHtmlMessage");
        }

        /// <summary>
        /// Reads the error data in child nodes.
        /// </summary>

        private static void ReadInnerXml(XmlReader reader, Error error)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            //
            // Loop through the elements, reading those that we
            // recognize. If an unknown element is found then
            // this method bails out immediately without
            // consuming it, assuming that it belongs to a subclass.
            //

            while (reader.IsStartElement())
            {
                //
                // Optimization Note: This block could be re-wired slightly 
                // to be more efficient by not causing a collection to be
                // created if the element is going to be empty.
                //

                NameValueCollection collection;

                switch (reader.Name)
                {
                    case "serverVariables" : collection = error.ServerVariables; break;
                    case "queryString"     : collection = error.QueryString; break;
                    case "form"            : collection = error.Form; break;
                    case "cookies"         : collection = error.Cookies; break;
                    default                : reader.Skip(); continue;
                }

                if (reader.IsEmptyElement)
                    reader.Read();
                else
                    UpcodeTo(reader, collection);
            }
        }

        /// <summary>
        /// Encodes the default XML representation of an <see cref="Error"/> 
        /// object to a string.
        /// </summary>

        public static string EncodeString(Error error)
        {
            var sw = new StringWriter();

            using (var writer = XmlWriter.Create(sw, new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = true,
                CheckCharacters = false,
                OmitXmlDeclaration = true, // see issue #120: http://code.google.com/p/elmah/issues/detail?id=120
            }))
            {
                writer.WriteStartElement("error");
                Encode(error, writer);
                writer.WriteEndElement();
                writer.Flush();
            }

            return sw.ToString();
        }

        /// <summary>
        /// Encodes the XML representation of an <see cref="Error"/> object.
        /// </summary>

        public static void Encode(Error error, XmlWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (writer.WriteState != WriteState.Element) throw new ArgumentException("Writer is not in the expected Element state.", "writer");

            //
            // Write out the basic typed information in attributes
            // followed by collections as inner elements.
            //

            WriteXmlAttributes(error, writer);
            WriteInnerXml(error, writer);
        }

        /// <summary>
        /// Writes the error data that belongs in XML attributes.
        /// </summary>

        private static void WriteXmlAttributes(Error error, XmlWriter writer)
        {
            Debug.Assert(error != null);
            if (writer == null) throw new ArgumentNullException("writer");

            WriteXmlAttribute(writer, "application", error.ApplicationName);
            WriteXmlAttribute(writer, "host", error.HostName);
            WriteXmlAttribute(writer, "type", error.Type);
            WriteXmlAttribute(writer, "message", error.Message);
            WriteXmlAttribute(writer, "source", error.Source);
            WriteXmlAttribute(writer, "detail", error.Detail);
            WriteXmlAttribute(writer, "user", error.User);
            if (error.Time != DateTime.MinValue)
                WriteXmlAttribute(writer, "time", XmlConvert.ToString(error.Time.ToUniversalTime(), @"yyyy-MM-dd\THH:mm:ss.fffffff\Z"));
            if (error.StatusCode != 0)
                WriteXmlAttribute(writer, "statusCode", XmlConvert.ToString(error.StatusCode));
            WriteXmlAttribute(writer, "webHostHtmlMessage", error.WebHostHtmlMessage);
        }

        /// <summary>
        /// Writes the error data that belongs in child nodes.
        /// </summary>

        private static void WriteInnerXml(Error error, XmlWriter writer)
        {
            Debug.Assert(error != null);
            if (writer == null) throw new ArgumentNullException("writer");

            WriteCollection(writer, "serverVariables", error.ServerVariables);
            WriteCollection(writer, "queryString", error.QueryString);
            WriteCollection(writer, "form", error.Form);
            WriteCollection(writer, "cookies", error.Cookies);
        }

        private static void WriteCollection(XmlWriter writer, string name, NameValueCollection collection)
        {
            Debug.Assert(writer != null);
            Debug.AssertStringNotEmpty(name);

            if (collection == null || collection.Count == 0) 
                return;

            writer.WriteStartElement(name);
            Encode(collection, writer);
            writer.WriteEndElement();
        }

        private static void WriteXmlAttribute(XmlWriter writer, string name, string value)
        {
            Debug.Assert(writer != null);
            Debug.AssertStringNotEmpty(name);

            if (!string.IsNullOrEmpty(value))
                writer.WriteAttributeString(name, value);
        }

        /// <summary>
        /// Encodes an XML representation for a 
        /// <see cref="NameValueCollection" /> object.
        /// </summary>

        private static void Encode(NameValueCollection collection, XmlWriter writer) 
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (writer == null) throw new ArgumentNullException("writer");

            if (collection.Count == 0)
                return;

            //
            // Write out a named multi-value collection as follows 
            // (example here is the ServerVariables collection):
            //
            //      <item name="HTTP_URL">
            //          <value string="/myapp/somewhere/page.aspx" />
            //      </item>
            //      <item name="QUERY_STRING">
            //          <value string="a=1&amp;b=2" />
            //      </item>
            //      ...
            //

            foreach (string key in collection.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteAttributeString("name", key);
                
                var values = collection.GetValues(key);

                if (values != null)
                {
                    foreach (var value in values)
                    {
                        writer.WriteStartElement("value");
                        writer.WriteAttributeString("string", value);
                        writer.WriteEndElement();
                    }
                }
                
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Updates an existing <see cref="NameValueCollection" /> object from
        /// its XML representation.
        /// </summary>

        private static void UpcodeTo(XmlReader reader, NameValueCollection collection)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (collection == null) throw new ArgumentNullException("collection");

            Debug.Assert(!reader.IsEmptyElement);
            reader.Read();

            //
            // Add entries into the collection as <item> elements
            // with child <value> elements are found.
            //

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.IsStartElement("item"))
                {
                    var name = reader.GetAttribute("name");
                    var isNull = reader.IsEmptyElement;

                    reader.Read(); // <item>

                    if (!isNull)
                    {
                        while (reader.NodeType != XmlNodeType.EndElement)
                        {
                            if (reader.IsStartElement("value")) // <value ...>
                            {
                                var value = reader.GetAttribute("string");
                                collection.Add(name, value);
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                }
                                else
                                {
                                    reader.Read();
                                    while (reader.NodeType != XmlNodeType.EndElement)
                                        reader.Skip();
                                    reader.ReadEndElement();
                                }
                            }
                            else
                            {
                                reader.Skip();
                            }

                            reader.MoveToContent();
                        }
                        
                        reader.ReadEndElement(); // </item>
                    }
                    else
                    {
                        collection.Add(name, null);
                    }
                }
                else
                {
                    reader.Skip();
                }

                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }
    }
}
