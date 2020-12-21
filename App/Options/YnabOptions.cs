namespace App.Options {
    public class YnabOptions {
        public const string Section = "ynab";
       
        // Get this from YNAB's site
        public string AccessToken { get; set; }

        // This is the first hash in the URL of a budget
        public string BudgetId { get; set; }
    }
}
