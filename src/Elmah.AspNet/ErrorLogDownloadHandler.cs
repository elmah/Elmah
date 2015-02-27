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
    #if !NET_3_5 && !NET_4_0
    using System.Threading.Tasks;
    #endif
    using System.Web;
    using System.Collections.Generic;

    #endregion

    static class ErrorLogDownloadHandler
    {
        private static readonly TimeSpan _beatPollInterval = TimeSpan.FromSeconds(3);

        private const int _pageSize = 100;

        public static IEnumerable<AsyncResultOr<string>> ProcessRequest(
            HttpContextBase context, 
            Func<AsyncCallback> getAsyncCallback)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (getAsyncCallback == null) throw new ArgumentNullException("getAsyncCallback");

            return ProcessRequestPrelude(context, (format, maxDownloadCount) => ProcessRequest(context, getAsyncCallback, format, maxDownloadCount));
        }

        private static T ProcessRequestPrelude<T>(HttpContextBase context, Func<Format, int, T> resultor)
        {
            Debug.Assert(context != null);
            Debug.Assert(resultor != null);

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
            return resultor(format, maxDownloadCount);
        }

        #if !NET_3_5 && !NET_4_0

        public static Task ProcessRequestAsync(HttpContextBase context)
        {
            if (context == null) throw new ArgumentNullException("context");
            return ProcessRequestPrelude(context, (format, maxDownloadCount) => ProcessRequestAsync(context, format, maxDownloadCount));
        }

        private static async Task ProcessRequestAsync(HttpContextBase context, Format format, int maxDownloadCount)
        {
            var response = context.Response;
            var output = response.Output;

            foreach (var text in format.Header())
                await output.WriteAsync(text);

            var log = ErrorLog.GetDefault(context);
            var errorEntryList = new List<ErrorLogEntry>(_pageSize);
            var downloadCount = 0;
            var clientDisconnectedToken = response.ClientDisconnectedToken;

            for (var pageIndex = 0; ; pageIndex++)
            {
                var total = await log.GetErrorsAsync(pageIndex, _pageSize, errorEntryList, clientDisconnectedToken);
                var count = errorEntryList.Count;

                if (maxDownloadCount > 0)
                {
                    var remaining = maxDownloadCount - (downloadCount + count);
                    if (remaining < 0)
                        count += remaining;
                }

                foreach (var entry in format.Entries(errorEntryList, 0, count, total))
                    await output.WriteAsync(entry);

                downloadCount += count;

                await Task.Factory.FromAsync(response.BeginFlush, response.EndFlush, null);

                //
                // Done if either the end of the list (no more errors found) or
                // the requested limit has been reached.
                //

                if (count == 0 || downloadCount == maxDownloadCount)
                {
                    if (count > 0)
                    {
                        foreach (var entry in format.Entries(new ErrorLogEntry[0], total)) // Terminator
                            await output.WriteAsync(entry);
                    }
                    break;
                }

                if (clientDisconnectedToken.IsCancellationRequested)
                    break;

                //
                // Fetch next page of results.
                //

                errorEntryList.Clear();
            }
        }

        #endif // !NET_3_5 && !NET_4_0

        private static IEnumerable<AsyncResultOr<string>> ProcessRequest(HttpContextBase context, Func<AsyncCallback> getAsyncCallback, Format format, int maxDownloadCount)
        {
            var response = context.Response;

            foreach (var text in format.Header())
                yield return AsyncResultOr.Value(text);

            var log = ErrorLog.GetDefault(context);
            var lastBeatTime = DateTime.Now;
            var errorEntryList = new List<ErrorLogEntry>(_pageSize);
            var downloadCount = 0;

            for (var pageIndex = 0; ; pageIndex++)
            {
                var ar = log.BeginGetErrors(pageIndex, _pageSize, errorEntryList,
                                            getAsyncCallback(), null);
                yield return ar.InsteadOf<string>();

                var total = log.EndGetErrors(ar);
                var count = errorEntryList.Count;

                if (maxDownloadCount > 0)
                {
                    var remaining = maxDownloadCount - (downloadCount + count);
                    if (remaining < 0)
                        count += remaining;
                }

                foreach (var entry in format.Entries(errorEntryList, 0, count, total))
                    yield return AsyncResultOr.Value(entry);

                downloadCount += count;

                response.Flush();

                //
                // Done if either the end of the list (no more errors found) or
                // the requested limit has been reached.
                //

                if (count == 0 || downloadCount == maxDownloadCount)
                {
                    if (count > 0)
                    {
                        foreach (var entry in format.Entries(new ErrorLogEntry[0], total)) // Terminator
                            yield return AsyncResultOr.Value(entry);
                    }
                    break;
                }

                //
                // Poll whether the client is still connected so data is not
                // unnecessarily sent to an abandoned connection. This check is 
                // only performed at certain intervals.
                //

                if (DateTime.Now - lastBeatTime > _beatPollInterval)
                {
                    if (!response.IsClientConnected)
                        break;

                    lastBeatTime = DateTime.Now;
                }

                //
                // Fetch next page of results.
                //

                errorEntryList.Clear();
            }
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

            public virtual IEnumerable<string> Header() { yield break; }

            public IEnumerable<string> Entries(IList<ErrorLogEntry> entries, int total)
            {
                return Entries(entries, 0, entries.Count, total);
            }

            public abstract IEnumerable<string> Entries(IList<ErrorLogEntry> entries, int index, int count, int total);
        }

        private sealed class CsvFormat : Format
        {
            public CsvFormat(HttpContextBase context) : 
                base(context) {}

            public override IEnumerable<string> Header()
            {
                var response = Context.Response;
                response.AppendHeader("Content-Type", "text/csv; header=present");
                response.AppendHeader("Content-Disposition", "attachment; filename=errorlog.csv");
                yield return "Application,Host,Time,Unix Time,Type,Source,User,Status Code,Message,URL,XMLREF,JSONREF\r\n";
            }

            public override IEnumerable<string> Entries(IList<ErrorLogEntry> entries, int index, int count, int total)
            {
                Debug.Assert(entries != null);
                Debug.Assert(index >= 0);
                Debug.Assert(index + count <= entries.Count);

                if (count == 0)
                    yield break;

                //
                // Setup to emit CSV records.
                //

                var writer = new StringWriter { NewLine = "\r\n" };
                var csv = new CsvWriter(writer);

                var culture = CultureInfo.InvariantCulture;
                var epoch = new DateTime(1970, 1, 1);

                //
                // For each error, emit a CSV record.
                //

                for (var i = index; i < count; i++)
                {
                    var entry = entries[i];
                    var error = entry.Error;
                    var time = error.Time.ToUniversalTime();
                    var query = "?id=" + Uri.EscapeDataString(entry.Id);
                    var requestUrl = ErrorLogPageFactory.GetRequestUrl(Context);

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

                yield return writer.ToString();
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

            public override IEnumerable<string> Header()
            {
                var callback = Context.Request.QueryString[Mask.EmptyString(null, "callback")] 
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

                    yield return "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
                    yield return @"
                    <html xmlns='http://www.w3.org/1999/xhtml'>
                    <head>
                        <title>Error Log in HTML-Wrapped JSONP Format</title>
                    </head>
                    <body>
                        <p>This page is primarily designed to be used in an IFRAME of a parent HTML document.</p>";
                }
            }

            public override IEnumerable<string> Entries(IList<ErrorLogEntry> entries, int index, int count, int total)
            {
                Debug.Assert(entries != null);
                Debug.Assert(index >= 0);
                Debug.Assert(index + count <= entries.Count);

                var writer = new StringWriter { NewLine = "\n" };

                if (_wrapped)
                {
                    writer.WriteLine("<script type='text/javascript' language='javascript'>");
                    writer.WriteLine("//<[!CDATA[");
                }
                
                writer.Write(_callback);
                writer.Write('(');

                var json = new JsonTextWriter(writer);
                json.Object()
                    .Member("total").Number(total)
                    .Member("errors").Array();

                var requestUrl = ErrorLogPageFactory.GetRequestUrl(Context);

                for (var i = index; i < count; i++)
                {
                    var entry = entries[i];
                    writer.WriteLine();
                    if (i == 0) writer.Write(' ');
                    writer.Write("  ");

                    var urlTemplate = new Uri(requestUrl, "{0}?id=" + Uri.EscapeDataString(entry.Id)).ToString();
                    
                    json.Object();
                            ErrorJson.EncodeMembers(entry.Error, json);
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

                yield return writer.ToString();
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

                var index = value.IndexOfAny(_reserved);

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
