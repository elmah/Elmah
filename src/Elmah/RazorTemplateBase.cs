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
    using System.Text;

    class RazorTemplateBase
    {
        string _content;
        private readonly StringBuilder _generatingEnvironment = new StringBuilder();

        public RazorTemplateBase Layout { get; set; }

        public virtual void Execute() {}

        public void WriteLiteral(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
                return;
            _generatingEnvironment.Append(textToAppend); ;
        }

        public virtual void Write(object value)
        {
            if (value == null)
                return;
            WriteLiteral(value.ToString());
        }

        public string RenderBody()
        {
            return _content;
        }

        public string TransformText()
        {
            Execute();
            
            if (Layout != null)
            {
                Layout._content = _generatingEnvironment.ToString();
                return Layout.TransformText();
            }

            return _generatingEnvironment.ToString();
        }
    }
}
