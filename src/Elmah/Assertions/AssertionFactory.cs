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

[assembly: Elmah.Scc("$Id: AssertionFactory.cs 640 2009-06-01 17:22:02Z azizatif $")]

namespace Elmah.Assertions
{
    #region Imports

    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Xml;
    using System.Collections.Generic;
    
    #endregion

    /// <summary>
    /// Represents the method that will be responsible for creating an 
    /// assertion object and initializing it from an XML configuration
    /// element.
    /// </summary>

    public delegate IAssertion AssertionFactoryHandler(XmlElement config);

    /// <summary>
    /// Holds factory methods for creating configured assertion objects.
    /// </summary>

    public static class AssertionFactory
    {
        public static IAssertion assert_is_null(IContextExpression binding)
        {
            return new IsNullAssertion(binding);
        }

        public static IAssertion assert_is_not_null(IContextExpression binding)
        {
            return new UnaryNotAssertion(assert_is_null(binding));
        }

        public static IAssertion assert_equal(IContextExpression binding, TypeCode type, string value)
        {
            return new ComparisonAssertion(ComparisonResults.Equal, binding, type, value);
        }

        public static IAssertion assert_not_equal(IContextExpression binding, TypeCode type, string value)
        {
            return new UnaryNotAssertion(assert_equal(binding, type, value));
        }

        public static IAssertion assert_lesser(IContextExpression binding, TypeCode type, string value)
        {
            return new ComparisonAssertion(ComparisonResults.Lesser, binding, type, value);
        }

        public static IAssertion assert_lesser_or_equal(IContextExpression binding, TypeCode type, string value)
        {
            return new ComparisonAssertion(ComparisonResults.LesserOrEqual, binding, type, value);
        }

        public static IAssertion assert_greater(IContextExpression binding, TypeCode type, string value)
        {
            return new ComparisonAssertion(ComparisonResults.Greater, binding, type, value);
        }

        public static IAssertion assert_greater_or_equal(IContextExpression binding, TypeCode type, string value)
        {
            return new ComparisonAssertion(ComparisonResults.GreaterOrEqual, binding, type, value);
        }

        public static IAssertion assert_and(XmlElement config)
        {
            return LogicalAssertion.LogicalAnd(Create(config.ChildNodes));
        }

        public static IAssertion assert_or(XmlElement config)
        {
            return LogicalAssertion.LogicalOr(Create(config.ChildNodes));
        }

        public static IAssertion assert_not(XmlElement config)
        {
            return LogicalAssertion.LogicalNot(Create(config.ChildNodes));
        }

        public static IAssertion assert_is_type(IContextExpression binding, Type type)
        {
            return new TypeAssertion(binding, type, /* byCompatibility */ false);
        }

        public static IAssertion assert_is_type_compatible(IContextExpression binding, Type type)
        {
            return new TypeAssertion(binding, type, /* byCompatibility */ true);
        }

        public static IAssertion assert_regex(IContextExpression binding, string pattern, bool caseSensitive, bool dontCompile)
        {
            if ((pattern ?? string.Empty).Length == 0)
                return StaticAssertion.False;

            //
            // NOTE: There is an assumption here that most uses of this
            // assertion will be for culture-insensitive matches. Since
            // it is difficult to imagine all the implications of involving
            // a culture at this point, it seems safer to just err with the
            // invariant culture settings.
            //

            var options = RegexOptions.CultureInvariant;

            if (!caseSensitive)
                options |= RegexOptions.IgnoreCase;

            if (!dontCompile)
                options |= RegexOptions.Compiled;

            return new RegexMatchAssertion(binding, new Regex(pattern, options));
        }

        public static IAssertion assert_jscript(XmlElement config)
        {
            if (config == null) throw new ArgumentNullException("config");
            
            //
            // The expression can be specified via an attribute or a
            // a child element named "expression". The element form takes
            // precedence.
            //

            // TODO More rigorous validation
            // For example, expression should not appear as an attribute and 
            // a child node. Also multiple child expressions should result in
            // an error.

            string expression = null;

            var expressionElement = config["expression"];
            if (expressionElement != null)
                expression = expressionElement.InnerText;

            if (expression == null)
                expression = config.GetAttribute("expression");

            return new JScriptAssertion(
                expression,
                DeserializeStringArray(config, "assemblies", "assembly", "name"),
                DeserializeStringArray(config, "imports", "import", "namespace"));
        }

        public static IAssertion Create(XmlElement config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            try
            {
                return CreateImpl(config);
            }
            catch (ConfigurationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ConfigurationException(e.Message, e);
            }
        }

        public static IAssertion[] Create(XmlNodeList nodes)
        {
            if (nodes == null) 
                throw new ArgumentNullException("nodes");

            //
            // First count the number of elements, which will be used to
            // allocate the array at its correct and final size.
            //

            var elementCount = 0;

            foreach (XmlNode child in nodes)
            {
                var nodeType = child.NodeType;

                //
                // Skip comments and whitespaces.
                //

                if (nodeType == XmlNodeType.Comment || nodeType == XmlNodeType.Whitespace)
                    continue;

                //
                // Otherwise all elements only.
                //

                if (nodeType != XmlNodeType.Element)
                {
                    throw new ConfigurationException(
                        string.Format("Unexpected type of node ({0}).", nodeType.ToString()),
                        child);
                }

                elementCount++;
            }

            //
            // In the second pass, create and configure the assertions
            // from each element.
            //

            var assertions = new IAssertion[elementCount];
            elementCount = 0;

            foreach (XmlNode node in nodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                    assertions[elementCount++] = Create((XmlElement) node);
            }

            return assertions;
        }

        private static IAssertion CreateImpl(XmlElement config)
        {
            Debug.Assert(config != null);

            var name = "assert_" + config.LocalName;
            
            if (name.IndexOf('-') > 0)
                name = name.Replace("-", "_");
            
            Type factoryType;

            var xmlns = config.NamespaceURI ?? string.Empty;

            if (xmlns.Length > 0)
            {
                string assemblyName, ns;

                if (!DecodeClrTypeNamespaceFromXmlNamespace(xmlns, out ns, out assemblyName) 
                    || ns.Length == 0 || assemblyName.Length == 0)
                {
                    throw new ConfigurationException(string.Format(
                        "Error decoding CLR type namespace and assembly from the XML namespace '{0}'.", xmlns));
                }
                
                var assembly = Assembly.Load(assemblyName);
                factoryType = assembly.GetType(ns + ".AssertionFactory", /* throwOnError */ true);
            }
            else
            {
                factoryType = typeof(AssertionFactory);
            }

            var method = factoryType.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                throw new MissingMemberException(string.Format(
                    "{0} does not have a method named {1}. " +
                    "Ensure that the method is named correctly and that it is public and static.",
                    factoryType, name));
            }

            var parameters = method.GetParameters();

            if (parameters.Length == 1 
                && parameters[0].ParameterType == typeof(XmlElement)
                && method.ReturnType == typeof(IAssertion))
            {
                var handler = (AssertionFactoryHandler) Delegate.CreateDelegate(typeof(AssertionFactoryHandler), factoryType, name);
                return handler(config); // TODO Check if Delegate.CreateDelegate could return null
            }

            return (IAssertion) method.Invoke(null, ParseArguments(method, config));
        }

        private static object[] ParseArguments(MethodInfo method, XmlElement config) 
        {
            Debug.Assert(method != null);
            Debug.Assert(config != null);

            var parameters = method.GetParameters();
            var args = new object[parameters.Length];
            
            foreach (var parameter in parameters)
                args[parameter.Position] = ParseArgument(parameter, config);

            return args;
        }

        private static readonly string[] _truths = new[] { "true", "yes", "on", "1" }; // TODO Remove duplication with SecurityConfiguration
        
        private static object ParseArgument(ParameterInfo parameter, XmlElement config) 
        {
            Debug.Assert(parameter != null);
            Debug.Assert(config != null);

            var name = parameter.Name;
            var type = parameter.ParameterType;
            string text;

            var attribute = config.GetAttributeNode(name);
            if (attribute != null)
            {
                text = attribute.Value;
            }
            else
            {
                var element = config[name];
                if (element == null)
                    return null;

                text = element.InnerText;
            }

            if (type == typeof(IContextExpression))
                return new WebDataBindingExpression(text);

            if (type == typeof(Type))
                return TypeResolution.GetType(text);

            if (type == typeof(bool))
            {
                text = text.Trim().ToLowerInvariant();
                return Boolean.TrueString.Equals(StringTranslation.Translate(Boolean.TrueString, text, _truths));
            }

            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFromInvariantString(text);
        }

        /// <remarks>
        /// Ideally, we would be able to use SoapServices.DecodeXmlNamespaceForClrTypeNamespace
        /// but that requires a link demand permission that will fail in partially trusted
        /// environments such as ASP.NET medium trust.
        /// </remarks>
        
        private static bool DecodeClrTypeNamespaceFromXmlNamespace(string xmlns, out string typeNamespace, out string assemblyName)
        {
            Debug.Assert(xmlns != null);

            assemblyName = string.Empty;
            typeNamespace = string.Empty;

            const string assemblyNS = "http://schemas.microsoft.com/clr/assem/";
            const string namespaceNS = "http://schemas.microsoft.com/clr/ns/";
            const string fullNS = "http://schemas.microsoft.com/clr/nsassem/";
            
            if (OrdinalStringStartsWith(xmlns, assemblyNS))
            {
                assemblyName = Uri.UnescapeDataString(xmlns.Substring(assemblyNS.Length));
                return assemblyName.Length > 0;
            }
            
            if (OrdinalStringStartsWith(xmlns, namespaceNS))
            {
                typeNamespace = xmlns.Substring(namespaceNS.Length);
                return typeNamespace.Length > 0;
            }
            
            if (OrdinalStringStartsWith(xmlns, fullNS))
            {
                var index = xmlns.IndexOf("/", fullNS.Length);
                typeNamespace = xmlns.Substring(fullNS.Length, index - fullNS.Length);
                assemblyName = Uri.UnescapeDataString(xmlns.Substring(index + 1));
                return assemblyName.Length > 0 && typeNamespace.Length > 0;
            }

            return false;
        }
        
        private static bool OrdinalStringStartsWith(string s, string prefix)
        {
            Debug.Assert(s != null);
            Debug.Assert(prefix != null);
            
            return s.Length >= prefix.Length && 
                string.CompareOrdinal(s.Substring(0, prefix.Length), prefix) == 0;
        }

        private static string[] DeserializeStringArray(XmlElement config,
            string containerName, string elementName, string valueName)
        {
            Debug.Assert(config != null);
            Debug.AssertStringNotEmpty(containerName);
            Debug.AssertStringNotEmpty(elementName);
            Debug.AssertStringNotEmpty(valueName);

            var list = new List<string>(4);

            var xpath = containerName + "/" + elementName + "/@" + valueName;
            foreach (XmlAttribute attribute in config.SelectNodes(xpath))
            {
                var value = attribute.Value ?? string.Empty;
                if (value.Length > 0)
                    list.Add(attribute.Value);
            }

            return list.ToArray();
        }
    }
}
