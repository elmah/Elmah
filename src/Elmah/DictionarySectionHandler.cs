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
// Software Foundation; either version 2.1 of the License, or (at your option)
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

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections;
    using System.Configuration;
    using System.Xml;

    #endregion

    // Adapted from DictionarySectionHandler from Jayrock:
    // http://code.google.com/p/jayrock/source/browse/tags/REL-12915/src/Jayrock.Json/Configuration/DictionarySectionHandler.cs

    internal class DictionarySectionHandler : IConfigurationSectionHandler
    {
        public virtual object Create(object parent, object configContext, XmlNode section)
        {
            if (section == null)
                throw new ArgumentNullException("section");

            IDictionary dictionary = CreateDictionary(parent);

            foreach (XmlNode childNode in section.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Comment ||
                    childNode.NodeType == XmlNodeType.Whitespace)
                {
                    continue;
                }

                if (childNode.NodeType != XmlNodeType.Element)
                {
                    throw new ConfigurationException(string.Format("Unexpected type of node ({0}) in configuration.", 
                        childNode.NodeType.ToString()), childNode);
                }

                string nodeName = childNode.Name;

                if (nodeName == "clear")
                {
                    OnClear(dictionary);
                }
                else
                {
                    object key = GetKey(childNode);
                    if (nodeName == "add")
                    {
                        OnAdd(dictionary, key, childNode);
                    }
                    else if (nodeName == "remove")
                    {
                        OnRemove(dictionary, key);
                    }
                    else
                    {
                        throw new ConfigurationException(string.Format("'{0}' is not a valid dictionary node. Use add, remove or clear.", nodeName), childNode);
                    }
                }
            }
            
            return dictionary;
        }

        protected virtual IDictionary CreateDictionary(object parent)
        {
#if NET_1_0
            CaseInsensitiveHashCodeProvider hcp = new CaseInsensitiveHashCodeProvider(CultureInfo.InvariantCulture);
            CaseInsensitiveComparer comparer = new CaseInsensitiveComparer(CultureInfo.InvariantCulture);
#else
            CaseInsensitiveHashCodeProvider hcp = CaseInsensitiveHashCodeProvider.DefaultInvariant;
            CaseInsensitiveComparer comparer = CaseInsensitiveComparer.DefaultInvariant;
#endif
            
            return parent != null ?
                new Hashtable((IDictionary) parent, hcp, comparer) :
                new Hashtable(hcp, comparer);
        }

        protected virtual object GetKey(XmlNode node)
        {
            return GetKey(node, "key");
        }

        protected static string GetKey(XmlNode node, string name)
        {
            XmlAttribute keyAttribute = node.Attributes[name];
            string value = keyAttribute == null ? null : keyAttribute.Value;
            if (value == null || value.Length == 0)
                throw new ConfigurationException("Missing entry key.", node);
            return value;
        }

        protected virtual object GetValue(XmlNode node)
        {
            return GetValue(node, "value");
        }

        protected static string GetValue(XmlNode node, string name)
        {
            XmlAttribute valueAttribute = node.Attributes[name];
            return valueAttribute != null ? valueAttribute.Value : null;
        }

        protected virtual void OnAdd(IDictionary dictionary, object key, XmlNode node)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            if (node == null)
                throw new ArgumentNullException("node");

            dictionary.Add(key, GetValue(node));
        }
 
        protected virtual void OnRemove(IDictionary dictionary, object key)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            dictionary.Remove(key);
        }

        protected virtual void OnClear(IDictionary dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            dictionary.Clear();
        }
    }
}