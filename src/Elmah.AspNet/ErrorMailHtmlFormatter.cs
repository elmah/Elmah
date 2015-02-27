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

[assembly: Elmah.Scc("$Id: ErrorMailHtmlFormatter.cs 640 2009-06-01 17:22:02Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using TextWriter = System.IO.TextWriter;

    #endregion

    /// <summary>
    /// Formats the HTML to display the details of a given error that is
    /// suitable for sending as the body of an e-mail message.
    /// </summary>
    
    public class ErrorMailHtmlFormatter : ErrorTextFormatter
    {
        /// <summary>
        /// Returns the text/html MIME type that is the format provided 
        /// by this <see cref="ErrorTextFormatter"/> implementation.
        /// </summary>

        public override string MimeType
        { 
            get { return "text/html"; }
        }

        /// <summary>
        /// Formats a complete HTML document describing the given 
        /// <see cref="Error"/> instance.
        /// </summary>

        public override void Format(TextWriter writer, Error error)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (error == null) throw new ArgumentNullException("error");

            var page = new ErrorMailHtmlPage(error);
            writer.Write(page.TransformText());
        }
    }
}
