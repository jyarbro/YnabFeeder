using libfintx.Swift;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YnabFeeder.Common;
using YnabFeeder.Common.Utilities;

namespace YnabFeeder {
    public class TestClient_ProcessTransactions {
        readonly FintsOptions FintsOptions;
        readonly YnabOptions YnabOptions;

        YNAB.SDK.API Client { get; set; }

        public TestClient_ProcessTransactions(
            IOptions<FintsOptions> fintsOptions,
            IOptions<YnabOptions> ynabOptions
        ) {
            FintsOptions = fintsOptions.Value;
            YnabOptions = ynabOptions.Value;
        }

        public async Task Run() {
            OpenConnection();
            await ListBudgets();
        }

        void OpenConnection() {
            Client = new YNAB.SDK.API(YnabOptions.AccessToken);
        }

        async Task ListBudgets() {
            var budgetsResponse = await Client.Budgets.GetBudgetsAsync();

            budgetsResponse.Data.Budgets.ForEach(budget => {
                Console.WriteLine($"Budget Name: {budget.Name}");
            });
        }
    }
}
