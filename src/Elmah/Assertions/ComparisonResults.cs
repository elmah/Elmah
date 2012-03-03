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

[assembly: Elmah.Scc("$Id: ComparisonResults.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah.Assertions
{
    #region Import

    using System;

    #endregion

    public delegate bool ComparisonResultPredicate(int result);
    
    public sealed class ComparisonResults
    {
        public readonly static ComparisonResultPredicate Equal = new ComparisonResultPredicate(MeansEqual);
        public readonly static ComparisonResultPredicate Lesser = new ComparisonResultPredicate(MeansLesser);
        public readonly static ComparisonResultPredicate LesserOrEqual = new ComparisonResultPredicate(MeansLessOrEqual);
        public readonly static ComparisonResultPredicate Greater = new ComparisonResultPredicate(MeansGreater);
        public readonly static ComparisonResultPredicate GreaterOrEqual = new ComparisonResultPredicate(MeansGreaterOrEqual);
        
        private static bool MeansEqual(int result) { return result == 0; }
        private static bool MeansLesser(int result) { return result < 0; }
        private static bool MeansLessOrEqual(int result) { return result <= 0; }
        private static bool MeansGreater(int result) { return result > 0; }
        private static bool MeansGreaterOrEqual(int result) { return result >= 0; }

        private ComparisonResults()
        {
            throw new NotSupportedException();
        }
    }
}
