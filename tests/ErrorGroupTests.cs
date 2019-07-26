namespace Elmah.Tests
{
    extern alias e;
    using System;
    using System.Threading;
    using Xunit;

    public class ErrorGroupTests
    {
        [Fact]
        public void AddsOneErrorWithoutTimerEnabled()
        {
            var subject = new ErrorGroup("key", (x) => { }, 0, 0);
            Assert.Equal(0, subject.Errors.Count);
            subject.Add(new e::Elmah.Error());
            Assert.Equal(1, subject.Errors.Count);
            Assert.False(subject.ErrorRetentionTimer.Enabled);
        }

        [Fact]
        public void ExceedsMaxOccurrences()
        {
            var isFlushed = false;
            var callback = new Action<ErrorGroup>(x => isFlushed = true);
            var subject = new ErrorGroup("key", callback, errorGroupFlushTimeInMilliseconds: 0, errorGroupFlushMaxOccurrences: 3);
            Assert.Equal(0, subject.Errors.Count);
            subject.Add(new e::Elmah.Error());
            Assert.Equal(1, subject.Errors.Count);
            Assert.False(isFlushed);
            subject.Add(new e::Elmah.Error());
            Assert.False(isFlushed);
            subject.Add(new e::Elmah.Error());
            Assert.True(isFlushed);
            Assert.False(subject.ErrorRetentionTimer.Enabled);
        }

        [Fact]
        public void ExceedsTimeLimit()
        {
            var isFlushed = false;
            var callback = new Action<ErrorGroup>(x => isFlushed = true);
            var subject = new ErrorGroup("key", callback, errorGroupFlushTimeInMilliseconds: 500, errorGroupFlushMaxOccurrences: 0);
            Assert.Equal(0, subject.Errors.Count);
            Assert.False(subject.ErrorRetentionTimer.Enabled);
            subject.Add(new e::Elmah.Error());
            Assert.Equal(1, subject.Errors.Count);
            Assert.True(subject.ErrorRetentionTimer.Enabled);
            Assert.False(isFlushed);
            Thread.Sleep(550);
            Assert.False(subject.ErrorRetentionTimer.Enabled);
            Assert.True(isFlushed);
        }

        [Fact]
        public void PerformsFlush()
        {
            var isFlushed = false;
            var callback = new Action<ErrorGroup>(x => isFlushed = true);
            var subject = new ErrorGroup("key", callback, errorGroupFlushTimeInMilliseconds: int.MaxValue, errorGroupFlushMaxOccurrences: Int32.MaxValue);
            subject.Add(new e::Elmah.Error());
            Assert.False(isFlushed);
            Assert.Equal(1, subject.Errors.Count);
            subject.Flush();
            Assert.False(subject.ErrorRetentionTimer.Enabled);
            Assert.Equal(0, subject.Errors.Count);
            Assert.True(isFlushed);
        }
    }
}