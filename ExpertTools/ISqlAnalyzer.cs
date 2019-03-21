using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertTools
{
    public interface ISqlAnalyzer
    {
        Task StartCollectSqlData();

        Task StopCollectSqlData();

        Task HandleSqlData();

        Task LoadSqlDataIntoDatabase();
    }
}
