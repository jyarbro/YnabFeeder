using libfintx;
using libfintx.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nrrdio.Utilities.Loggers;
using Nrrdio.Utilities.Loggers.Contracts;
using Nrrdio.YnabClient.Options;
using Nrrdio.YnabClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YNAB.SDK;
using YNAB.SDK.Model;

namespace Nrrdio.YnabClient {
    public class Client : IHostedService {
        ILogger<Client> Logger { get; init; }
        FintsOptions FintsOptions { get; init; }
        YnabOptions YnabOptions { get; init; }
        API YnabApi { get; init; }

        BudgetDetail Budget { get; set; }
        FinTsClient FintsClient { get; set; }
        TANDialog Dialog { get; set; }

        public Client(
            ILogger<Client> logger,
            IOptions<FintsOptions> fintsOptions,
            IOptions<YnabOptions> ynabOptions,
            API api
        ) {
            Logger = logger;
            FintsOptions = fintsOptions.Value;
            YnabOptions = ynabOptions.Value;
            YnabApi = api;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            Budget = YnabApi.Budgets.GetBudgetById(YnabOptions.BudgetId).Data.Budget;

            foreach (var bankOptions in FintsOptions.Banks) {
                var fintsConnectionDetails = new ConnectionDetails {
                    Blz = bankOptions.Blz,
                    UserId = bankOptions.UserId,
                    Pin = bankOptions.Pin,
                    Url = bankOptions.Endpoint
                };

                FintsClient = new FinTsClient(fintsConnectionDetails);
                Dialog = new TANDialog((dialog) => {
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine($"{nameof(TANDialog)} messages:");

                    foreach (var message in dialog.DialogResult.Messages) {
                        Console.WriteLine($"{message.Code} {message.Message}");
                    }

                    return Task.Run(() => string.Empty);
                });

                Console.WriteLine("--------------------------------------");
                Console.WriteLine("Getting accounts");

                var fintsAccountsResult = await FintsClient.Accounts(Dialog);

                Console.WriteLine($"{nameof(FintsClient.Accounts)} messages:");

                foreach (var message in fintsAccountsResult.Messages) {
                    Console.WriteLine($"{message.Code} {message.Message}");
                }

                var fintsAccounts = fintsAccountsResult.Data;

                foreach (var fintsAccount in fintsAccounts) {
                    Console.WriteLine("--------------------------------------");

                    var accountOptions = bankOptions.Accounts.FirstOrDefault(o => o.Iban == fintsAccount.AccountIban);

                    if (accountOptions is null) {
                        Console.WriteLine($"Account not found with IBAN {fintsAccount.AccountIban}");
                    }
                    else {
                        Console.WriteLine($"Getting transactions for {fintsAccount.AccountIban}");

                        FintsClient.ConnectionDetails.Account = fintsAccount.AccountNumber;

                        var fintsTransactionsResult = await FintsClient.Transactions(Dialog, DateTime.Today.AddDays(-10), DateTime.Today);

                        Console.WriteLine($"{nameof(FintsClient.Transactions)} messages:");

                        foreach (var message in fintsTransactionsResult.Messages) {
                            Console.WriteLine($"{message.Code} {message.Message}");
                        }

                        var swiftStatements = fintsTransactionsResult.Data;

                        var ynabTransactions = new List<SaveTransaction>();

                        var importIds = new List<string>();

                        foreach (var swiftStatement in swiftStatements) {
                            foreach (var swiftTransaction in swiftStatement.SwiftTransactions) {
                                var milliunits = Convert.ToInt64(swiftTransaction.Amount * 1000);
                                var importIdBase = $"NRRDIO:{milliunits}:{swiftTransaction.ValueDate:d}";

                                importIds.Add(importIdBase);
                                var occurrences = importIds.Count(o => o == importIdBase);

                                var importId = $"{importIdBase}:{occurrences}";

                                var memo = swiftTransaction.SVWZ ?? string.Empty;

                                if (memo.Length > 200) {
                                    memo = memo.Substring(0, 200);
                                }

                                ynabTransactions.Add(new SaveTransaction {
                                    AccountId = accountOptions.YnabAccountId,
                                    Amount = milliunits,
                                    Date = swiftTransaction.ValueDate,
                                    PayeeName = swiftTransaction.PartnerName,
                                    Memo = memo,
                                    Cleared = SaveTransaction.ClearedEnum.Cleared,
                                    ImportId = importId
                                });
                            }
                        }

                        if (ynabTransactions.Any()) {
                            Console.WriteLine($"Sending {ynabTransactions.Count} transactions to YNAB.");

                            var response = await YnabApi.Transactions.CreateTransactionAsync(Budget.Id.ToString(), new SaveTransactionsWrapper(transactions: ynabTransactions));

                            Console.WriteLine($"{response.Data.DuplicateImportIds.Count} duplicates found on YNAB. {response.Data.TransactionIds.Count} added.");
                        }
                        else {
                            Console.WriteLine("No transactions to send to YNAB.");
                        }
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host
                .CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => {
                    services.AddHostedService<Client>();
                    
                    var configurationBuilder = new ConfigurationBuilder();

                    var configuration = configurationBuilder
                        .AddJsonFile("appsettings.json")
                        .Build();

                    services.Configure<FintsOptions>((options) => {
                        configuration.GetSection("fints").Bind(options);
                    });

                    services.Configure<YnabOptions>((options) => {
                        configuration.GetSection("ynab").Bind(options);
                    });

                    services.AddScoped<IConfiguration>((services) => configuration);

                    services.AddScoped((services) => {
                        var ynabOptions = services.GetService<IOptions<YnabOptions>>().Value;
                        return new API(ynabOptions.AccessToken);
                    });

                    services.AddDbContext<DataContext>(options => options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

                    services.AddSingleton<ILogEntryRepository, LogEntryRepository>();
                    services.AddSingleton<ILoggerProvider, DatabaseLoggerProvider>();
                    services.AddLogging();
                });
    }
}
