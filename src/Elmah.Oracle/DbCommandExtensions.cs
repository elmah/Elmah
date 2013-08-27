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

namespace Elmah
{
    #region Imports

    using System;
    using System.Data;
    using System.Reflection;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="IDbCommand"/> objects.
    /// </summary>
    
    static class DbCommandExtensions
    {
        /// <remarks>
        /// Use <see cref="Missing.Value"/> for parameter value to avoid 
        /// having it set by the returned function.
        /// </remarks>

        public static Func<string, DbType?, object, IDbDataParameter> ParameterAdder(this IDbCommand command)
        {
            return ParameterAdder(command, cmd => cmd.CreateParameter());
        }

        /// <remarks>
        /// Use <see cref="Missing.Value"/> for parameter value to avoid 
        /// having it set by the returned function.
        /// </remarks>

        public static Func<string, DbType?, object, TParameter> ParameterAdder<TCommand, TParameter>(this TCommand command, Func<TCommand, TParameter> parameterCreator)
            where TCommand : IDbCommand
            where TParameter : IDataParameter
        {
            // ReSharper disable CompareNonConstrainedGenericWithNull
            if (command == null) throw new ArgumentNullException("command"); // ReSharper restore CompareNonConstrainedGenericWithNull
            if (parameterCreator == null) throw new ArgumentNullException("parameterCreator");

            return (name, dbType, value) =>
            {
                var parameter = parameterCreator(command);
                parameter.ParameterName = name;
                if (dbType != null)
                    parameter.DbType = dbType.Value;
                if (Missing.Value != value)
                    parameter.Value = value;
                command.Parameters.Add(parameter);
                return parameter;
            };
        }
    }
}