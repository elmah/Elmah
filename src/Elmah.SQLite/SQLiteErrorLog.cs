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

[assembly: Elmah.Scc("$Id: SQLiteErrorLog.cs 924 2011-12-23 22:41:47Z azizatif $")]

namespace Elmah
{

    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using System.Globalization;
    using System.IO;

    using IDictionary = System.Collections.IDictionary;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses SQLite as its backing store.
    /// </summary>

    public class SQLiteErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public SQLiteErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            var connectionString = ConnectionStringHelper.GetConnectionString(config, true);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the SQLite error log.");

            _connectionString = connectionString;

            InitializeDatabase();

            ApplicationName = config.Find("applicationName", string.Empty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public SQLiteErrorLog(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentException(null, "connectionString");

            _connectionString = ConnectionStringHelper.GetResolvedConnectionString(connectionString);

            InitializeDatabase();
        }

        private static readonly object _lock = new object();
        private void InitializeDatabase()
        {
            var connectionString = ConnectionString;
            Debug.AssertStringNotEmpty(connectionString);

            var dbFilePath = ConnectionStringHelper.GetDataSourceFilePath(connectionString);

            if (File.Exists(dbFilePath))
                return;

            //
            // Make sure that we don't have multiple threads all trying to create the database
            //

            lock (_lock)
            {
                //
                // Just double check that no other thread has created the database while
                // we were waiting for the lock
                //

                if (File.Exists(dbFilePath))
                    return;

                SQLiteConnection.CreateFile(dbFilePath);

                const string sql = @"
                CREATE TABLE Error (
                    ErrorId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    Application TEXT NOT NULL,
                    Host TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Source TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    User TEXT NOT NULL,
                    StatusCode INTEGER NOT NULL,
                    TimeUtc TEXT NOT NULL,
                    AllXml TEXT NOT NULL
                )";

                using (var connection = new SQLiteConnection(connectionString))
                using (var command = new SQLiteCommand(sql, connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>

        public override string Name
        {
            get { return "SQLite Error Log"; }
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

            var errorXml = ErrorXml.EncodeString(error);

            const string query = @"
                INSERT INTO Error (
                    Application, Host, 
                    Type, Source, Message, User, StatusCode, 
                    TimeUtc, AllXml)
                VALUES (
                    @Application, @Host, 
                    @Type, @Source, @Message, @User, @StatusCode, 
                    @TimeUtc, @AllXml);

                SELECT last_insert_rowid();";

            using (var connection = new SQLiteConnection(ConnectionString))
            using (var command = new SQLiteCommand(query, connection))
            {
                var parameters = command.Parameters;

                parameters.Add("@Application", DbType.String, 60).Value = ApplicationName;
                parameters.Add("@Host", DbType.String, 30).Value = error.HostName;
                parameters.Add("@Type", DbType.String, 100).Value = error.Type;
                parameters.Add("@Source", DbType.String, 60).Value = error.Source;
                parameters.Add("@Message", DbType.String, 500).Value = error.Message;
                parameters.Add("@User", DbType.String, 50).Value = error.User;
                parameters.Add("@StatusCode", DbType.Int64).Value = error.StatusCode;
                parameters.Add("@TimeUtc", DbType.DateTime).Value = error.Time.ToUniversalTime();
                parameters.Add("@AllXml", DbType.String).Value = errorXml;

                connection.Open();

                return Convert.ToInt64(command.ExecuteScalar()).ToString(CultureInfo.InvariantCulture);
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

            const string sql = @"
                SELECT
                    ErrorId,
                    Application,
                    Host,
                    Type,
                    Source,
                    Message,
                    User,
                    StatusCode,
                    TimeUtc
                FROM
                    Error
                ORDER BY
                    ErrorId DESC
                LIMIT 
                    @PageIndex * @PageSize,
                    @PageSize;

                SELECT COUNT(*) FROM Error";

            using (var connection = new SQLiteConnection(ConnectionString))
            using (var command = new SQLiteCommand(sql, connection))
            {
                var parameters = command.Parameters;

                parameters.Add("@PageIndex", DbType.Int16).Value = pageIndex;
                parameters.Add("@PageSize", DbType.Int16).Value = pageSize;

                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (errorEntryList != null)
                    {
                        while (reader.Read())
                        {
                            var id = Convert.ToString(reader["ErrorId"], CultureInfo.InvariantCulture);

                            var error = new Error
                            {
                                ApplicationName = reader["Application"].ToString(),
                                HostName = reader["Host"].ToString(),
                                Type = reader["Type"].ToString(),
                                Source = reader["Source"].ToString(),
                                Message = reader["Message"].ToString(),
                                User = reader["User"].ToString(),
                                StatusCode = Convert.ToInt32(reader["StatusCode"]),
                                Time = Convert.ToDateTime(reader["TimeUtc"]).ToLocalTime()
                            };

                            errorEntryList.Add(new ErrorLogEntry(this, id, error));
                        }
                    }

                    //
                    // Get the result of SELECT COUNT(*) FROM Page
                    //

                    reader.NextResult();
                    reader.Read();
                    return reader.GetInt32(0);
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

            long key;
            
            try
            {
                key = long.Parse(id, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, "id", e);
            }

            const string sql = @"
                SELECT 
                    AllXml
                FROM 
                    Error
                WHERE
                    ErrorId = @ErrorId";

            using (var connection = new SQLiteConnection(ConnectionString))
            using (var command = new SQLiteCommand(sql, connection))
            {
                var parameters = command.Parameters;
                parameters.Add("@ErrorId", DbType.Int64).Value = key;

                connection.Open();

                var errorXml = (string) command.ExecuteScalar();

                if (errorXml == null)
                    return null;

                var error = ErrorXml.DecodeString(errorXml);
                return new ErrorLogEntry(this, id, error);
            }
        }
    }
}
