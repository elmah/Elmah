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

[assembly: Elmah.Scc("$Id: HttpStatus.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;

    using HttpWorkerRequest = System.Web.HttpWorkerRequest;
    using CultureInfo = System.Globalization.CultureInfo;

    #endregion

    /// <summary>
    /// Represents an HTTP status (code plus reason) as per 
    /// <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec6.html#sec6.1">Section 6.1 of RFC 2616</a>.
    /// </summary>

    [ Serializable ]
    internal struct HttpStatus
    {
        public static readonly HttpStatus NotFound = new HttpStatus(404);
        public static readonly HttpStatus InternalServerError = new HttpStatus(500);

        private readonly int _code;
        private readonly string _reason;

        public HttpStatus(int code) :
            this(code, HttpWorkerRequest.GetStatusDescription(code)) {}

        public HttpStatus(int code, string reason)
        {
            Debug.Assert(code >= 100);
            Debug.Assert(code < 1000);
            Debug.AssertStringNotEmpty(reason);
            
            _code = code;
            _reason = reason;
        }

        public int Code
        {
            get { return _code; }
        }

        public string Reason
        {
            get { return Mask.NullString(_reason); }
        }

        public string StatusLine
        {
            get { return Code.ToString(CultureInfo.InvariantCulture) + " " + Reason; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is HttpStatus))
                return false;

            return Equals((HttpStatus) obj);
        }

        public bool Equals(HttpStatus other)
        {
            return Code.Equals(other.Code);
        }
        
        public override int GetHashCode()
        {
            return Code.GetHashCode();
        }

        public override string ToString()
        {
            return StatusLine;
        }
    }
}