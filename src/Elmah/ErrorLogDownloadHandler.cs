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

[assembly: Elmah.Scc("$Id: ErrorLogDownloadHandler.cs 923 2011-12-23 22:02:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Web;
    using System.Collections.Generic;

    #endregion

    internal sealed class ErrorLogDownloadHandler : IHttpAsyncHandler
    {
        private static readonly TimeSpan _beatPollInterval = TimeSpan.FromSeconds(3);

        private const int _pageSize = 100;

        #region IHttpAsyncHandler implementation

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            ProcessRequest(new HttpContextWrapper(context));
        }

        IAsyncResult IHttpAsyncHandler.BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            return BeginProcessRequest(new HttpContextWrapper(context), cb, extraData);
        }

        void IHttpAsyncHandler.EndProcessRequest(IAsyncResult result)
        {
            EndProcessRequest(result);
        }

        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }

        #endregion

        public static void ProcessRequest(HttpContextBase context)
        {
            EndProcessRequest(BeginProcessRequest(context, null, null));
        }

        public static IAsyncResult BeginProcessRequest(HttpContextBase context, AsyncCallback cb, object extraData)
        {
            if (context == null) throw new ArgumentNullException("context");
            
            var request = context.Request;
            var query = request.QueryString;

            //
            // Limit the download by some maximum # of records?
            //

            var maxDownloadCount = Math.Max(0, Convert.ToInt32(query["limit"], CultureInfo.InvariantCulture));

            //
            // Determine the desired output format.
            //

            var format = GetFormat(context, Mask.EmptyString(query["format"], "csv").ToLowerInvariant());
            Debug.Assert(format != null);

            //
            // Emit format header, initialize and then fetch results.
            //

            context.Response.BufferOutput = false;
            format.Header();

            var result = new AsyncResult(cb, extraData, typeof(ErrorLogDownloadHandler), "ProcessRequest");
            var log = ErrorLog.GetDefault(context);
            var pageIndex = 0;
            var lastBeatTime = DateTime.Now;
            var errorEntryList = new List<ErrorLogEntry>(_pageSize);
            var downloadCount = 0;

            var onGetErrors = new AsyncCallback[1];
            onGetErrors[0] = ar =>
            {
                try
                {
                    if (ar == null) throw new ArgumentNullException("ar");

                    var total = log.EndGetErrors(ar);
                    var count = errorEntryList.Count;

                    if (maxDownloadCount > 0)
                    {
                        var remaining = maxDownloadCount - (downloadCount + count);
                        if (remaining < 0)
                            count += remaining;
                    }

                    format.Entries(errorEntryList, 0, count, total);
                    downloadCount += count;

                    var response = context.Response;
                    response.Flush();

                    //
                    // Done if either the end of the list (no more errors found) or
                    // the requested limit has been reached.
                    //

                    if (count == 0 || downloadCount == maxDownloadCount)
                    {
                        if (count > 0)
                            format.Entries(new ErrorLogEntry[0], total); // Terminator
                        result.Complete();
                        return;
                    }

                    //
                    // Poll whether the client is still connected so data is not
                    // unnecessarily sent to an abandoned connection. This check is 
                    // only performed at certain intervals.
                    //

                    if (DateTime.Now - lastBeatTime > _beatPollInterval)
                    {
                        if (!response.IsClientConnected)
                        {
                            result.Complete();
                            return;
                        }

                        lastBeatTime = DateTime.Now;
                    }

                    //
                    // Fetch next page of results.
                    //

                    errorEntryList.Clear();

                    // FIXME!!!
                    // TODO Don't let the stack get too deep if callbacks complete synchronously!

                    log.BeginGetErrors(++pageIndex, _pageSize, errorEntryList,
                        onGetErrors[0], null);
                }
                catch (Exception e)
                {
                    //
                    // If anything goes wrong during callback processing then 
                    // the exception needs to be captured and the raising 
                    // delayed until EndProcessRequest.Meanwhile, the 
                    // BeginProcessRequest called is notified immediately of 
                    // completion.
                    //

                    result.Complete(e);
                }
            };

            Debug.Assert(onGetErrors[0] != null);

            log.BeginGetErrors(pageIndex, _pageSize, errorEntryList, 
                onGetErrors[0], null);

            return result;
        }

        public static void EndProcessRequest(IAsyncResult result)
        {
            if (result == null)
                throw new ArgumentNullException("result");
            
            AsyncResult.End(result, typeof(ErrorLogDownloadHandler), "ProcessRequest");
        }

        private static Format GetFormat(HttpContextBase context, string format)
        {
            Debug.Assert(context != null);
            switch (format)
            {
                case "csv": return new CsvFormat(context);
                case "jsonp": return new JsonPaddingFormat(context);
                case "html-jsonp": return new JsonPaddingFormat(context, /* wrapped */ true);
                default:
                    throw new Exception("Request log format is not supported.");
            }
        }

        private abstract class Format
        {
            private readonly HttpContextBase _context;

            protected Format(HttpContextBase context)
            {
                Debug.Assert(context != null);
                _context = context;
            }

            protected HttpContextBase Context { get { return _context; } }

            public virtual void Header() {}

            public void Entries(IList<ErrorLogEntry> entries, int total)
            {
                Entries(entries, 0, entries.Count, total);
            }

            public abstract void Entries(IList<ErrorLogEntry> entries, int index, int count, int total);
        }

        private sealed class CsvFormat : Format
        {
            public CsvFormat(HttpContextBase context) : 
                base(context) {}

            public override void Header()
            {
                var response = Context.Response;
                response.AppendHeader("Content-Type", "text/csv; header=present");
                response.AppendHeader("Content-Disposition", "attachment; filename=errorlog.csv");
                response.Output.Write("Application,Host,Time,Unix Time,Type,Source,User,Status Code,Message,URL,XMLREF,JSONREF\r\n");
            }

            public override void Entries(IList<ErrorLogEntry> entries, int index, int count, int total)
            {
                Debug.Assert(entries != null);
                Debug.Assert(index >= 0);
                Debug.Assert(index + count <= entries.Count);

                if (count == 0)
                    return;

                //
                // Setup to emit CSV records.
                //

                StringWriter writer = new StringWriter();
                writer.NewLine = "\r\n";
                CsvWriter csv = new CsvWriter(writer);

                CultureInfo culture = CultureInfo.InvariantCulture;
                DateTime epoch = new DateTime(1970, 1, 1);

                //
                // For each error, emit a CSV record.
                //

                for (int i = index; i < count; i++)
                {
                    ErrorLogEntry entry = entries[i];
                    Error error = entry.Error;
                    DateTime time = error.Time.ToUniversalTime();
                    string query = "?id=" + HttpUtility.UrlEncode(entry.Id);
                    Uri requestUrl = ErrorLogPageFactory.GetRequestUrl(Context);

                    csv.Field(error.ApplicationName)
                        .Field(error.HostName)
                        .Field(time.ToString("yyyy-MM-dd HH:mm:ss", culture))
                        .Field(time.Subtract(epoch).TotalSeconds.ToString("0.0000", culture))
                        .Field(error.Type)
                        .Field(error.Source)
                        .Field(error.User)
                        .Field(error.StatusCode.ToString(culture))
                        .Field(error.Message)
                        .Field(new Uri(requestUrl, "detail" + query).ToString())
                        .Field(new Uri(requestUrl, "xml" + query).ToString())
                        .Field(new Uri(requestUrl, "json" + query).ToString())
                        .Record();
                }

                Context.Response.Output.Write(writer.ToString());
            }
        }

        private sealed class JsonPaddingFormat : Format
        {
            private static readonly Regex _callbackExpression = new Regex(@"^ 
                     [a-z_] [a-z0-9_]+ ( \[ [0-9]+ \] )?
                ( \. [a-z_] [a-z0-9_]+ ( \[ [0-9]+ \] )? )* $",
                RegexOptions.IgnoreCase
                | RegexOptions.Singleline
                | RegexOptions.ExplicitCapture
                | RegexOptions.IgnorePatternWhitespace
                | RegexOptions.CultureInvariant);

            private string _callback;
            private readonly bool _wrapped;

            public JsonPaddingFormat(HttpContextBase context) :
                this(context, false) {}

            public JsonPaddingFormat(HttpContextBase context, bool wrapped) : 
                base(context)
            {
                _wrapped = wrapped;
            }

            public override void Header()
            {
                string callback = Context.Request.QueryString[Mask.EmptyString(null, "callback")] 
                                  ?? string.Empty;
                
                if (callback.Length == 0)
                    throw new Exception("The JSONP callback parameter is missing.");

                if (!_callbackExpression.IsMatch(callback))
                    throw new Exception("The JSONP callback parameter is not in an acceptable format.");

                _callback = callback;

                var response = Context.Response;

                if (!_wrapped)
                {
                    response.AppendHeader("Content-Type", "text/javascript");
                    response.AppendHeader("Content-Disposition", "attachment; filename=errorlog.js");
                }
                else
                {
                    response.AppendHeader("Content-Type", "text/html");

                    TextWriter output = response.Output;
                    output.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                    output.WriteLine(@"
                    <html xmlns='http://www.w3.org/1999/xhtml'>
                    <head>
                        <title>Error Log in HTML-Wrapped JSONP Format</title>
                    </head>
                    <body>
                        <p>This page is primarily designed to be used in an IFRAME of a parent HTML document.</p>");
                }
            }

            public override void Entries(IList<ErrorLogEntry> entries, int index, int count, int total)
            {
                Debug.Assert(entries != null);
                Debug.Assert(index >= 0);
                Debug.Assert(index + count <= entries.Count);

                StringWriter writer = new StringWriter();
                writer.NewLine = "\n";

                if (_wrapped)
                {
                    writer.WriteLine("<script type='text/javascript' language='javascript'>");
                    writer.WriteLine("//<[!CDATA[");
                }
                
                writer.Write(_callback);
                writer.Write('(');

                JsonTextWriter json = new JsonTextWriter(writer);
                json.Object()
                    .Member("total").Number(total)
                    .Member("errors").Array();

                Uri requestUrl = ErrorLogPageFactory.GetRequestUrl(Context);

                for (int i = index; i < count; i++)
                {
                    ErrorLogEntry entry = entries[i];
                    writer.WriteLine();
                    if (i == 0) writer.Write(' ');
                    writer.Write("  ");

                    string urlTemplate = new Uri(requestUrl, "{0}?id=" + HttpUtility.UrlEncode(entry.Id)).ToString();
                    
                    json.Object();
                        ErrorJson.Encode(entry.Error, json);
                        json.Member("hrefs")
                        .Array()
                            .Object()
                                .Member("type").String("text/html")
                                .Member("href").String(string.Format(urlTemplate, "detail")).Pop()
                            .Object()
                                .Member("type").String("aplication/json")
                                .Member("href").String(string.Format(urlTemplate, "json")).Pop()
                            .Object()
                                .Member("type").String("application/xml")
                                .Member("href").String(string.Format(urlTemplate, "xml")).Pop()
                        .Pop()
                    .Pop();
                }

                json.Pop();
                json.Pop();

                if (count > 0) 
                    writer.WriteLine();

                writer.WriteLine(");");

                if (_wrapped)
                {
                    writer.WriteLine("//]]>");
                    writer.WriteLine("</script>");

                    if (count == 0)
                        writer.WriteLine(@"</body></html>");
                }

                Context.Response.Output.Write(writer);
            }
        }

        private sealed class CsvWriter
        {
            private readonly TextWriter _writer;
            private int _column;

            private static readonly char[] _reserved = new char[] { '\"', ',', '\r', '\n' };

            public CsvWriter(TextWriter writer)
            {
                Debug.Assert(writer != null);

                _writer = writer;
            }

            public CsvWriter Record()
            {
                _writer.WriteLine();
                _column = 0;
                return this;
            }

            public CsvWriter Field(string value)
            {
                if (_column > 0)
                    _writer.Write(',');

                // 
                // Fields containing line breaks (CRLF), double quotes, and commas 
                // need to be enclosed in double-quotes. 
                //

                int index = value.IndexOfAny(_reserved);

                if (index < 0)
                {
                    _writer.Write(value);
                }
                else
                {
                    //
                    // As double-quotes are used to enclose fields, then a 
                    // double-quote appearing inside a field must be escaped by 
                    // preceding it with another double quote. 
                    //

                    const string quote = "\"";
                    _writer.Write(quote);
                    _writer.Write(value.Replace(quote, quote + quote));
                    _writer.Write(quote);
                }

                _column++;
                return this;
            }
        }
    }
}
