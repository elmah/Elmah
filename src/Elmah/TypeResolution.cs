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

#region License, Terms and Conditions
//
// Jayrock - JSON and JSON-RPC for Microsoft .NET Framework and Mono
// Written by Atif Aziz (www.raboof.com)
// Copyright (c) 2005 Atif Aziz. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it under
// the terms of the GNU Lesser General Public License as published by the Free
// Software Foundation; either version 3 of the License, or (at your option)
// any later version.
//
// This library is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more
// details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this library; if not, write to the Free Software Foundation, Inc.,
// 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
//
#endregion

namespace Elmah
{
    #region Imports

    using System;

    #endregion

    public delegate Type TypeResolutionHandler(string typeName, bool throwOnError, bool ignoreCase);

    public static class TypeResolution
    {
        private static TypeResolutionHandler _current;

        static TypeResolution()
        {
            _current = Default = Type.GetType;
        }

        public static TypeResolutionHandler Default { get; private set; }

        public static TypeResolutionHandler Current
        {
            get { return _current; }

            set
            {
                if (value == null) throw new ArgumentNullException("value");
                _current = value;
            }
        }

        public static Type FindType(string typeName)
        {
            return Current(typeName, /* throwOnError = */ false, /* ignoreCase = */ false);
        }

        public static Type GetType(string typeName)
        {
            return Current(typeName, /* throwOnError = */ true, /* ignoreCase = */ false);
        }
    }
}
