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

[assembly: Elmah.Scc("$Id: ApplicationException.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports
    
    using System;

    using SerializationInfo = System.Runtime.Serialization.SerializationInfo;
    using StreamingContext = System.Runtime.Serialization.StreamingContext;

    #endregion

    /// <summary>
    /// The exception that is thrown when a non-fatal error occurs. 
    /// This exception also serves as the base for all exceptions thrown by
    /// this library.
    /// </summary>

    [ Serializable ]
    public class ApplicationException : Exception
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationException"/> class.
        /// </summary>
        
        public ApplicationException() {}
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationException"/> class 
        /// with a specified error message.
        /// </summary>

        public ApplicationException(string message) : 
            base(message) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationException"/> 
        /// class with a specified error message and a reference to the 
        /// inner exception that is the cause of this exception.
        /// </summary>

        public ApplicationException(string message, Exception innerException) : 
            base(message, innerException) {}
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationException"/> class 
        /// with serialized data.
        /// </summary>

        protected ApplicationException(SerializationInfo info, StreamingContext context) : 
            base(info, context) {}
    }
}

