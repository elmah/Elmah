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

[assembly: Elmah.Scc("$Id: XmlSerializer.cs 907 2011-12-18 13:03:58Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System.IO;
    using System.Xml;
    using SystemXmlSerializer = System.Xml.Serialization.XmlSerializer;

    #endregion

    /// <summary>
    /// Serializes object to and from XML documents.
    /// </summary>
    
    internal sealed class XmlSerializer
    {
        private XmlSerializer() { }

        public static string Serialize(object obj)
        {
            StringWriter sw = new StringWriter();
            Serialize(obj, sw);
            return sw.GetStringBuilder().ToString();
        }

        public static void Serialize(object obj, TextWriter output)
        {
            Debug.Assert(obj != null);
            Debug.Assert(output != null);

#if !NET_1_0 && !NET_1_1
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            settings.CheckCharacters = false;
            settings.OmitXmlDeclaration = true;
            XmlWriter writer = XmlWriter.Create(output, settings);
#else
            XmlTextWriter writer = new XmlTextWriter(output);
            writer.Formatting = Formatting.Indented;

            StringWriter sw = output as StringWriter;
            if (sw != null)
            {
                // HACK for issue #220:
                // http://code.google.com/p/elmah/issues/detail?id=220
                //
                // Have the declaration omitted by clearing the underlying
                // StringBuilder object after starting the XML document.

                writer.WriteStartDocument(true);
                writer.Flush();
                output.Flush();
                sw.GetStringBuilder().Length = 0;
            }
#endif

            try
            {
                SystemXmlSerializer serializer = new SystemXmlSerializer(obj.GetType());
                serializer.Serialize(writer, obj);
                writer.Flush();
            }
            finally
            {
                writer.Close();
            }
        }
    }
}