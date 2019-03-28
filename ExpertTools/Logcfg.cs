using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ExpertTools
{
    /// <summary>
    /// Represents an configuration file of the 1C:Enterprise technology log
    /// </summary>
    public class Logcfg : XDocument
    {
        /// <summary>
        /// Namespace of the configuration file
        /// </summary>
        XNamespace _ns = "http://v8.1c.ru/v8/tech-log";
        string _techLogConfFolder;

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

        #region Event_Name

        /// <summary>
        /// MSSQL requests
        /// </summary>
        public const string DBMSSQL_EV = "DBMSSQL";
        /// <summary>
        /// Context of previous event
        /// </summary>
        public const string CONTEXT_EV = "Context";

        #endregion

        #region Event_Property

        /// <summary>
        /// Event name property
        /// </summary>
        public const string NAME_PR = "name";
        /// <summary>
        /// The date and the time of an event
        /// </summary>
        public const string DATETIME_PR = "DateTime";
        /// <summary>
        /// Duration of an event
        /// </summary>
        public const string DURATION_PR = "durationus";
        /// <summary>
        /// Client ID property
        /// </summary>
        public const string CLIENT_ID_PR = "t:clientID";
        /// <summary>
        /// Connect ID property
        /// </summary>
        public const string CONNECT_ID_PR = "t:connectID";
        /// <summary>
        /// User property
        /// </summary>
        public const string USER_PR = "Usr";
        /// <summary>
        /// Request statement
        /// </summary>
        public const string SQL_PR = "Sql";
        /// <summary>
        /// Process name
        /// </summary>
        public const string PROCESS_NAME_PR = "p:processName";
        /// <summary>
        /// Context of an event
        /// </summary>
        public const string CONTEXT_PR = "Context";
        /// <summary>
        /// Request statement on SDBL language
        /// </summary>
        public const string SDBL_PR = "Sdbl";

        #endregion

        /// <summary>
        /// Path of the config file
        /// </summary>
        public string FilePath => Path.Combine(_techLogConfFolder, "logcfg.xml");

        /// <summary>
        /// Initialize a new instance of LogCfg class
        /// </summary>
        /// <param name="analyzerSettings">Analyzer settings</param>
        public Logcfg(ITlAnalyzerSettings analyzerSettings)
        {
            _techLogConfFolder = analyzerSettings.TlConfFolder;

            Add(new XElement(_ns + "config"));
        }

        /// <summary>
        /// Add a new "log" element
        /// </summary>
        /// <param name="history">Collect period (in hours)</param>
        /// <param name="location">Path to the folder of collection data</param>
        public XElement AddLog(string history, string location)
        {
            var elem = new XElement(_ns + "log");

            elem.Add(new XAttribute("history", history));
            elem.Add(new XAttribute("location", location));

            Root.Add(elem);

            return elem;
        }

        /// <summary>
        /// Add a new "event" element into parent "log" element
        /// </summary>
        /// <param name="logElem">Parent "log" element</param>
        /// <param name="eventName">Event name</param>
        public XElement AddEvent(XElement logElem, string eventName)
        {
            var eventElem = new XElement(_ns + "event");

            var eqEventElem = new XElement(_ns + EQ_CT);

            eventElem.Add(new XAttribute("property", NAME_PR));
            eventElem.Add(new XAttribute("value", eventName));

            eventElem.Add(eqEventElem);

            logElem.Add(eventElem);

            return eventElem;
        }

        /// <summary>
        /// Add a new "event" element into all "log" elements
        /// </summary>
        /// <param name="eventName">Event name</param>
        public XElement AddEvent(string eventName)
        {
            var eventElem = new XElement(_ns + "event");

            var eqEventElem = new XElement(_ns + EQ_CT);

            eqEventElem.Add(new XAttribute("property", NAME_PR));
            eqEventElem.Add(new XAttribute("value", eventName));

            eventElem.Add(eqEventElem);

            Descendants(_ns + "log").ToList().ForEach(c => c.Add(eventElem));

            return eventElem;
        }

        /// <summary>
        /// Add a new filter element into "event" parent element
        /// </summary>
        /// <param name="eventElem">Parent "event" element</param>
        /// <param name="comparisonType">Comparison type for property</param>
        /// <param name="property">Filtered property name</param>
        /// <param name="value">Filter value</param>
        public XElement AddFilter(XElement eventElem, string comparisonType, string property, string value)
        {
            var filterElem = new XElement(_ns + comparisonType);

            filterElem.Add(new XAttribute("property", property));
            filterElem.Add(new XAttribute("value", value));

            eventElem.Add(filterElem);

            return filterElem;
        }

        /// <summary>
        /// Adds a new filter element into all "event" elements
        /// </summary>
        /// <param name="comparisonType">Comparison type</param>
        /// <param name="property">Filtered property name</param>
        /// <param name="value">Filter value</param>
        public XElement AddFilter(string comparisonType, string property, string value)
        {
            var filterElem = new XElement(_ns + comparisonType);

            filterElem.Add(new XAttribute("property", property));
            filterElem.Add(new XAttribute("value", value));

            Descendants(_ns + "event").ToList().ForEach(c => c.Add(filterElem));

            return filterElem;
        }

        /// <summary>
        /// Adds a new "property" element
        /// </summary>
        /// <param name="logElem">Parent "log" element</param>
        /// <param name="property">Property name</param>
        public void AddProperty(XElement logElem, string property)
        {
            var propertyElem = new XElement(_ns + "property");

            propertyElem.Add(new XAttribute("name", property));

            logElem.Add(propertyElem);
        }

        /// <summary>
        /// Adds a new "property" element into all "log" elements
        /// </summary>
        /// <param name="property">Property name</param>
        public void AddProperty(string property)
        {
            var propertyElem = new XElement(_ns + "property");

            propertyElem.Add(new XAttribute("name", property));

            Descendants(_ns + "log").ToList().ForEach(c => c.Add(propertyElem));
        }

        /// <summary>
        /// Returns all paths of log folders
        /// </summary>
        /// <returns></returns>
        public string[] GetLogPaths()
        {
            List<string> paths = new List<string>();

            Descendants(_ns + "log").ToList().ForEach(c => paths.Add(c.Attribute("location").Value));

            return paths.Distinct().ToArray();
        }

        /// <summary>
        /// Writes the new configuration file (replace if exists)
        /// </summary>
        /// <returns></returns>
        public async Task Write()
        {
            using (var writer = new StreamWriter(FilePath))
            {
                await writer.WriteAsync(ToString());
            }
        }

        /// <summary>
        /// Deletes the configuration file
        /// </summary>
        public void Delete()
        {
            File.Delete(FilePath);
        }
    }
}
