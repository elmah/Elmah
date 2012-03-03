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

[assembly: Elmah.Scc("$Id: InvariantStringArray.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections;
    using System.Globalization;

    #endregion

    /// <summary> 
    /// Helper methods for array containing culturally-invariant strings.
    /// The main reason for this helper is to help with po
    /// </summary>
 
    internal sealed class InvariantStringArray
    {
        public static void Sort(string[] keys)
        {
            Sort(keys, 0, keys.Length);
        }

        public static void Sort(string[] keys, int index, int length)
        {
            Sort(keys, null, index, length);
        }
        
        public static void Sort(string[] keys, Array items, int index, int length)
        {
            Debug.Assert(keys != null);

            Array.Sort(keys, items, index, length, InvariantComparer);
        }

        private static IComparer InvariantComparer
        {
            get
            {
#if NET_1_0
                return StringComparer.DefaultInvariant;
#else
                return Comparer.DefaultInvariant;
#endif
            }
        }

#if NET_1_0
        
        [ Serializable ]
        private sealed class StringComparer : IComparer
        {
            private CompareInfo _compareInfo;
            
            public static readonly StringComparer DefaultInvariant = new StringComparer(CultureInfo.InvariantCulture);

            private StringComparer(CultureInfo culture)
            {
                Debug.Assert(culture != null);
                
                _compareInfo = culture.CompareInfo;
            }

            public int Compare(object x, object y)
            {
                if (x == y) 
                    return 0;
                else if (x == null) 
                    return -1;
                else if (y == null) 
                    return 1;
                else
                    return _compareInfo.Compare((string) x, (string) y);
            }
        }

#endif
        
        private InvariantStringArray() {}
    }
}