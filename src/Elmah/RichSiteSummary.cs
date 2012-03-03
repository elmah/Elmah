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

[assembly: Elmah.Scc("$Id: RichSiteSummary.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah.ContentSyndication 
{
    #region Imports

    using System.Xml.Serialization;

    #endregion

    //
    // See RSS 0.91 specification at http://backend.userland.com/rss091 for
    // explanation of the XML vocabulary represented by the classes in this
    // file.
    //

    [ XmlRoot("rss", Namespace = "", IsNullable = false) ]
    public class RichSiteSummary 
    {
        public Channel channel;
        [ XmlAttribute ]
        public string version;
    }
    
    public class Channel 
    {
        public string title;
        [ XmlElement(DataType = "anyURI") ]
        public string link;
        public string description;
        [ XmlElement(DataType = "language") ]
        public string language;
        public string rating;
        public Image image;
        public TextInput textInput;
        public string copyright;
        [ XmlElement(DataType = "anyURI") ]
        public string docs;
        public string managingEditor;
        public string webMaster;
        public string pubDate;
        public string lastBuildDate;
        [ XmlArrayItem("hour", IsNullable = false) ]
        public int[] skipHours;
        [ XmlArrayItem("day", IsNullable = false) ]
        public Day[] skipDays;
        [ XmlElement("item") ]
        public Item[] item;
    }
    
    public class Image 
    {
        public string title;
        [ XmlElement(DataType = "anyURI") ]
        public string url;
        [ XmlElement(DataType = "anyURI") ]
        public string link;
        public int width;
        [ XmlIgnore() ]
        public bool widthSpecified;
        public int height;
        [ XmlIgnore() ]
        public bool heightSpecified;
        public string description;
    }
    
    public class Item 
    {
        public string title;
        public string description;
        public string pubDate;
        [ XmlElement(DataType = "anyURI") ]
        public string link;
    }
    
    public class TextInput 
    {
        public string title;
        public string description;
        public string name;
        [ XmlElement(DataType = "anyURI") ]
        public string link;
    }

    public enum Day 
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday,
    }
}
