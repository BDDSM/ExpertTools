using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace ExpertTools
{
    /// <summary>
    /// Represents a settings for an instance of the QueriesAnalyzer class
    /// </summary>
    public class QueriesAnalyzerSettings : IAnalyzerSettings, ISqlAnalyzerSettings, ITlAnalyzerSettings
    {
        public string Name => "QueriesAnalyzer";

        /// <summary>
        /// Path to the temp folder
        /// </summary>
        [Setting]
        public string TempFolder { get; set; } = "";

        /// <summary>
        /// Path to the parent folder of the "logcfg.xml" file
        /// </summary>
        [Setting]
        public string TlConfFolder { get; set; } = "";

        /// <summary>
        /// Path to the folder of the tech log data collection
        /// </summary>
        [Setting]
        public string TlFolder { get; set; } = "";

        /// <summary>
        /// Path to the folder of the sql trace data
        /// </summary>
        [Setting]
        public string SqlTraceFolder { get; set; } = "";

        /// <summary>
        /// A number of minutes of data collecting
        /// </summary>
        [Setting]
        public int CollectPeriod { get; set; } = 0;

        /// <summary>
        /// A flag of the filter by database
        /// </summary>
        [Setting]
        public bool FilterByDatabase { get; set; } = false;

        /// <summary>
        /// Value of the database field for filtering
        /// </summary>
        [Setting]
        public string Database1C { get; set; } = "";

        /// <summary>
        /// Value of the database field for filtering
        /// </summary>
        [Setting]
        public string DatabaseSQL { get; set; } = "";

        /// <summary>
        /// A flag of the filter by duration
        /// </summary>
        [Setting]
        public bool FilterByDuration { get; set; } = false;

        /// <summary>
        /// Value of the database field for filtering
        /// </summary>
        [Setting]
        public int Duration { get; set; } = 0;

        /// <summary>
        /// Sql server address
        /// </summary>
        [Setting]
        public string SqlServer { get; set; } = "";

        /// <summary>
        /// Integrated security flag
        /// </summary>
        [Setting]
        public bool IntegratedSecurity { get; set; } = false;

        /// <summary>
        /// User of the sql server instance
        /// </summary>
        [Setting]
        public string SqlUser { get; set; } = "";

        /// <summary>
        /// Password of the sql server instance user
        /// </summary>
        [Setting]
        public string SqlUserPassword { get; set; } = "";

        /// <summary>
        /// Check the settings for mistakes
        /// </summary>
        /// <returns></returns>
        public async Task Check()
        {
            await SqlHelper.CheckConnection(this);
            await SqlHelper.CheckDatabase(this);
            Common.CheckFolderExisted(SqlTraceFolder);
            Common.CheckFolderExisted(TlConfFolder);
            Common.CheckFolderWriting(TlConfFolder);
            Common.CheckFolderExisted(TlFolder);
            Common.CheckFolderExisted(TempFolder);
            Common.CheckFolderWriting(TempFolder);
        }
    }
}
