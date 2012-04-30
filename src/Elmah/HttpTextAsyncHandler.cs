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

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Web;

    #endregion

    static class HttpTextAsyncHandler
    {
        public static Func<HttpContextBase, Func<AsyncCallback>, IEnumerable<IAsyncResult>> Create(Func<HttpContextBase, Func<AsyncCallback>, IEnumerable<AsyncResultOr<string>>> handler)
        {
            return (context, getAsyncCallback) => ProcessRequest(context, getAsyncCallback, handler);
        }

        private static IEnumerable<IAsyncResult> ProcessRequest(
            HttpContextBase context, 
            Func<AsyncCallback> getAsyncCallback,
            Func<HttpContextBase, Func<AsyncCallback>, IEnumerable<AsyncResultOr<string>>> handler)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (getAsyncCallback == null) throw new ArgumentNullException("getAsyncCallback");

            if (handler == null)
                yield break;

            var response = context.Response;
            var output = response.OutputStream;
            var encoding = response.ContentEncoding;
            var encoder = encoding.GetEncoder();

            var chars = new char[2048];
            var bytes = new byte[encoding.GetMaxByteCount(chars.Length)];

            foreach (var item in handler(context, getAsyncCallback))
            {
                string text;
                if (!item.HasValue)
                {
                    yield return item.AsyncResult;
                }
                else if ((text = item.Value) != null)
                {
                    int charsRead, textIndex = 0;

                    do
                    {
                        charsRead = Math.Min(chars.Length, text.Length - textIndex);
                        text.CopyTo(textIndex, chars, 0, charsRead);
                        textIndex += charsRead;

                        var completed = false;
                        var charIndex = 0;

                        while (!completed)
                        {
                            var flush = charsRead == 0;
                            int bytesUsed, charsUsed;
                            encoder.Convert(chars, charIndex, charsRead - charIndex,
                                            bytes, 0, bytes.Length, flush,
                                            out charsUsed, out bytesUsed,
                                            out completed);

                            var ar = output.BeginWrite(bytes, 0, bytesUsed, getAsyncCallback(), null);
                            yield return ar;
                            output.EndWrite(ar);

                            charIndex += charsUsed;
                        }
                    }
                    while (charsRead != 0);
                }
            }

            output.Flush();
        }
    }
}