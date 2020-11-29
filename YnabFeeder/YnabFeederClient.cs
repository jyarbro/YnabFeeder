using libfintx;
using libfintx.Data;
using libfintx.Swift;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YnabFeeder.Common;
using YnabFeeder.Common.Models;

namespace YnabFeeder {
    public class YnabFeederClient {
        readonly FintsOptions Options;

        FinTsClient FintsClient { get; set; }
        TANDialog Dialog { get; set; }

        public YnabFeederClient(
            IOptions<FintsOptions> options
        ) {
            Options = options.Value;
        }

        public async Task Run() {
            foreach (var bank in Options.Banks) {
                OpenBankConnection(bank);

                var accounts = await GetAccounts();

                foreach (var account in accounts) {
                    var transactions = await GetTransactions(account);

                    foreach (var transaction in transactions) {
                        if (!transaction.Pending) {
                            // ... do something ...
                        }
                    }
                }
            }
        }

        public void OpenBankConnection(Bank bankDetails) {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Opening Connection");

            var connection = new ConnectionDetails {
                HbciVersion = 300,
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

        public async Task<List<AccountInformation>> GetAccounts() {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Getting accounts");
            
            var result = await FintsClient.Accounts(Dialog);

            Console.WriteLine($"{nameof(FintsClient.Accounts)} messages:");

            foreach (var message in result.Messages) {
                Console.WriteLine($"{message.Code} {message.Message}");
            }

            return result.Data;
        }

        public async Task<List<SwiftStatement>> GetTransactions(AccountInformation account) {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Getting transactions");

            var result = await FintsClient.Transactions(Dialog, DateTime.Today.AddDays(-28), DateTime.Today);

            Console.WriteLine($"{nameof(FintsClient.Accounts)} messages:");

            foreach (var message in result.Messages) {
                Console.WriteLine($"{message.Code} {message.Message}");
            }

            return result.Data;
        }
    }
}
