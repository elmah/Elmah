namespace Elmah.Tests
{
    extern alias e;
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Web;
    using Moq;
    using Xunit;
    using Error = e::Elmah.Error;
    using XmlFileErrorLog = e::Elmah.XmlFileErrorLog;

    public class ErrorGroupCollectionTests
    {
        [Fact]
        public void AddsOneError()
        {
            var subject = new ErrorGroupCollection(
                (e) => { },
                errorGroupingMethods: new ErrorGroupingMethod[]{ErrorGroupingMethod.Exception},
                errorGroupFlushMaxOccurrences: 0,
                errorGroupFlushTimeInMilliseconds: 0
                );
            Assert.Equal(0, subject.Storage.Count);
            subject.Add(new e::Elmah.Error(new Exception()));
            Assert.Equal(1, subject.Storage.Count);
        }

        [Fact]
        public void AddsTwoIdenticalErrors()
        {
            var subject = new ErrorGroupCollection(
                (e) => { },
                errorGroupingMethods: new ErrorGroupingMethod[]{ErrorGroupingMethod.Exception},
                errorGroupFlushMaxOccurrences: 0,
                errorGroupFlushTimeInMilliseconds: 0
                );
            Assert.Equal(0, subject.Storage.Count);
            subject.Add(new e::Elmah.Error(new Exception()));
            subject.Add(new e::Elmah.Error(new Exception()));
            Assert.Equal(1, subject.Storage.Count);
            Assert.Equal(2, subject.Storage.First().Value.Errors.Count);
        }

        [Fact]
        public void AddsTwoDifferentErrors()
        {
            var subject = new ErrorGroupCollection(
                (e) => { },
                errorGroupingMethods: new ErrorGroupingMethod[]{ErrorGroupingMethod.Exception},
                errorGroupFlushMaxOccurrences: 0,
                errorGroupFlushTimeInMilliseconds: 0
                );
            Assert.Equal(0, subject.Storage.Count);
            subject.Add(new e::Elmah.Error(new ArgumentException()));
            subject.Add(new e::Elmah.Error(new NullReferenceException()));
            Assert.Equal(2, subject.Storage.Count);
        }

        private HttpContextBase GetMockHttpContextBaseWithUrl(string url)
        {
            var serverVariables = new NameValueCollection();
            serverVariables.Add("URL", url);
            var mockRequest = new Mock<HttpRequestBase>();
            mockRequest.SetupGet(x => x.ServerVariables).Returns(serverVariables);
            var result = new Mock<HttpContextBase>();
            result.SetupGet(x => x.Request).Returns(mockRequest.Object);
            return result.Object;
        }

        [Fact]
        public void GetErrorGroupKeyForUrl()
        {
            var subject = new ErrorGroupCollection(
                (e) => { },
                errorGroupingMethods: new ErrorGroupingMethod[]{ErrorGroupingMethod.Url},
                errorGroupFlushMaxOccurrences: 0,
                errorGroupFlushTimeInMilliseconds: 0
                );
            var error1 = new Error(new ArgumentException(), GetMockHttpContextBaseWithUrl("foo"));
            Assert.Equal("foo", subject.GetErrorGroupKey(error1));
        }

        [Fact]
        public void GetErrorGroupKeyForUrlWithQuerystring()
        {
            var subject = new ErrorGroupCollection(
                (e) => { },
                errorGroupingMethods: new ErrorGroupingMethod[]{ErrorGroupingMethod.Url},
                errorGroupFlushMaxOccurrences: 0,
                errorGroupFlushTimeInMilliseconds: 0
                );
            var error1 = new Error(new ArgumentException(), GetMockHttpContextBaseWithUrl("~/foo?bar=123&baz=456"));
            Assert.Equal("~/foo", subject.GetErrorGroupKey(error1));
        }

        [Fact]
        public void GetErrorGroupKeyForException()
        {
            var subject = new ErrorGroupCollection(
                (e) => { },
                errorGroupingMethods: new ErrorGroupingMethod[]{ErrorGroupingMethod.Exception},
                errorGroupFlushMaxOccurrences: 0,
                errorGroupFlushTimeInMilliseconds: 0
                );
            var error1 = new Error(new ArgumentException(), GetMockHttpContextBaseWithUrl("foo"));
            Assert.Equal("System.ArgumentException", subject.GetErrorGroupKey(error1));
        }

        [Fact]
        public void GetErrorGroupKeyForExceptionAndUrl()
        {
            var subject = new ErrorGroupCollection(
                (e) => { },
                errorGroupingMethods: new ErrorGroupingMethod[]{ErrorGroupingMethod.Exception, ErrorGroupingMethod.Url},
                errorGroupFlushMaxOccurrences: 0,
                errorGroupFlushTimeInMilliseconds: 0
                );
            var error1 = new Error(new ArgumentException(), GetMockHttpContextBaseWithUrl("foo"));
            Assert.Equal("foo~System.ArgumentException", subject.GetErrorGroupKey(error1));
        }
    }
}