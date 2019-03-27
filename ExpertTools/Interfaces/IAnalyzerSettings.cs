using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ExpertTools
{
    public interface IAnalyzerSettings
    {
        string Name { get; }

        Task Check();
    }
}
