using System;

namespace App.Options {
    public class AccountOptions {
        public string Name { get; set; }
        public string Iban { get; set; }

        // This is the second long hash in the URL when viewing a specific account (i.e. a checking account)
        public Guid YnabAccountId { get; set; }
    }
}
