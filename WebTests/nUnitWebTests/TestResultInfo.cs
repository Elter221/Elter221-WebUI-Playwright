using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nUnitWebTests
{
    public class TestResultInfo
    {
        public string TestName { get; set; }
        public string Status { get; set; }
        public long DurationMs { get; set; }
        public string Category { get; set; }
        public string ErrorMessage { get; set; }
    }
}
