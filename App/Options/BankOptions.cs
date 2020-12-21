using System.Collections.Generic;

namespace App.Options {
    public class BankOptions {
        public int Blz { get; set; }
        public string UserId { get; set; }
        public string Pin { get; set; }
        public string Endpoint { get; set; }
        public List<AccountOptions> Accounts { get; set; }
    }
}
