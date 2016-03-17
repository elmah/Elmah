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

#if !NET_1_0 && !NET_1_1
#define ASYNC_ADONET
#endif

namespace Elmah
{
    #region Imports

    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Xml;

    using IDictionary = System.Collections.IDictionary;
    using IList = System.Collections.IList;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses Microsoft SQL 
    /// Server 2000 as its backing store.
    /// </summary>
    
    public class SqlErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        private const int _maxAppNameLength = 60;

#if ASYNC_ADONET
        private delegate RV Function<RV, A>(A a);
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public SqlErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            string connectionString = ConnectionStringHelper.GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the SQL error log.");

            _connectionString = connectionString;

            //
            // Set the application name as this implementation provides
            // per-application isolation over a single store.
            //

            string appName = Mask.NullString((string) config["applicationName"]);

            if (appName.Length > _maxAppNameLength)
            {
                throw new ApplicationException(string.Format(
                    "Application name is too long. Maximum length allowed is {0} characters.",
                    _maxAppNameLength.ToString("N0")));
            }

            ApplicationName = appName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public SqlErrorLog(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentException(null, "connectionString");
            
            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>
        
        public override string Name
        {
            get { return "Microsoft SQL Server Error Log"; }
        }

        /// <summary>
        /// Gets the connection string used by the log to connect to the database.
        /// </summary>
        
        public virtual string ConnectionString
        {
            get { return _connectionString; }
        }

        /// <summary>
        /// Logs an error to the database.
        /// </summary>
        /// <remarks>
        /// Use the stored procedure called by this implementation to set a
        /// policy on how long errors are kept in the log. The default
        /// implementation stores all errors for an indefinite time.
        /// </remarks>

        public override string Log(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            string errorXml = ErrorXml.EncodeString(error);
            Guid id = Guid.NewGuid();

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = Commands.LogError(
                id, this.ApplicationName, 
                error.HostName, error.Type, error.Source, error.Message, error.User,
                error.StatusCode, error.Time.ToUniversalTime(), errorXml))
            {
                command.Connection = connection;
                connection.Open();
                command.ExecuteNonQuery();
                return id.ToString();
            }
        }

        /// <summary>
        /// Returns a page of errors from the databse in descending order 
        /// of logged time.
        /// </summary>

        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = Commands.GetErrorsXml(this.ApplicationName, pageIndex, pageSize))
            {
                command.Connection = connection;
                connection.Open();

#if !NET_1_0 && !NET_1_1
                string xml = ReadSingleXmlStringResult(command.ExecuteReader());
                ErrorsXmlToList(xml, errorEntryList);
#else
                XmlReader reader = command.ExecuteXmlReader();
                try
                {
                    ErrorsXmlToList(reader, errorEntryList);
                }
                finally
                {
                    reader.Close();
                }
#endif

                int total;
                Commands.GetErrorsXmlOutputs(command, out total);
                return total;
            }
        }

        private static string ReadSingleXmlStringResult(SqlDataReader reader)
        {
            using (reader)
            {
                if (!reader.Read())
                    return null;

                //
                // See following MS KB article why the XML string is read 
                // and reconstructed in chunks:
                //
                // The XML data row is truncated at 2,033 characters when you use the SqlDataReader object
                // http://support.microsoft.com/kb/310378
                // 
                // When you read XML data from Microsoft SQL Server by using 
                // the SqlDataReader object, the XML in the first column of 
                // the first row is truncated at 2,033 characters. You 
                // expect all of the contents of the XML data to be 
                // contained in a single row and column. This behavior 
                // occurs because, for XML results greater than 2,033 
                // characters in length, SQL Server returns the XML in 
                // multiple rows of 2,033 characters each. 
                //
                // See also comment 18 in issue 129:
                // http://code.google.com/p/elmah/issues/detail?id=129#c18
                //

                StringBuilder sb = new StringBuilder(/* capacity */ 2033);
                do { sb.Append(reader.GetString(0)); } while (reader.Read());
                return sb.ToString();
            }
        }

#if ASYNC_ADONET

        /// <summary>
        /// Begins an asynchronous version of <see cref="GetErrors"/>.
        /// </summary>

        public override IAsyncResult BeginGetErrors(int pageIndex, int pageSize, IList errorEntryList,
            AsyncCallback asyncCallback, object asyncState)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            //
            // Modify the connection string on the fly to support async 
            // processing otherwise the asynchronous methods on the
            // SqlCommand will throw an exception. This ensures the
            // right behavior regardless of whether configured
            // connection string sets the Async option to true or not.
            //

            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(this.ConnectionString);
            csb.AsynchronousProcessing = true;
            SqlConnection connection = new SqlConnection(csb.ConnectionString);

            //
            // Create the command object with input parameters initialized
            // and setup to call the stored procedure.
            //

            SqlCommand command = Commands.GetErrorsXml(this.ApplicationName, pageIndex, pageSize);
            command.Connection = connection;

            //
            // Create a closure to handle the ending of the async operation
            // and retrieve results.
            //

            AsyncResultWrapper asyncResult = null;

            Function<int, IAsyncResult> endHandler = delegate
            {
                Debug.Assert(asyncResult != null);

                using (connection)
                using (command)
                {
                    string xml = ReadSingleXmlStringResult(command.EndExecuteReader(asyncResult.InnerResult));
                    ErrorsXmlToList(xml, errorEntryList);
                    int total;
                    Commands.GetErrorsXmlOutputs(command, out total);
                    return total;
                }
            };

            //
            // Open the connenction and execute the command asynchronously,
            // returning an IAsyncResult that wrap the downstream one. This
            // is needed to be able to send our own AsyncState object to
            // the downstream IAsyncResult object. In order to preserve the
            // one sent by caller, we need to maintain and return it from
            // our wrapper.
            //

            try
            {
                connection.Open();

                asyncResult = new AsyncResultWrapper(
                    command.BeginExecuteReader(
                        asyncCallback != null ? /* thunk */ delegate { asyncCallback(asyncResult); } : (AsyncCallback) null, 
                        endHandler), asyncState);

                return asyncResult;
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }
        }

        private void ErrorsXmlToList(string xml, IList errorEntryList)
        {
            if (xml == null || xml.Length == 0) 
                return;

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.CheckCharacters = false;
            settings.ConformanceLevel = ConformanceLevel.Fragment;

            using (XmlReader reader = XmlReader.Create(new StringReader(xml), settings))
                ErrorsXmlToList(reader, errorEntryList);
        }

        /// <summary>
        /// Ends an asynchronous version of <see cref="ErrorLog.GetErrors"/>.
        /// </summary>

        public override int EndGetErrors(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            AsyncResultWrapper wrapper = asyncResult as AsyncResultWrapper;

            if (wrapper == null)
                throw new ArgumentException("Unexepcted IAsyncResult type.", "asyncResult");

            Function<int, IAsyncResult> endHandler = (Function<int, IAsyncResult>) wrapper.InnerResult.AsyncState;
            return endHandler(wrapper.InnerResult);
        }

#endif

        private void ErrorsXmlToList(XmlReader reader, IList errorEntryList)
        {
            Debug.Assert(reader != null);

            if (errorEntryList != null)
            {
                while (reader.IsStartElement("error"))
                {
                    string id = reader.GetAttribute("errorId");
                    Error error = ErrorXml.Decode(reader);
                    errorEntryList.Add(new ErrorLogEntry(this, id, error));
                }
            }
        }

        /// <summary>
        /// Returns the specified error from the database, or null 
        /// if it does not exist.
        /// </summary>

        public override ErrorLogEntry GetError(string id)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Length == 0)
                throw new ArgumentException(null, "id");

            Guid errorGuid;

            try
            {
                errorGuid = new Guid(id);
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, "id", e);
            }

            string errorXml;

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = Commands.GetErrorXml(this.ApplicationName, errorGuid))
            {
                command.Connection = connection;
                connection.Open();
                errorXml = (string) command.ExecuteScalar();
            }

            if (errorXml == null)
                return null;

            Error error = ErrorXml.DecodeString(errorXml);
            return new ErrorLogEntry(this, id, error);
        }

        private sealed class Commands
        {
            private Commands() {}

            public static SqlCommand LogError(
                Guid id,
                string appName,
                string hostName,
                string typeName,
                string source,
                string message,
                string user,
                int statusCode,
                DateTime time,
                string xml)
            {
                SqlCommand command = new SqlCommand("ELMAH_LogError");
                command.CommandType = CommandType.StoredProcedure;

                SqlParameterCollection parameters = command.Parameters;

                parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier).Value = id;
                parameters.Add("@Application", SqlDbType.NVarChar, _maxAppNameLength).Value = appName;
                parameters.Add("@Host", SqlDbType.NVarChar, 30).Value = hostName;
                parameters.Add("@Type", SqlDbType.NVarChar, 100).Value = typeName;
                parameters.Add("@Source", SqlDbType.NVarChar, 60).Value = source;
                parameters.Add("@Message", SqlDbType.NVarChar, 500).Value = message;
                parameters.Add("@User", SqlDbType.NVarChar, 50).Value = user;
                parameters.Add("@AllXml", SqlDbType.NText).Value = xml;
                parameters.Add("@StatusCode", SqlDbType.Int).Value = statusCode;
                parameters.Add("@TimeUtc", SqlDbType.DateTime).Value = time;

                return command;
            }

            public static SqlCommand GetErrorXml(string appName, Guid id)
            {
                SqlCommand command = new SqlCommand("ELMAH_GetErrorXml");
                command.CommandType = CommandType.StoredProcedure;

                SqlParameterCollection parameters = command.Parameters;
                parameters.Add("@Application", SqlDbType.NVarChar, _maxAppNameLength).Value = appName;
                parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier).Value = id;

                return command;
            }

            public static SqlCommand GetErrorsXml(string appName, int pageIndex, int pageSize)
            {
                SqlCommand command = new SqlCommand("ELMAH_GetErrorsXml");
                command.CommandType = CommandType.StoredProcedure;

                SqlParameterCollection parameters = command.Parameters;

                parameters.Add("@Application", SqlDbType.NVarChar, _maxAppNameLength).Value = appName;
                parameters.Add("@PageIndex", SqlDbType.Int).Value = pageIndex;
                parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
                parameters.Add("@TotalCount", SqlDbType.Int).Direction = ParameterDirection.Output;

                return command;
            }

            public static void GetErrorsXmlOutputs(SqlCommand command, out int totalCount)
            {
                Debug.Assert(command != null);

                totalCount = (int) command.Parameters["@TotalCount"].Value;
            }
        }

        /// <summary>
        /// An <see cref="IAsyncResult"/> implementation that wraps another.
        /// </summary>

        private sealed class AsyncResultWrapper : IAsyncResult
        {
            private readonly IAsyncResult _inner;
            private readonly object _asyncState;

            public AsyncResultWrapper(IAsyncResult inner, object asyncState)
            {
                _inner = inner;
                _asyncState = asyncState;
            }

            public IAsyncResult InnerResult
            {
                get { return _inner; }
            }

            public bool IsCompleted
            {
                get { return _inner.IsCompleted; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _inner.AsyncWaitHandle; }
            }

            public object AsyncState
            {
                get { return _asyncState; }
            }

            public bool CompletedSynchronously
            {
                get { return _inner.CompletedSynchronously; }
            }
        }
    }
}
