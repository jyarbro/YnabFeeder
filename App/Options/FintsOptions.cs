using System.Collections.Generic;

namespace App.Options {
    public class FintsOptions {
        public const string Section = "fints";

        public int DaysToRetrieve { get; set; }
        public List<BankOptions> Banks { get; set; }
    }
}
