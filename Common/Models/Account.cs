namespace YnabFeeder.Common.Models {
    public class Account {
        public string Name { get; set; }
        public string Iban { get; set; }

        // This is the second long hash in the URL when viewing a specific account (i.e. a checking account)
        public string YnabAccountId { get; set; }
    }
}
