using System.Collections.Generic;

namespace App.Options {
    public class FintsOptions {
        public const string Section = "fints";

        public string FilePath { get; set; }
        public List<BankOptions> Banks { get; set; }
    }
}
