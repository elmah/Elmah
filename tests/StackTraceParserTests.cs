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
    using System.Linq;
    using System.Text.RegularExpressions;
    using Mannex;
    using Xunit;

    #endregion

    public class StackTraceParserTests
    {
        [Fact]
        public void ParseDotNetStackTrace()
        {
            var actuals = StackTraceParser.Parse(@"
                Elmah.TestException: This is a test exception that can be safely ignored.
                    at Elmah.ErrorLogPageFactory.FindHandler(String name) in C:\ELMAH\src\Elmah\ErrorLogPageFactory.cs:line 126
                    at Elmah.ErrorLogPageFactory.GetHandler(HttpContext context, String requestType, String url, String pathTranslated) in C:\ELMAH\src\Elmah\ErrorLogPageFactory.cs:line 66
                    at System.Web.HttpApplication.MapHttpHandler(HttpContext context, String requestType, VirtualPath path, String pathTranslated, Boolean useAppConfig)
                    at System.Web.HttpApplication.MapHandlerExecutionStep.System.Web.HttpApplication.IExecutionStep.Execute()
                    at System.Web.HttpApplication.ExecuteStep(IExecutionStep step, Boolean& completedSynchronously)",
                (idx, len, txt) => new
                {
                    Index = idx,
                    Length = len,
                    Text = txt,
                },
                (type, method) => new
                {
                    Type   = type,
                    Method = method,
                },
                (type, name) => new
                {
                    Type = type,
                    Name = name,
                },
                (pl, ps) => new
                {
                    List = pl,
                    Parameters = ps,
                },
                (file, line) => new
                {
                    File = file,
                    Line = line,
                },
                (f, tm, p, fl) => new
                {
                    Frame = f.Text,
                    Type = tm.Type.Text,
                    Method = tm.Method.Text,
                    ParameterList = p.List.Text,
                    Parameters = string.Join(", ", p.Parameters.Select(e => e.Type.Text + " " + e.Name.Text).ToArray()),
                    File = fl.File.Text,
                    Line = fl.Line.Text,
                });

            var expectations = new[]
            {
                new
                {
                    Frame          = @"Elmah.ErrorLogPageFactory.FindHandler(String name) in C:\ELMAH\src\Elmah\ErrorLogPageFactory.cs:line 126",
                    Type           = @"Elmah.ErrorLogPageFactory", 
                    Method         = @"FindHandler", 
                    ParameterList = @"(String name)", 
                    Parameters     = @"String name", 
                    File           = @"C:\ELMAH\src\Elmah\ErrorLogPageFactory.cs",  
                    Line           = @"126",
                },
                new
                {
                    Frame         = @"Elmah.ErrorLogPageFactory.GetHandler(HttpContext context, String requestType, String url, String pathTranslated) in C:\ELMAH\src\Elmah\ErrorLogPageFactory.cs:line 66",
                    Type          = @"Elmah.ErrorLogPageFactory", 
                    Method        = @"GetHandler", 
                    ParameterList = @"(HttpContext context, String requestType, String url, String pathTranslated)", 
                    Parameters    = @"HttpContext context, String requestType, String url, String pathTranslated", 
                    File          = @"C:\ELMAH\src\Elmah\ErrorLogPageFactory.cs", 
                    Line          = @"66",
                },
                new
                {
                    Frame         = @"System.Web.HttpApplication.MapHttpHandler(HttpContext context, String requestType, VirtualPath path, String pathTranslated, Boolean useAppConfig)",
                    Type          = @"System.Web.HttpApplication", 
                    Method        = @"MapHttpHandler", 
                    ParameterList = @"(HttpContext context, String requestType, VirtualPath path, String pathTranslated, Boolean useAppConfig)", 
                    Parameters    = @"HttpContext context, String requestType, VirtualPath path, String pathTranslated, Boolean useAppConfig", 
                    File          = string.Empty,
                    Line          = string.Empty,
                },                 
                new                
                {                  
                    Frame         = @"System.Web.HttpApplication.MapHandlerExecutionStep.System.Web.HttpApplication.IExecutionStep.Execute()",
                    Type          = @"System.Web.HttpApplication.MapHandlerExecutionStep.System.Web.HttpApplication.IExecutionStep", 
                    Method        = @"Execute", 
                    ParameterList = @"()", 
                    Parameters    = string.Empty, 
                    File          = string.Empty,  
                    Line          = string.Empty, 
                },                 
                new                
                {                  
                    Frame         = @"System.Web.HttpApplication.ExecuteStep(IExecutionStep step, Boolean& completedSynchronously)",
                    Type          = @"System.Web.HttpApplication", 
                    Method        = @"ExecuteStep", 
                    ParameterList = @"(IExecutionStep step, Boolean& completedSynchronously)", 
                    Parameters    = @"IExecutionStep step, Boolean& completedSynchronously", 
                    File          = string.Empty,  
                    Line          = string.Empty, 
                },
            };

            Assert.Equal(expectations, actuals);            
        }

        [Fact] // See https://code.google.com/p/elmah/issues/detail?id=320
        public void ParseMonoStackTrace()
        {
            var actuals = StackTraceParser.Parse(@"
                System.Web.HttpException: The controller for path '/helloworld' was not found or does not implement IController.
                    at System.Web.Mvc.DefaultControllerFactory.GetControllerInstance (System.Web.Routing.RequestContext requestContext, System.Type controllerType) [0x00000] in <filename unknown>:0 
                    at System.Web.Mvc.DefaultControllerFactory.CreateController (System.Web.Routing.RequestContext requestContext, System.String controllerName) [0x00000] in <filename unknown>:0 
                    at System.Web.Mvc.MvcHandler.ProcessRequestInit (System.Web.HttpContextBase httpContext, IController& controller, IControllerFactory& factory) [0x00000] in <filename unknown>:0 
                    at System.Web.Mvc.MvcHandler.BeginProcessRequest (System.Web.HttpContextBase httpContext, System.AsyncCallback callback, System.Object state) [0x00000] in <filename unknown>:0 
                    at System.Web.Mvc.MvcHandler.BeginProcessRequest (System.Web.HttpContext httpContext, System.AsyncCallback callback, System.Object state) [0x00000] in <filename unknown>:0 
                    at System.Web.Mvc.MvcHandler.System.Web.IHttpAsyncHandler.BeginProcessRequest (System.Web.HttpContext context, System.AsyncCallback cb, System.Object extraData) [0x00000] in <filename unknown>:0 
                    at System.Web.HttpApplication+<Pipeline>c__Iterator3.MoveNext () [0x00000] in <filename unknown>:0",
                (idx, len, txt) => new
                {
                    Index = idx,
                    Length = len,
                    Text = txt,
                },
                (type, method) => new
                {
                    Type = type,
                    Method = method,
                },
                (type, name) => new
                {
                    Type = type,
                    Name = name,
                },
                (pl, ps) => new
                {
                    List = pl,
                    Parameters = ps,
                },
                (file, line) => new
                {
                    File = file,
                    Line = line,
                },
                (f, tm, p, fl) => new
                {
                    Type = tm.Type.Text,
                    Method = tm.Method.Text,
                    ParameterList = p.List.Text,
                    Parameters = string.Join(", ", p.Parameters.Select(e => e.Type.Text + " " + e.Name.Text).ToArray()),
                    File = fl.File.Text,
                    Line = fl.Line.Text,
                });

            var expectations = 
                from e in new[]
                {
                    new
                    {
                        Frame         = "System.Web.Mvc.DefaultControllerFactory.GetControllerInstance (System.Web.Routing.RequestContext requestContext, System.Type controllerType) [0x00000] in <filename unknown>:0 ",
                        Type          = "System.Web.Mvc.DefaultControllerFactory", 
                        Method        = "GetControllerInstance", 
                        ParameterList = "(System.Web.Routing.RequestContext requestContext, System.Type controllerType)", 
                        Parameters    = "System.Web.Routing.RequestContext requestContext, System.Type controllerType", 
                    },                 
                    new                
                    {                  
                        Frame         = "System.Web.Mvc.DefaultControllerFactory.CreateController (System.Web.Routing.RequestContext requestContext, System.String controllerName) [0x00000] in <filename unknown>:0", 
                        Type          = "System.Web.Mvc.DefaultControllerFactory", 
                        Method        = "CreateController", 
                        ParameterList = "(System.Web.Routing.RequestContext requestContext, System.String controllerName)", 
                        Parameters    = "System.Web.Routing.RequestContext requestContext, System.String controllerName", 
                    },                
                    new               
                    {                 
                        Frame         = "System.Web.Mvc.MvcHandler.ProcessRequestInit (System.Web.HttpContextBase httpContext, IController& controller, IControllerFactory& factory) [0x00000] in <filename unknown>:0", 
                        Type          = "System.Web.Mvc.MvcHandler", 
                        Method        = "ProcessRequestInit", 
                        ParameterList = "(System.Web.HttpContextBase httpContext, IController& controller, IControllerFactory& factory)", 
                        Parameters    = "System.Web.HttpContextBase httpContext, IController& controller, IControllerFactory& factory", 
                    },                
                    new               
                    {                 
                        Frame         = "System.Web.Mvc.MvcHandler.BeginProcessRequest (System.Web.HttpContextBase httpContext, System.AsyncCallback callback, System.Object state) [0x00000] in <filename unknown>:0", 
                        Type          = "System.Web.Mvc.MvcHandler", 
                        Method        = "BeginProcessRequest", 
                        ParameterList = "(System.Web.HttpContextBase httpContext, System.AsyncCallback callback, System.Object state)", 
                        Parameters    = "System.Web.HttpContextBase httpContext, System.AsyncCallback callback, System.Object state", 
                    },                
                    new               
                    {                 
                        Frame         = "System.Web.Mvc.MvcHandler.BeginProcessRequest (System.Web.HttpContext httpContext, System.AsyncCallback callback, System.Object state) [0x00000] in <filename unknown>:0", 
                        Type          = "System.Web.Mvc.MvcHandler", 
                        Method        = "BeginProcessRequest", 
                        ParameterList = "(System.Web.HttpContext httpContext, System.AsyncCallback callback, System.Object state)", 
                        Parameters    = "System.Web.HttpContext httpContext, System.AsyncCallback callback, System.Object state", 
                    },                
                    new               
                    {                 
                        Frame         = "System.Web.Mvc.MvcHandler.System.Web.IHttpAsyncHandler.BeginProcessRequest (System.Web.HttpContext context, System.AsyncCallback cb, System.Object extraData) [0x00000] in <filename unknown>:0", 
                        Type          = "System.Web.Mvc.MvcHandler.System.Web.IHttpAsyncHandler", 
                        Method        = "BeginProcessRequest", 
                        ParameterList = "(System.Web.HttpContext context, System.AsyncCallback cb, System.Object extraData)", 
                        Parameters    = "System.Web.HttpContext context, System.AsyncCallback cb, System.Object extraData", 
                    },                 
                    new                
                    {                  
                        Frame         = "System.Web.HttpApplication+<Pipeline>c__Iterator3.MoveNext () [0x00000] in <filename unknown>:0",
                        Type          = "System.Web.HttpApplication+<Pipeline>c__Iterator3", 
                        Method        = "MoveNext", 
                        ParameterList = "()", 
                        Parameters    = string.Empty, 
                    },
                }
                select new
                {
                    e.Type, e.Method, e.ParameterList, e.Parameters,
                    File = "filename unknown", Line = "0",
                };

            Assert.Equal(expectations, actuals);
        }
    }
}