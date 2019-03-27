using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

namespace ExpertTools
{
    public interface ISqlAnalyzerSettings
    {
        string Name { get; }
        string SqlServer { get; set; }
        bool IntegratedSecurity { get; set; }
        string SqlUser { get; set; }
        string SqlUserPassword { get; set; }
    }
}
