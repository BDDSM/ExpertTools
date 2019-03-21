using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertTools
{
    public interface ITlAnalyzer
    {
        Task StartCollectTlData();

        void StopCollectTlData();

        Task HandleTlData();

        Task LoadTlDataIntoDatabase();
    }
}
