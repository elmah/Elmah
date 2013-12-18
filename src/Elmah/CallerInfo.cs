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
    using System.Runtime.CompilerServices; // caller info attributes
    using Mannex;

    #endregion

    #if NET_3_5 || NET_4_0

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)] /* FIXFIX */public sealed class CallerMemberNameAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)] /* FIXFIX */public sealed class CallerFilePathAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)] /* FIXFIX */public sealed class CallerLineNumberAttribute : Attribute { }
    
    #endif

    [Serializable]
    public sealed class CallerInfo
    {
        // ReSharper disable RedundantArgumentDefaultValue
        public static readonly CallerInfo Empty = new CallerInfo(null, null, 0); // ReSharper restore RedundantArgumentDefaultValue

        private readonly string _memberName;
        private readonly string _filePath;

        public string MemberName { get { return _memberName ?? string.Empty; } }
        public string FilePath { get { return _filePath ?? string.Empty; } }
        public int LineNumber { get; private set; }

        public CallerInfo([CallerMemberName] string memberName = null,
                          [CallerFilePath] string filePath = null,
                          [CallerLineNumber] int lineNumber = 0)
        {
            _memberName = memberName;
            _filePath = filePath;
            LineNumber = lineNumber;
        }

        public bool IsEmpty { get { return 0 == MemberName.Length
                                        && 0 == FilePath.Length
                                        && 0 == LineNumber; } }

        public override string ToString()
        {
            return Mask.EmptyString(MemberName, "<?member>") 
                 + "@" + Mask.EmptyString(FilePath, "<?filename>")
                 + ":" + LineNumber.ToInvariantString();
        }
    }
}