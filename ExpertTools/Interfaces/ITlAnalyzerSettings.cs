using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

namespace ExpertTools
{
    public interface ITlAnalyzerSettings
    {
        string TlConfFolder { get; }
    }
}
