#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//		Nick Berard, http://www.coderjournal.com
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

[assembly: Elmah.Scc("$Id: MySqlErrorLog.cs 925 2011-12-23 22:46:09Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Data;

    using MySql.Data.MySqlClient;

    using IDictionary = System.Collections.IDictionary;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses 
    /// <a href="http://www.mysql.com/">MySQL</a> as its backing store.
    /// </summary>

    public class MySqlErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        private const int _maxAppNameLength = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public MySqlErrorLog(IDictionary config)
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

            var appName = config.Find("applicationName", string.Empty);

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

        public MySqlErrorLog(string connectionString)
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
            get { return "MySQL Server Error Log"; }
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

            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            using (MySqlCommand command = Commands.LogError(
                id, ApplicationName,
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
        public override int GetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            using (MySqlCommand command = Commands.GetErrorsXml(ApplicationName, pageIndex, pageSize))
            {
                command.Connection = connection;
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    Debug.Assert(reader != null);

                    if (errorEntryList != null)
                    {
                        while (reader.Read())
                        {
                            Error error = new Error();

                            error.ApplicationName = reader["Application"].ToString();
                            error.HostName = reader["Host"].ToString();
                            error.Type = reader["Type"].ToString();
                            error.Source = reader["Source"].ToString();
                            error.Message = reader["Message"].ToString();
                            error.User = reader["User"].ToString();
                            error.StatusCode = Convert.ToInt32(reader["StatusCode"]);
                            error.Time = Convert.ToDateTime(reader.GetString("TimeUtc")).ToLocalTime();

                            errorEntryList.Add(new ErrorLogEntry(this, reader["ErrorId"].ToString(), error));
                        }
                    }
                    reader.Close();
                }

                return (int)command.Parameters["TotalCount"].Value;
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

            string errorXml = null;

            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            using (MySqlCommand command = Commands.GetErrorXml(ApplicationName, errorGuid))
            {
                command.Connection = connection;
                connection.Open();

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    Debug.Assert(reader != null);

                    while (reader.Read())
                    {
                        errorXml = reader.GetString("AllXml");
                    }
                    reader.Close();
                }
            }

            if (errorXml == null)
                return null;

            Error error = ErrorXml.DecodeString(errorXml);
            return new ErrorLogEntry(this, id, error);
        }

        private static class Commands
        {
            public static MySqlCommand LogError(
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
                MySqlCommand command = new MySqlCommand("elmah_LogError");
                command.CommandType = CommandType.StoredProcedure;

                MySqlParameterCollection parameters = command.Parameters;
                parameters.Add("ErrorId", MySqlDbType.String, 36).Value = id.ToString();
                parameters.Add("Application", MySqlDbType.VarChar, _maxAppNameLength).Value = appName.Substring(0, Math.Min(_maxAppNameLength, appName.Length));
                parameters.Add("Host", MySqlDbType.VarChar, 30).Value = hostName.Substring(0, Math.Min(30, hostName.Length));
                parameters.Add("Type", MySqlDbType.VarChar, 100).Value = typeName.Substring(0, Math.Min(100, typeName.Length));
                parameters.Add("Source", MySqlDbType.VarChar, 60).Value = source.Substring(0, Math.Min(60, source.Length));
                parameters.Add("Message", MySqlDbType.VarChar, 500).Value = message.Substring(0, Math.Min(500, message.Length));
                parameters.Add("User", MySqlDbType.VarChar, 50).Value = user.Substring(0, Math.Min(50, user.Length));
                parameters.Add("AllXml", MySqlDbType.Text).Value = xml;
                parameters.Add("StatusCode", MySqlDbType.Int32).Value = statusCode;
                parameters.Add("TimeUtc", MySqlDbType.Datetime).Value = time;

                return command;
            }

            public static MySqlCommand GetErrorXml(string appName, Guid id)
            {
                MySqlCommand command = new MySqlCommand("elmah_GetErrorXml");
                command.CommandType = CommandType.StoredProcedure;

                MySqlParameterCollection parameters = command.Parameters;
                parameters.Add("Id", MySqlDbType.String, 36).Value = id.ToString();
                parameters.Add("App", MySqlDbType.VarChar, _maxAppNameLength).Value = appName.Substring(0, Math.Min(_maxAppNameLength, appName.Length));

                return command;
            }

            public static MySqlCommand GetErrorsXml(string appName, int pageIndex, int pageSize)
            {
                MySqlCommand command = new MySqlCommand("elmah_GetErrorsXml");
                command.CommandType = CommandType.StoredProcedure;

                MySqlParameterCollection parameters = command.Parameters;
                parameters.Add("App", MySqlDbType.VarChar, _maxAppNameLength).Value = appName.Substring(0, Math.Min(_maxAppNameLength, appName.Length));
                parameters.Add("PageIndex", MySqlDbType.Int32).Value = pageIndex;
                parameters.Add("PageSize", MySqlDbType.Int32).Value = pageSize;
                parameters.Add("TotalCount", MySqlDbType.Int32).Direction = ParameterDirection.Output;

                return command;
            }

            public static void GetErrorsXmlOutputs(MySqlCommand command, out int totalCount)
            {
                Debug.Assert(command != null);

                totalCount = (int)command.Parameters["TotalCount"].Value;
            }
        }
    }
}
