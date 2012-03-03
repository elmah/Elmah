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

[assembly: Elmah.Scc("$Id: Build.cs 915 2011-12-22 23:01:32Z azizatif $")]

namespace Elmah
{
    internal sealed class Build
    {
        private Build() { }

#if DEBUG
        public const bool IsDebug = true;
        public const bool IsRelease = !IsDebug;
        public const string Type = "Debug";
        public const string TypeUppercase = "DEBUG";
        public const string TypeLowercase = "debug";
#else
        public const bool IsDebug = false;
        public const bool IsRelease = !IsDebug;
        public const string Type = "Release";
        public const string TypeUppercase = "RELEASE";
        public const string TypeLowercase = "release";
#endif

#if NET_1_0
        public const string Framework = "net-1.0";
#elif NET_1_1
        public const string Framework = "net-1.1";
#elif NET_2_0
        public const string Framework = "net-2.0";
#elif NET_3_5
        public const string Framework = "net-3.5";
#elif NET_4_0
        public const string Framework = "net-4.0";
#else
        public const string Framework = "unknown";
#endif

        public const string Configuration = TypeLowercase + "; " + Status + "; " + Framework;

        /// <summary>
        /// Gets a string representing the version of the CLR saved in 
        /// the file containing the manifest. Under 1.0, this returns
        /// the hard-wired string "v1.0.3705".
        /// </summary>

        public static string ImageRuntimeVersion
        {
            get
            {
#if NET_1_0
                //
                // As Assembly.ImageRuntimeVersion property was not available
                // under .NET Framework 1.0, we just return the version 
                // hard-coded based on conditional compilation symbol.
                //

                return "v1.0.3705";
#else
                return typeof(ErrorLog).Assembly.ImageRuntimeVersion;
#endif
            }
        }

        /// <summary>
        /// This is the status or milestone of the build. Examples are
        /// M1, M2, ..., Mn, BETA1, BETA2, RC1, RC2, RTM.
        /// </summary>

        public const string Status = "RTM";
    }
}
