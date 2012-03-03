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

[assembly: Elmah.Scc("$Id: ErrorXmlHandler.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System.Net;
    using System.Web;
    using System.Xml;

    #endregion

    /// <summary>
    /// Renders an error as an XML document.
    /// </summary>

    internal sealed class ErrorXmlHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            HttpResponse response = context.Response;
            response.ContentType = "application/xml";

            //
            // Retrieve the ID of the requested error and read it from 
            // the store.
            //

            string errorId = Mask.NullString(context.Request.QueryString["id"]);

            if (errorId.Length == 0)
                throw new ApplicationException("Missing error identifier specification.");

            ErrorLogEntry entry = ErrorLog.GetDefault(context).GetError(errorId);

            //
            // Perhaps the error has been deleted from the store? Whatever
            // the reason, pretend it does not exist.
            //

            if (entry == null)
            {
                throw new HttpException((int) HttpStatusCode.NotFound, 
                    string.Format("Error with ID '{0}' not found.", errorId));
            }

            //
            // Stream out the error as formatted XML.
            //

#if !NET_1_0 && !NET_1_1
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            settings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(response.Output, settings);
#else
            XmlTextWriter writer = new XmlTextWriter(response.Output);
            writer.Formatting = Formatting.Indented;
#endif

            writer.WriteStartDocument();
            writer.WriteStartElement("error");
            ErrorXml.Encode(entry.Error, writer);
            writer.WriteEndElement(/* error */);
            writer.WriteEndDocument();
            writer.Flush();
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}