#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      James Driscoll, mailto:jamesdriscoll@btinternet.com
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

namespace Elmah.Versioning
{
    #region Imports
    using System;
    using System.Collections.Generic;
    #endregion

    internal abstract class BaseHgSccStampList
    {
        public List<HgSccStamp> SccStamps { get; private set; }

        protected BaseHgSccStampList()
        {
            SccStamps = new List<HgSccStamp>();
        }

        private string _localChangeSet;
        public string LocalChangeset
        { 
            get { return _localChangeSet; }
            protected set { _localChangeSet = value.Trim(); } 
        }
        private string _remoteChangeSet;
        public string RemoteChangeset
        { 
            get { return _remoteChangeSet; }
            protected set { _remoteChangeSet = value.Trim(); } 
        }

        public bool HasUncommittedLocalChanges
        {
            get { return LocalChangeset.EndsWith("+"); }
        }

        public bool IsLocalBuild
        {
            get { return LocalChangeset.Equals(RemoteChangeset, StringComparison.InvariantCultureIgnoreCase); }
        }

        public virtual void PopulateList()
        {
            LocalChangeset = "No Hg";
        }
    }
}
