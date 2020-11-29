using System.Collections.Generic;

namespace YnabFeeder.Common.Models {
    public class Bank {
        public int Blz { get; set; }
        public string UserId { get; set; }
        public string Pin { get; set; }
        public string Endpoint { get; set; }
        public List<Account> Accounts { get; set; }
    }
}
