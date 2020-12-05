using libfintx.Swift;
using Microsoft.Extensions.Options;
using Nrrdio.Utilities;
using Nrrdio.YnabClient.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YNAB.SDK;
using YNAB.SDK.Model;

namespace YnabFeeder {
    public class TestClient_ProcessTransactions {
        readonly FintsOptions FintsOptions;
        readonly YnabOptions YnabOptions;

        API Client { get; set; }
        BudgetDetail Budget { get; set; }

        public TestClient_ProcessTransactions(
            IOptions<FintsOptions> fintsOptions,
            IOptions<YnabOptions> ynabOptions
        ) {
            FintsOptions = fintsOptions.Value;
            YnabOptions = ynabOptions.Value;
        }

        public async Task Run() {
            OpenConnection();
            await ProcessTransactions();
        }

        void OpenConnection() {
            Client = new API(YnabOptions.AccessToken);
            Budget = Client.Budgets.GetBudgetById(YnabOptions.BudgetId).Data.Budget;
        }

        async Task ProcessTransactions() {
            var swiftStatements = JsonFiles.ReadFromJsonFile<List<SwiftStatement>>($"{FintsOptions.FilePath}\\transactions.json");

            var accountId = FintsOptions.Banks.First().Accounts.First().YnabAccountId;
            var account = Budget.Accounts.First(o => o.Id == accountId);

            var ynabTransactions = new List<SaveTransaction>();

            var importIds = new List<string>();

            foreach (var swiftStatement in swiftStatements) {
                foreach (var swiftTransaction in swiftStatement.SwiftTransactions) {
                    var milliunits = Convert.ToInt64(swiftTransaction.Amount * 1000);
                    var importIdBase = $"NRRDIO:{milliunits}:{swiftTransaction.ValueDate:d}";

                    importIds.Add(importIdBase);
                    var occurrences = importIds.Count(o => o == importIdBase);

                    var importId = $"{importIdBase}:{occurrences}";

                    ynabTransactions.Add(new SaveTransaction {
                        AccountId = account.Id,
                        Amount = milliunits,
                        Date = swiftTransaction.ValueDate,
                        PayeeName = swiftTransaction.PartnerName,
                        Memo = swiftTransaction.SVWZ,
                        Cleared = SaveTransaction.ClearedEnum.Cleared,
                        ImportId = importId
                    });
                }
            }

            var response = await Client.Transactions.CreateTransactionAsync(YnabOptions.BudgetId, new SaveTransactionsWrapper(transactions: ynabTransactions));

            Console.WriteLine($"{response.Data.DuplicateImportIds.Count} duplicates found. {response.Data.TransactionIds.Count} added.");
        }
    }
}
