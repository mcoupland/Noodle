using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noodle
{
    public class SearchResult
    {
        public int Index;
        public string Name;
        public string Location;
        public int? LineNumber;
        public string Value;

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
