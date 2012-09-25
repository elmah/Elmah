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

namespace Elmah.Tests
{
    #region Imports

    using System;
    using System.Collections;
    using System.Linq;
    using System.Net;
    using System.Web;
    using Moq;
    using Xunit;

    #endregion

    public class ErrorLogPageFactoryTests
    {
        [Fact]
        public void CustomAuthorizationDenial()
        {
            var result = TestAuthorization(false, true, (ctx, hh) => new
            {
                Context = ctx, 
                Handler = hh
            });
            Assert.Null(result.Handler);
            AssertStatus(HttpStatusCode.Forbidden, result.Context.Response);
        }

        [Fact]
        public void CustomAuthorizationGrant()
        {
            var result = TestAuthorization(true, true, (ctx, hh) => new
            {
                Context = ctx, 
                Handler = hh
            });
            Assert.NotNull(result.Handler);
        }

        [Fact]
        public void LocalRequestIsAuthorized()
        {
            var handler = TestAuthorization(null, true, (_, hh) => hh);
            Assert.NotNull(handler);
        }

        [Fact]
        public void RemoteRequestIsUnauthorized()
        {
            var result = TestAuthorization(null, false, (ctx, hh) => new
            {
                Context = ctx,
                Handler = hh
            });
            Assert.Null(result.Handler);
            AssertStatus(HttpStatusCode.Forbidden, result.Context.Response);
        }

        static void AssertStatus(HttpStatusCode expected, HttpResponseBase response)
        {
            var status = response.Status;
            Assert.NotNull(status);
            var code = Enum.Parse(typeof(HttpStatusCode), status.Split().First());
            Assert.Equal(expected, code);
        }

        static T TestAuthorization<T>(bool? allow, bool isLocalRequest, Func<HttpContextBase, IHttpHandler, T> resultor)
        {
            var mocks = new
            {
                Context = new Mock<HttpContextBase> { DefaultValue = DefaultValue.Mock },
                Response = new Mock<HttpResponseBase> { DefaultValue = DefaultValue.Mock },
            };

            using (var app = allow == null
                           ? new HttpApplication()
                           : new Application
                           {
                               AuthorizationHandler = _ => allow.Value
                           })
            {
                mocks.Context.Setup(c => c.ApplicationInstance).Returns(app);
                mocks.Context.Setup(c => c.Request.PathInfo).Returns("/");
                mocks.Context.Setup(c => c.Request.IsLocal).Returns(isLocalRequest);
                mocks.Response.SetupAllProperties();
                mocks.Context.Setup(c => c.Response).Returns(mocks.Response.Object);
                mocks.Context.Setup(c => c.Items).Returns(new Hashtable());

                var context = mocks.Context.Object;
                var factory = new ErrorLogPageFactory();
                var handler = factory.GetHandler(context, null, null, null);
                return resultor(context, handler);
            }
        }

        sealed class Application : HttpApplication, IRequestAuthorizationHandler
        {
            public Func<HttpContextBase, bool> AuthorizationHandler { private get; set; }

            bool IRequestAuthorizationHandler.Authorize(HttpContextBase context)
            {
                var handler = AuthorizationHandler ?? delegate { throw new NotImplementedException(); };
                return handler(context);
            }
        }
    }
}