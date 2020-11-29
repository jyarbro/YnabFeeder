using libfintx;
using libfintx.Data;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using YnabFeeder.Common;

namespace YnabFeeder {
    public class Class1 {
        readonly FintsOptions Options;

        public Class1(
            IOptions<FintsOptions> options
        ) {
            Options = options.Value;
        }

        public async Task Test() {
            foreach (var bank in Options.Banks) {
                var connection = new ConnectionDetails();
                connection.HbciVersion = 300;
                connection.Blz = bank.Blz;
                connection.UserId = bank.UserId;
                connection.Pin = bank.Pin;
                connection.Url = bank.Endpoint;

                var client = new FinTsClient(connection);

                var tanDialog = new TANDialog((dialog) => {
                    foreach (var message in dialog.DialogResult.Messages) {
                        Console.WriteLine($"{message.Code} {message.Message}");
                    }

                    return Task.Run(() => string.Empty);
                });

                var result = await client.Accounts(tanDialog);

                foreach (var message in result.Messages) {
                    Console.WriteLine($"{message.Code} {message.Message}");
                }

                foreach (var account in result.Data) {
                    Console.Write($"IBAN: {account.AccountIban}");
                }
            }
        }
    }
}
