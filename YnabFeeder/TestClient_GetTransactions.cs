using libfintx;
using libfintx.Data;
using libfintx.Swift;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YnabFeeder.Common;
using YnabFeeder.Common.Models;
using YnabFeeder.Common.Utilities;

namespace YnabFeeder {
    public class TestClient_GetTransactions {
        readonly FintsOptions Options;

        FinTsClient FintsClient { get; set; }
        TANDialog Dialog { get; set; }

        public TestClient_GetTransactions(
            IOptions<FintsOptions> options
        ) {
            Options = options.Value;
        }

        public async Task Run() {
            OpenBankConnection(Options.Banks.First());

            var accounts = await GetAccounts();
            var transactions = await GetTransactions(accounts.First());

            FileStorage.WriteToJsonFile($"{Options.FilePath}\\transactions.json", transactions);
        }

        void OpenBankConnection(Bank bankDetails) {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Opening Connection");

            var connection = new ConnectionDetails {
                Blz = bankDetails.Blz,
                UserId = bankDetails.UserId,
                Pin = bankDetails.Pin,
                Url = bankDetails.Endpoint
            };

            FintsClient = new FinTsClient(connection);
            Dialog = new TANDialog((dialog) => {
                Console.WriteLine("--------------------------------------");
                Console.WriteLine($"{nameof(TANDialog)} messages:");

                foreach (var message in dialog.DialogResult.Messages) {
                    Console.WriteLine($"{message.Code} {message.Message}");
                }

                return Task.Run(() => string.Empty);
            });
        }

        async Task<List<AccountInformation>> GetAccounts() {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Getting accounts");
            
            var result = await FintsClient.Accounts(Dialog);

            Console.WriteLine($"{nameof(FintsClient.Accounts)} messages:");

            foreach (var message in result.Messages) {
                Console.WriteLine($"{message.Code} {message.Message}");
            }

            return result.Data;
        }

        async Task<List<SwiftStatement>> GetTransactions(AccountInformation account) {
            FintsClient.ConnectionDetails.Account = account.AccountNumber;

            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Getting transactions");

            var result = await FintsClient.Transactions(Dialog, DateTime.Today.AddDays(-10), DateTime.Today, saveMt940File: true);

            Console.WriteLine($"{nameof(FintsClient.Accounts)} messages:");

            foreach (var message in result.Messages) {
                Console.WriteLine($"{message.Code} {message.Message}");
            }

            return result.Data;
        }
    }
}
