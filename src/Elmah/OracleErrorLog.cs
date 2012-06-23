#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      James Driscoll, mailto:jamesdriscoll@btinternet.com
//      with contributions from Hath1
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

[assembly: Elmah.Scc("$Id: OracleErrorLog.cs 926 2011-12-23 22:50:57Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using IDictionary = System.Collections.IDictionary;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses Oracle as its backing store.
    /// </summary>

    public class OracleErrorLog : ErrorLog
    {
        private static readonly Func<DbProviderFactory, ProviderInfo> _providerInfo = Memoization.MemoizeLast<DbProviderFactory, ProviderInfo>(GetProviderInfo);

        private readonly string _connectionString;
        private readonly DbProviderFactory _dbProviderFactory;
        private string _name;
        private string _schemaOwner;
        private bool _schemaOwnerInitialized;

        private const int _maxAppNameLength = 60;
        private const int _maxSchemaNameLength = 30;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public OracleErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            var connectionString = ConnectionStringHelper.GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the Oracle error log.");

            _connectionString = connectionString;

            //
            // Initialize the provider factory if it hasn't already been done.
            //

            var providerName = ConnectionStringHelper.GetConnectionStringProviderName(config);
            _dbProviderFactory = GetDbProviderFactory(providerName);

            //
            // Set the application name as this implementation provides
            // per-application isolation over a single store.
            //

            var appName = (string) config["applicationName"] ?? string.Empty;

            if (appName.Length > _maxAppNameLength)
            {
                throw new ApplicationException(string.Format(
                    "Application name is too long. Maximum length allowed is {0} characters.",
                    _maxAppNameLength.ToString("N0")));
            }

            ApplicationName = appName;

            SchemaOwner = (string)config["schemaOwner"];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public OracleErrorLog(string connectionString) :
            this(connectionString, (DbProviderFactory) null) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// to use a specific connection string and provider for connecting 
        /// to the database.
        /// </summary>
        /// <remarks>
        /// The only supported <see cref="DbProviderFactory"/> instances are
        /// those of <c>Oracle.DataAccess.Client</c> (ODP.NET) and
        /// <c>System.Data.OracleClient</c>. The supplied instance is not
        /// validated so any other provider will yield undefined behavior.
        /// </remarks>

        public OracleErrorLog(string connectionString, DbProviderFactory dbProviderFactory) : 
            this(connectionString, null, dbProviderFactory) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// to use a specific connection string for connecting  to the database. 
        /// An additional parameter specifies the schema owner.
        /// </summary>

        public OracleErrorLog(string connectionString, string schemaOwner) : 
            this(connectionString, schemaOwner, null) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// to use a specific connection string and provider  for connecting 
        /// to the database. An additional parameter specifies the schema owner.
        /// </summary>
        /// <remarks>
        /// The only supported <see cref="DbProviderFactory"/> instances are
        /// those of <c>Oracle.DataAccess.Client</c> (ODP.NET) and
        /// <c>System.Data.OracleClient</c>. The supplied instance is not
        /// validated so any other provider will yield undefined behavior.
        /// </remarks>

        public OracleErrorLog(string connectionString, string schemaOwner, DbProviderFactory dbProviderFactory)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentException(null, "connectionString");

            _connectionString = connectionString;
            _dbProviderFactory = dbProviderFactory ?? GetDbProviderFactory(null);
            SchemaOwner = schemaOwner;
        }

        private ProviderInfo ThisProviderInfo { get { return _providerInfo(_dbProviderFactory); } }

        /// <summary>
        /// Gets the name of the schema owner where the errors are being stored.
        /// </summary>

        public string SchemaOwner
        {
            get { return _schemaOwner ?? string.Empty; }

            set
            {
                if (_schemaOwnerInitialized)
                    throw new InvalidOperationException("The schema owner cannot be reset once initialized.");

                if (string.IsNullOrEmpty(value))
                    return;

                if (value.Length > 0)
                {
                    if (value.Length > _maxSchemaNameLength)
                        throw new ApplicationException(string.Format(
                            "Oracle schema owner is too long. Maximum length allowed is {0} characters.",
                            _maxSchemaNameLength.ToString("N0")));
                }

                _schemaOwner = value + ".";
                _schemaOwnerInitialized = true;
            }
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>

        public override string Name
        {
            get { return _name ?? (_name = string.Format("Oracle Error Log ({0})", _dbProviderFactory.GetType().Namespace)); }
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
            var id = Guid.NewGuid();

            using (var connection = CreateOpenConnection())
            using (var command = connection.CreateCommand())
            using (var transaction = connection.BeginTransaction())
            {
                // because we are storing the XML data in a NClob, we need to jump through a few hoops!!
                // so first we've got to operate within a transaction
                command.Transaction = transaction;

                // then we need to create a temporary lob on the database server
                command.CommandText = "declare xx nclob; begin dbms_lob.createtemporary(xx, false, 0); :tempblob := xx; end;";
                command.CommandType = CommandType.Text;

                var parameters = command.Parameters;
                AddProviderSpecificTypeParameter(command, "tempblob", ThisProviderInfo.ClobDbType).Direction = ParameterDirection.Output;
                command.ExecuteNonQuery();

                object xmlValue;

                if (parameters[0].Value is string)
                {
                    xmlValue = errorXml;
                }
                else
                {
                    // now we can get a handle to the NClob
                    // TODO Review where Stream needs disposing
                    var stream = (Stream)parameters[0].Value;
                    // create a temporary buffer in which to store the XML
                    var bytes = Encoding.Unicode.GetBytes(errorXml);
                    // and finally we can write to it!
                    stream.Write(bytes, 0, bytes.Length);
                    xmlValue = stream;
                }

                command.CommandText = SchemaOwner + "pkg_elmah$log_error.LogError";
                command.CommandType = CommandType.StoredProcedure;

                parameters.Clear();
                var addParameter = command.ParameterAdder();
                addParameter("v_ErrorId", DbType.String, id.ToString("N"));
                addParameter("v_Application", DbType.String, ApplicationName);
                addParameter("v_Host", DbType.String, error.HostName);
                addParameter("v_Type", DbType.String, error.Type);
                addParameter("v_Source", DbType.String, error.Source);
                addParameter("v_Message", DbType.String, error.Message);
                addParameter("v_User", DbType.String, error.User);
                AddProviderSpecificTypeParameter(command, "v_AllXml", ThisProviderInfo.ClobDbType).Value = xmlValue;
                addParameter("v_StatusCode", DbType.Int32, error.StatusCode);
                addParameter("v_TimeUtc", DbType.DateTime, error.Time.ToUniversalTime());

                command.ExecuteNonQuery();
                transaction.Commit();
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

            using (var connection = CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SchemaOwner + "pkg_elmah$get_error.GetErrorsXml";
                command.CommandType = CommandType.StoredProcedure;

                var addParameter = command.ParameterAdder();
                addParameter("v_Application", DbType.String, ApplicationName);
                addParameter("v_PageIndex", DbType.Int32, pageIndex);
                addParameter("v_PageSize", DbType.Int32, pageSize);
                var totalCount = addParameter("v_TotalCount", DbType.Int32, Missing.Value);
                totalCount.Direction = ParameterDirection.Output;
                AddProviderSpecificTypeParameter(command, "v_Results", ThisProviderInfo.RefCursorDbType).Direction = ParameterDirection.Output;

                using (var reader = command.ExecuteReader())
                {
                    Debug.Assert(reader != null);

                    if (errorEntryList != null)
                    {
                        while (reader.Read())
                        {
                            var id = reader["ErrorId"].ToString();
                            var guid = new Guid(id);

                            var error = new Error
                            {
                                ApplicationName = reader["Application"].ToString(),
                                HostName = reader["Host"].ToString(),
                                Type = reader["Type"].ToString(),
                                Source = reader["Source"].ToString(),
                                Message = reader["Message"].ToString(),
                                User = reader["UserName"].ToString(),
                                StatusCode = Convert.ToInt32(reader["StatusCode"]),
                                Time = Convert.ToDateTime(reader["TimeUtc"]).ToLocalTime()
                            };

                            errorEntryList.Add(new ErrorLogEntry(this, guid.ToString(), error));
                        }
                    }
                    reader.Close();
                }

                return (int) totalCount.Value;
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

            using (var connection = CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SchemaOwner + "pkg_elmah$get_error.GetErrorXml";
                command.CommandType = CommandType.StoredProcedure;

                var addParameter = command.ParameterAdder();
                addParameter("v_Application", DbType.String, ApplicationName);
                addParameter("v_ErrorId", DbType.String, errorGuid.ToString("N"));
                var allXml = AddProviderSpecificTypeParameter(command, "v_AllXml", ThisProviderInfo.ClobDbType);
                allXml.Direction = ParameterDirection.Output;

                command.ExecuteNonQuery();
                errorXml = allXml.Value as string;
                if (errorXml == null)
                {
                    // TODO Review whether Stream needs disposing
                    var stream = (Stream) allXml.Value;
                    var reader = new StreamReader(stream, Encoding.Unicode);
                    errorXml = reader.ReadToEnd();
                }
            }

            var error = ErrorXml.DecodeString(errorXml);
            return new ErrorLogEntry(this, id, error);
        }

        private DbConnection CreateOpenConnection()
        {
            var connection = _dbProviderFactory.CreateConnection();
            Debug.Assert(connection != null); // TODO convert to run-time exception
            connection.ConnectionString = ConnectionString;
            connection.Open();
            return connection;
        }

        private static DbProviderFactory GetDbProviderFactory(string providerName)
        {
            DbProviderFactory dbProviderFactory;

            if (!string.IsNullOrEmpty(providerName))
            {
                //
                // If the user has supplied a provider name, that's the one
                // we must use.
                //

                dbProviderFactory = DbProviderFactoryQuery.GetFactory(providerName);
            }
            else
            {
                //
                // Otherwise, we try to use ODP.Net in the first instance
                // and then fallback to the Microsoft client.
                //

                dbProviderFactory = DbProviderFactoryQuery.FindFactory("Oracle.DataAccess.Client")
                                    ?? DbProviderFactoryQuery.GetFactory("System.Data.OracleClient");
            }

            return dbProviderFactory;
        }

        sealed class ProviderInfo
        {
            public PropertyInfo ProviderSpecificTypeProperty { get; private set; }
            public object ClobDbType { get; private set; }
            public object RefCursorDbType { get; private set; }

            public ProviderInfo(PropertyInfo providerSpecificTypeProperty,
                                object clobDbType, object refCursorDbType)
            {
                Debug.Assert(providerSpecificTypeProperty != null);
                Debug.Assert(clobDbType != null);
                Debug.Assert(refCursorDbType != null);

                ProviderSpecificTypeProperty = providerSpecificTypeProperty;
                ClobDbType = clobDbType;
                RefCursorDbType = refCursorDbType;
            }
        }

        private static ProviderInfo GetProviderInfo(DbProviderFactory dbProviderFactory)
        {
            Debug.Assert(dbProviderFactory != null);

            var parameter = dbProviderFactory.CreateParameter();
            if (parameter == null)
                throw new NotSupportedException();

            var specificTypeProperties =
                from property in parameter.GetType().GetProperties()
                let attribute = (DbProviderSpecificTypePropertyAttribute)Attribute.GetCustomAttribute(property, typeof(DbProviderSpecificTypePropertyAttribute), false)
                where attribute != null
                   && attribute.IsProviderSpecificTypeProperty
                select property;

            var specificTypeProperty = specificTypeProperties.Single();
            var specificType = specificTypeProperty.PropertyType;
            var clobDbType = GetEnumValueOfAny(specificType, "NClob");
            var refCursorDbType = GetEnumValueOfAny(specificType, "Cursor", "RefCursor");

            return new ProviderInfo(specificTypeProperty, clobDbType, refCursorDbType);
        }

        static object GetEnumValueOfAny(Type enumType, params string[] candidates)
        {
            Debug.Assert(enumType != null);
            Debug.Assert(enumType.IsEnum);
            Debug.Assert(candidates != null);

            var enumMembers = enumType.GetEnumMembers().ToArray();
            var matches = from dbType in candidates
                          select enumMembers.FirstOrDefault(p => p.Key.Equals(dbType, StringComparison.OrdinalIgnoreCase));

            return matches.First(m => m.Key != null).Value;
        }

        private IDbDataParameter AddProviderSpecificTypeParameter(IDbCommand command, string parameterName, object dbType)
        {
            var parameter = command.ParameterAdder()(parameterName, null, null);
            ThisProviderInfo.ProviderSpecificTypeProperty.SetValue(parameter, dbType, null);
            return parameter;
        }
    }
}
