using System;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace ExpertTools.TechLog
{
    public class LogCfg
    {
        private XDocument document;
        private XNamespace ns = "http://v8.1c.ru/v8/tech-log";

        #region Comparison_Types

        /// <summary>
        /// Like
        /// </summary>
        public const string LIKE_CT = "like";
        /// <summary>
        /// Equal
        /// </summary>
        public const string EQ_CT = "eq";
        /// <summary>
        /// No equal
        /// </summary>
        public const string NE_CT = "ne";
        /// <summary>
        /// Greater or equal
        /// </summary>
        public const string GE_CT = "ge";
        /// <summary>
        /// Greater
        /// </summary>
        public const string GT_CT = "gt";
        /// <summary>
        /// Less or equal
        /// </summary>
        public const string LE_CT = "le";
        /// <summary>
        /// Less
        /// </summary>
        public const string LT_CT = "lt";

        #endregion

        /// <summary>
        /// Path to this config file
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Initializes a new instance of LogCfg class
        /// </summary>
        /// <param name="confFolder">"conf" folder of the 1C:Enterprise application instance</param>
        public LogCfg(string confFolder)
        {
            FileName = Path.Combine(confFolder, "logcfg.xml");
            document = new XDocument();

            document.Add(new XElement(ns + "config"));
        }

        /// <summary>
        /// Adds a new "log" element
        /// </summary>
        /// <param name="history">Collect period (in hours)</param>
        /// <param name="location">Path to the folder of collection data</param>
        public XElement AddLog(int history, string location)
        {
            var elem = new XElement(ns + "log");

            elem.Add(new XAttribute("history", history));
            elem.Add(new XAttribute("location", location));

            document.Root.Add(elem);

            return elem;
        }

        /// <summary>
        /// Adds a new "event" element into parent "log" element
        /// </summary>
        /// <param name="log">Parent "log" element</param>
        /// <param name="eventName">Event name</param>
        public XElement AddEvent(XElement log, string eventName)
        {
            var eventElem = new XElement(ns + "event");

            var eqEventElem = new XElement(ns + EQ_CT);

            eqEventElem.Add(new XAttribute("property", "name"));
            eqEventElem.Add(new XAttribute("value", eventName));

            eventElem.Add(eqEventElem);

            log.Add(eventElem);

            return eventElem;
        }

        /// <summary>
        /// Adds a new "event" element into all "log" elements
        /// </summary>
        /// <param name="eventName">Event name</param>
        public void AddEvent(string eventName)
        {
            document.Descendants(ns + "log").ToList().ForEach(c => AddEvent(c, eventName));
        }

        /// <summary>
        /// Adds a new filter element into "event" parent element
        /// </summary>
        /// <param name="eventElem">Parent "event" element</param>
        /// <param name="comparisonType">Comparison type for property</param>
        /// <param name="property">Name of the filtering property</param>
        /// <param name="value">Filter value</param>
        public void AddFilter(XElement eventElem, string comparisonType, string property, string value)
        {
            var filterElem = new XElement(ns + comparisonType);

            filterElem.Add(new XAttribute("property", property));
            filterElem.Add(new XAttribute("value", value));

            eventElem.Add(filterElem);
        }

        /// <summary>
        /// Adds a new filter element into all "event" elements
        /// </summary>
        /// <param name="comparisonType">Comparison type</param>
        /// <param name="property">Name of the filtering property</param>
        /// <param name="value">Filter value</param>
        public void AddFilter(string comparisonType, string property, string value)
        {
            document.Descendants(ns + "event").ToList().ForEach(c => AddFilter(c, comparisonType, property, value));
        }

        /// <summary>
        /// Adds a new "property" element
        /// </summary>
        /// <param name="logElem">Parent "log" element</param>
        /// <param name="property">Property name</param>
        public void AddProperty(XElement logElem, string property)
        {
            var propertyElem = new XElement(ns + "property");

            propertyElem.Add(new XAttribute("name", property));

            logElem.Add(propertyElem);
        }

        /// <summary>
        /// Adds a new "property" element into all "log" elements
        /// </summary>
        /// <param name="property">Property name</param>
        public void AddProperty(string property)
        {
            document.Descendants(ns + "log").ToList().ForEach(c => AddProperty(c, property));
        }

        /// <summary>
        /// Returns all paths of log folders
        /// </summary>
        /// <returns></returns>
        public string[] GetLogFoldersPaths()
        {
            List<string> paths = new List<string>();

            document.Descendants(ns + "log").ToList().ForEach(c => paths.Add(c.Attribute("location").Value));

            return paths.Distinct().ToArray();
        }

        /// <summary>
        /// Writes content to the file
        /// </summary>
        /// <returns></returns>
        public void Write()
        {
            var data = document.ToString();

            using (var stream = new StreamWriter(FileName))
            {
                stream.Write(data);
            }
        }

        /// <summary>
        /// Deletes config file
        /// </summary>
        public void Delete()
        {
            File.Delete(FileName);
        }
    }
}
