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

        /*
        [Serializable]
        struct PrecisionScale : IEquatable<PrecisionScale>
        {
            // ReSharper disable RedundantDefaultFieldInitializer
            public static readonly PrecisionScale None = new PrecisionScale(); // ReSharper restore RedundantDefaultFieldInitializer

            public byte? Precision { get; private set; }
            public byte? Scale { get; private set; }

            public PrecisionScale(byte? precision, byte? scale) : this()
            {
                Precision = precision;
                Scale = scale;
            }

            public bool Equals(PrecisionScale other) { return other.Precision.Equals(Precision) && other.Scale.Equals(Scale); }
            public override bool Equals(object obj)  { return obj is PrecisionScale && Equals((PrecisionScale) obj); }
            public override int GetHashCode() { unchecked { return (Precision.GetHashCode() * 397) ^ Scale.GetHashCode(); } }
            public override string ToString() { return string.Format("Precision: {0}, Scale: {1}", Precision, Scale); }
        }

        delegate TParameter ParameterAdder<out TParameter>(string name, DbType? dbType, int? size, PrecisionScale precisionScale, object value);

        static class DataExtensions
        {
            public static ParameterAdder<IDbDataParameter> ParameterAdder(this IDbCommand command)
            {
                return ParameterAdder(command, cmd => cmd.CreateParameter());
            }

            public static ParameterAdder<TParameter> ParameterAdder<TCommand, TParameter>(this TCommand command, Func<TCommand, TParameter> parameterCreator)
                where TCommand : IDbCommand
                where TParameter : IDbDataParameter
            {
                // ReSharper disable CompareNonConstrainedGenericWithNull
                if (command == null) throw new ArgumentNullException("command"); // ReSharper restore CompareNonConstrainedGenericWithNull
                if (parameterCreator == null) throw new ArgumentNullException("parameterCreator");
            
                return (name, dbType, size, ps, value) =>
                {
                    var parameter = parameterCreator(command);
                    parameter.ParameterName = name;
                    if (dbType != null) parameter.DbType = dbType.Value;
                    if (Missing.Value != value) parameter.Value = value;
                    if (ps.Precision != null) parameter.Precision = ps.Precision.Value;
                    if (ps.Scale != null) parameter.Scale = ps.Scale.Value;
                    command.Parameters.Add(parameter);
                    return parameter;
                };
            }
        }
        */
    }
}