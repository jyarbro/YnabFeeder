using libfintx.Swift;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YNAB.SDK;
using YNAB.SDK.Api;
using YNAB.SDK.Client;
using YNAB.SDK.Model;
using YnabFeeder.Common;
using YnabFeeder.Common.Utilities;

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
            await TestTransaction();
        }

        void OpenConnection() {
            Client = new API(YnabOptions.AccessToken);
            Budget = Client.Budgets.GetBudgetById(YnabOptions.BudgetId).Data.Budget;
        }

        async Task TestTransaction() {
            var payee = Budget.Payees[4];

            var transaction = new SaveTransaction {
                AccountId = Budget.Accounts.First().Id,
                Amount = 100L,
                Date = DateTime.Now,
                PayeeId = payee.Id,
                PayeeName = payee.Name,
                CategoryId = Budget.Categories.First().Id,
                Memo = "Test Transaction",
                Approved = true,
                Cleared = SaveTransaction.ClearedEnum.Cleared
            };

            await Client.Transactions.CreateTransactionAsync(YnabOptions.BudgetId, new SaveTransactionsWrapper(transaction));
        }
    }
}
