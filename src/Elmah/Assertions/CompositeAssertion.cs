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

[assembly: Elmah.Scc("$Id: CompositeAssertion.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah.Assertions
{
    #region Imports

    using System;
    using System.Collections;

    #endregion

    /// <summary>
    /// Read-only collection of <see cref="Assertions.IAssertion"/> instances.
    /// </summary>

    [ Serializable ]
    public abstract class CompositeAssertion : ReadOnlyCollectionBase, IAssertion
    {
        protected CompositeAssertion() {}

        protected CompositeAssertion(IAssertion[] assertions)
        {
            if (assertions == null) 
                throw new ArgumentNullException("assertions");

            foreach (IAssertion assertion in assertions)
            {
                if (assertion == null)
                    throw new ArgumentException(null, "assertions");
            }

            InnerList.AddRange(assertions);
        }

        protected CompositeAssertion(ICollection assertions)
        {
            if (assertions != null)
                InnerList.AddRange(assertions);
        }

        public virtual IAssertion this[int index]
        {
            get { return (IAssertion) InnerList[index]; }
        }

        public virtual bool Contains(IAssertion assertion)
        {
            return InnerList.Contains(assertion);
        }

        public virtual int IndexOf(IAssertion assertion)
        {
            return InnerList.IndexOf(assertion);
        }
        
        public abstract bool Test(object context);
    }
}