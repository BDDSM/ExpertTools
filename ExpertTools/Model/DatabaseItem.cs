using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertTools
{
    public class DatabaseItem
    {
        public string Base1C { get; set; }
        public string BaseSql { get; set; }

        public DatabaseItem(string base1C, string baseSql)
        {
            Base1C = base1C;
            BaseSql = baseSql;
        }
    }
}
