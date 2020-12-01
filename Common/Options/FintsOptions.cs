using System.Collections.Generic;
using YnabFeeder.Common.Models;

namespace YnabFeeder.Common {
    public class FintsOptions {
        public string FilePath { get; set; }
        public List<Bank> Banks { get; set; }
    }
}
