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

[assembly: Elmah.Scc("$Id: HttpStatus.cs 640 2009-06-01 17:22:02Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Runtime.Remoting.Messaging;
    using Mannex;

    #endregion

    /// <summary>
    /// Represents an HTTP status (code plus reason) as per 
    /// <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec6.html#sec6.1">Section 6.1 of RFC 2616</a>.
    /// </summary>

    [ Serializable ]
    class HttpStatus : IEquatable<HttpStatus>
    {
        public static readonly HttpStatus Continue                       = new HttpStatus(100, "Continue");
        public static readonly HttpStatus SwitchingProtocols             = new HttpStatus(101, "Switching Protocols");
        // ReSharper disable once InconsistentNaming
        public static readonly HttpStatus OK                             = new HttpStatus(200, "OK");
        public static readonly HttpStatus Created                        = new HttpStatus(201, "Created");
        public static readonly HttpStatus Accepted                       = new HttpStatus(202, "Accepted");
        public static readonly HttpStatus NonAuthoritativeInformation    = new HttpStatus(203, "Non-Authoritative Information");
        public static readonly HttpStatus NoContent                      = new HttpStatus(204, "No Content");
        public static readonly HttpStatus ResetContent                   = new HttpStatus(205, "Reset Content");
        public static readonly HttpStatus PartialContent                 = new HttpStatus(206, "Partial Content");
        public static readonly HttpStatus MultipleChoices                = new HttpStatus(300, "Multiple Choices");
        public static readonly HttpStatus MovedPermanently               = new HttpStatus(301, "Moved Permanently");
        public static readonly HttpStatus Found                          = new HttpStatus(302, "Found");
        public static readonly HttpStatus SeeOther                       = new HttpStatus(303, "See Other");
        public static readonly HttpStatus NotModified                    = new HttpStatus(304, "Not Modified");
        public static readonly HttpStatus UseProxy                       = new HttpStatus(305, "Use Proxy");
        public static readonly HttpStatus TemporaryRedirect              = new HttpStatus(307, "Temporary Redirect");
        public static readonly HttpStatus BadRequest                     = new HttpStatus(400, "Bad Request");
        public static readonly HttpStatus Unauthorized                   = new HttpStatus(401, "Unauthorized");
        public static readonly HttpStatus PaymentRequired                = new HttpStatus(402, "Payment Required");
        public static readonly HttpStatus Forbidden                      = new HttpStatus(403, "Forbidden");
        public static readonly HttpStatus NotFound                       = new HttpStatus(404, "Not Found");
        public static readonly HttpStatus MethodNotAllowed               = new HttpStatus(405, "Method Not Allowed");
        public static readonly HttpStatus NotAcceptable                  = new HttpStatus(406, "Not Acceptable");
        public static readonly HttpStatus ProxyAuthenticationRequired    = new HttpStatus(407, "Proxy Authentication Required");
        public static readonly HttpStatus RequestTimeout                 = new HttpStatus(408, "Request Timeout");
        public static readonly HttpStatus Conflict                       = new HttpStatus(409, "Conflict");
        public static readonly HttpStatus Gone                           = new HttpStatus(410, "Gone");
        public static readonly HttpStatus LengthRequired                 = new HttpStatus(411, "Length Required");
        public static readonly HttpStatus PreconditionFailed             = new HttpStatus(412, "Precondition Failed");
        public static readonly HttpStatus RequestEntityTooLarge          = new HttpStatus(413, "Request Entity Too Large");
        public static readonly HttpStatus RequestUriTooLong              = new HttpStatus(414, "Request-URI Too Long");
        public static readonly HttpStatus UnsupportedMediaType           = new HttpStatus(415, "Unsupported Media Type");
        public static readonly HttpStatus RequestedRangeNotSatisfiable   = new HttpStatus(416, "Requested Range Not Satisfiable");
        public static readonly HttpStatus ExpectationFailed              = new HttpStatus(417, "Expectation Failed");
        public static readonly HttpStatus InternalServerError            = new HttpStatus(500, "Internal Server Error");
        public static readonly HttpStatus NotImplemented                 = new HttpStatus(501, "Not Implemented");
        public static readonly HttpStatus BadGateway                     = new HttpStatus(502, "Bad Gateway");
        public static readonly HttpStatus ServiceUnavailable             = new HttpStatus(503, "Service Unavailable");
        public static readonly HttpStatus GatewayTimeout                 = new HttpStatus(504, "Gateway Timeout");
        public static readonly HttpStatus HttpVersionNotSupported        = new HttpStatus(505, "HTTP Version Not Supported");

        static readonly HttpStatus[][] MapMap;

        static HttpStatus()
        {
            MapMap = new[]
            {
                /* 1xx */ new[] { Continue, SwitchingProtocols }, 
                /* 2xx */ new[] { OK, Created, Accepted, NonAuthoritativeInformation, NoContent, ResetContent, PartialContent }, 
                /* 3xx */ new[] { MultipleChoices, MovedPermanently, Found, SeeOther, NotModified, UseProxy, null, TemporaryRedirect }, 
                /* 4xx */ new[] { BadRequest, Unauthorized, PaymentRequired, Forbidden, NotFound, MethodNotAllowed, NotAcceptable, ProxyAuthenticationRequired, RequestTimeout, Conflict, Gone, LengthRequired, PreconditionFailed, RequestEntityTooLarge, RequestUriTooLong, UnsupportedMediaType, RequestedRangeNotSatisfiable, ExpectationFailed }, 
                /* 5xx */ new[] { InternalServerError, NotImplemented, BadGateway, ServiceUnavailable, GatewayTimeout, HttpVersionNotSupported }, 
            };            
        }

        public static HttpStatus FromCode(int code)
        {
            if (code < 100) throw new ArgumentOutOfRangeException("code", code, null);
            var h = (code / 100) - 1;
            if (h >= MapMap.Length) 
                return new HttpStatus(code);
            var map = MapMap[h];
            var r = code % 100;
            return r < map.Length ? map[r] : new HttpStatus(code);
        }

        HttpStatus(int code, string reason = null)
        {
            Debug.Assert(code >= 100);
            Debug.Assert(code < 600);
            Debug.AssertStringNotEmpty(reason);
            
            Code = code;
            Reason = reason ?? "HTTP Status Code " + code.ToInvariantString();
        }

        public int Code { get; private set; }
        public string Reason { get; private set; }

        public string StatusLine
        {
            get { return Code.ToInvariantString() + " " + Reason; }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as HttpStatus);
        }

        public bool Equals(HttpStatus other)
        {
            return other != null && Code.Equals(other.Code);
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