#region Microsoft Public License (Ms-PL)
//
// This portion of the code is licensed under Ms-PL[1]. It is derivative
// work of the implementation presented at, "How to implement the 
// IAsyncResult design pattern[2]".
//
// [1] http://www.opensource.org/licenses/MS-PL
// [2] http://blogs.msdn.com/b/nikos/archive/2011/03/14/how-to-implement-iasyncresult-in-another-way.aspx
//
#endregion

namespace Elmah
{
    #region Imports

    using System;
    using System.Threading;

    #endregion

    class AsyncResult : IAsyncResult
    {
        // Fields set at construction which never change while 
        // operation is pending
        private readonly AsyncCallback _asyncCallback;
        private readonly object _asyncState;

        // Fields set at construction which do change after 
        // operation completes
        private const int StatePending = 0;
        private const int StateCompletedSynchronously = 1;
        private const int StateCompletedAsynchronously = 2;
        private int _completedState = StatePending;

        // Field that may or may not get set depending on usage
        private ManualResetEvent _waitHandle;

        // Fields set when operation completes
        private Exception _exception;

        /// <summary>
        /// The object which started the operation.
        /// </summary>
        private readonly object _owner;

        /// <summary>
        /// Used to verify the BeginXXX and EndXXX calls match.
        /// </summary>
        private string _operationId;

        protected internal AsyncResult(
            AsyncCallback asyncCallback,
            object state,
            object owner,
            string operationId)
        {
            _asyncCallback = asyncCallback;
            _asyncState = state;
            _owner = owner;
            _operationId =
                String.IsNullOrEmpty(operationId) ? String.Empty : operationId;
        }

        internal virtual void Process()
        {
            // Starts processing of the operation.
        }

        public bool Complete()
        {
            return Complete(null);
        }

        public bool Complete(Exception exception)
        {
            return Complete(exception, false /*completedSynchronously*/);
        }

        public bool Complete(Exception exception, bool completedSynchronously)
        {
            var result = false;

            // The m_CompletedState field MUST be set prior calling the callback
            var prevState = Interlocked.Exchange(ref _completedState,
                completedSynchronously ? StateCompletedSynchronously :
                StateCompletedAsynchronously);
            
            if (prevState == StatePending)
            {
                // Passing null for exception means no error occurred. 
                // This is the common case
                _exception = exception;

                // Do any processing before completion.
                Completing(exception, completedSynchronously);

                // If the event exists, set it
                if (_waitHandle != null) _waitHandle.Set();

                MakeCallback(_asyncCallback, this);

                // Do any final processing after completion
                Completed(exception, completedSynchronously);

                result = true;
            }
            /*
                else
                {
                    throw new InvalidOperationException(
                        "You can set a result only once");
                }
            */
            return result;
        }

        private void CheckUsage(object owner, string operationId)
        {
            if (!ReferenceEquals(owner, _owner))
            {
                throw new InvalidOperationException(
                    "End was called on a different object than Begin.");
            }

            // Reuse the operation ID to detect multiple calls to end.
            if (ReferenceEquals(null, _operationId))
            {
                throw new InvalidOperationException(
                    "End was called multiple times for this operation.");
            }

            if (!string.Equals(operationId, _operationId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "End operation type was different than Begin.");
            }

            // Mark that End was already called.
            _operationId = null;
        }

        public static void End(
            IAsyncResult result, object owner, string operationId)
        {
            var asyncResult = result as AsyncResult;
            if (asyncResult == null)
            {
                throw new ArgumentException(
                    "Result passed represents an operation not supported " +
                    "by this framework.",
                    "result");
            }

            asyncResult.CheckUsage(owner, operationId);

            // This method assumes that only 1 thread calls EndInvoke 
            // for this object
            if (!asyncResult.IsCompleted)
            {
                // If the operation isn't done, wait for it
                asyncResult.AsyncWaitHandle.WaitOne();
                asyncResult.AsyncWaitHandle.Close();
                asyncResult._waitHandle = null;  // Allow early GC
            }

            // Operation is done: if an exception occurred, throw it
            if (asyncResult._exception != null)
            {
                //throw new ArgumentException("IAsyncResult object did not come from the corresponding async method on this type.", "asyncResult");
                //
                // IMPORTANT! The End method on SynchronousAsyncResult will 
                // throw an exception if that's what Log did when 
                // BeginLog called it. The unforunate side effect of this is
                // the stack trace information for the exception is lost and 
                // reset to this point. There seems to be a basic failure in the 
                // framework to accommodate for this case more generally. One 
                // could handle this through a custom exception that wraps the 
                // original exception, but this assumes that an invocation will 
                // only throw an exception of that custom type.
                //

                throw asyncResult._exception;
            }
        }

        #region Implementation of IAsyncResult

        public Object AsyncState { get { return _asyncState; } }

        public bool CompletedSynchronously
        {
            get
            {
                return Thread.VolatileRead(ref _completedState) ==
                    StateCompletedSynchronously;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_waitHandle == null)
                {
                    var done = IsCompleted;
                    var mre = new ManualResetEvent(done);
                    if (Interlocked.CompareExchange(ref _waitHandle, mre, null) != null)
                    {
                        // Another thread created this object's event; dispose 
                        // the event we just created
                        mre.Close();
                    }
                    else
                    {
                        if (!done && IsCompleted)
                        {
                            // If the operation wasn't done when we created 
                            // the event but now it is done, set the event
                            _waitHandle.Set();
                        }
                    }
                }
                return _waitHandle;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return Thread.VolatileRead(ref _completedState) != StatePending;
            }
        }
        #endregion

        protected virtual void Completing(
            Exception exception, bool completedSynchronously) {}

        protected virtual void MakeCallback(
            AsyncCallback callback, AsyncResult result)
        {
            // If a callback method was set, call it
            if (callback != null)
                callback(result);
        }

        protected virtual void Completed(
            Exception exception, bool completedSynchronously) {}
    }

    class AsyncResult<T> : AsyncResult
    {
        // Field set when operation completes
        private T _result;

        public void SetResult(T result)
        {
            _result = result;
        }

        public AsyncResult(AsyncCallback asyncCallback, object state, object owner, string operationId) :
            base(asyncCallback, state, owner, operationId) {}

        new public static T End(IAsyncResult result, object owner, string operationId)
        {
            var asyncResult = result as AsyncResult<T>;
            if (asyncResult == null)
            {
                throw new ArgumentException(
                    "Result passed represents an operation not supported " +
                    "by this framework.",
                    "result");
            }

            // Wait until operation has completed 
            AsyncResult.End(result, owner, operationId);

            // Return the result (if above didn't throw)
            return asyncResult._result;
        }
    }
}
