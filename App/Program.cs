using App;
using App.Options;
using App.Services;
using libfintx;
using libfintx.Data;
using libfintx.Swift;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nrrdio.Utilities.Loggers;
using Nrrdio.Utilities.Loggers.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YNAB.SDK;
using YNAB.SDK.Model;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureHostConfiguration((config) => {
        config.AddEnvironmentVariables("DOTNET_");
    })
    .ConfigureAppConfiguration((host, config) => {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{host.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        config.AddEnvironmentVariables();

        if (args is not { Length: >0 }) {
            config.AddCommandLine(args);
        }
    })
    .ConfigureServices((host, services) => {
        services.AddHostedService<AppHost>();

        services.Configure<FintsOptions>((options) => {
            host.Configuration.GetSection(FintsOptions.Section).Bind(options);
        });

        services.Configure<YnabOptions>((options) => {
            host.Configuration.GetSection(YnabOptions.Section).Bind(options);
        });

        services.AddSingleton((services) => {
            var ynabOptions = services.GetService<IOptions<YnabOptions>>().Value;
            return new API(ynabOptions.AccessToken);
        });

        services.AddSingleton<DataContext>();
        services.AddSingleton<ILogEntryRepository, LogEntryRepository>();
        services.AddSingleton<ILoggerProvider, DatabaseLoggerProvider>();
        services.AddSingleton<ILoggerProvider, ColorConsoleLoggerProvider>();
        services.AddLogging();
    })
    .Build()
    .RunAsync();

public class AppHost : IHostedService {
    IHostApplicationLifetime AppLifetime { get; init; }
    ILogger<AppHost> Logger { get; init; }
    FintsOptions FintsOptions { get; init; }
    YnabOptions YnabOptions { get; init; }
    API YnabApi { get; init; }

    BudgetDetail Budget { get; set; }
    FinTsClient FintsClient { get; set; }
    TANDialog Dialog { get; set; }

    public AppHost(
        IHostApplicationLifetime appLifetime,
        ILogger<AppHost> logger,
        IOptions<FintsOptions> fintsOptions,
        IOptions<YnabOptions> ynabOptions,
        API api
    ) {
        AppLifetime = appLifetime;
        Logger = logger;
        FintsOptions = fintsOptions.Value;
        YnabOptions = ynabOptions.Value;
        YnabApi = api;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        Logger.LogTrace($"{nameof(StartAsync)}({nameof(cancellationToken)})");

        try {
            Budget = YnabApi.Budgets.GetBudgetById(YnabOptions.BudgetId).Data.Budget;

            foreach (var bankOptions in FintsOptions.Banks) {
                var fintsConnectionDetails = new ConnectionDetails {
                    Blz = bankOptions.Blz,
                    UserId = bankOptions.UserId,
                    Pin = bankOptions.Pin,
                    Url = bankOptions.Endpoint
                };

                Logger.LogWarning($"Opening FinTS connection.");

                FintsClient = new FinTsClient(fintsConnectionDetails);
                Dialog = new TANDialog(dialogCallback);

                List<AccountInformation> fintsAccounts = await loadFintsAccounts();

                foreach (var fintsAccount in fintsAccounts) {
                    var accountOptions = bankOptions.Accounts.FirstOrDefault(o => o.Iban == fintsAccount.AccountIban);

                    if (accountOptions is null) {
                        Logger.LogInformation($"Account not found with IBAN {fintsAccount.AccountIban}");
                    }
                    else {
                        var swiftStatements = await loadSwiftStatements(fintsAccount);
                        await sendToYnab(accountOptions.YnabAccountId, swiftStatements);
                    }
                }
            }
        }
        catch (Exception e) {
            Logger.LogError(e, $"{nameof(AppHost)} process failed with exception.");
        }
        finally {
            AppLifetime.StopApplication();
        }

        Task<string> dialogCallback(TANDialog dialog) {
            Logger.LogTrace($"{nameof(dialogCallback)}({nameof(dialog)})");

            var logMessage = $"{nameof(TANDialog)} messages:\n";

            foreach (var message in dialog.DialogResult.Messages) {
                logMessage += $"{message.Code} {message.Message}\n";
            }

            Logger.LogInformation(logMessage);

            return Task.Run(() => string.Empty);
        }

        async Task<List<AccountInformation>> loadFintsAccounts() {
            Logger.LogTrace($"{nameof(loadFintsAccounts)}()");

            var fintsAccountsResult = await FintsClient.Accounts(Dialog);

            var logMessage = $"{nameof(FintsClient.Accounts)} messages:\n";

            foreach (var message in fintsAccountsResult.Messages) {
                logMessage += $"{message.Code} {message.Message}\n";
            }

            Logger.LogInformation(logMessage);

            return fintsAccountsResult.Data;
        }

        async Task<List<SwiftStatement>> loadSwiftStatements(AccountInformation fintsAccount) {
            Logger.LogTrace($"{nameof(loadSwiftStatements)}({nameof(fintsAccount)}:\n{fintsAccount})");

            FintsClient.ConnectionDetails.Account = fintsAccount.AccountNumber;

            var fintsTransactionsResult = await FintsClient.Transactions(Dialog, DateTime.Today.AddDays(-10), DateTime.Today);

            var logMessage = $"{nameof(FintsClient.Transactions)} messages:\n";

            foreach (var message in fintsTransactionsResult.Messages) {
                logMessage += $"{message.Code} {message.Message}\n";
            }

            Logger.LogInformation(logMessage);

            return fintsTransactionsResult.Data;
        }

        async Task sendToYnab(Guid ynabAccountId, List<SwiftStatement> swiftStatements) {
            Logger.LogTrace($"{nameof(sendToYnab)}({nameof(ynabAccountId)}: {ynabAccountId}, {nameof(swiftStatements)}: {swiftStatements.Count})");

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
                        AccountId = ynabAccountId,
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
                Logger.LogInformation($"Sending {ynabTransactions.Count} transactions to YNAB.");

                var response = await YnabApi.Transactions.CreateTransactionAsync(Budget.Id.ToString(), new SaveTransactionsWrapper(transactions: ynabTransactions));

                Logger.LogWarning($"{response.Data.DuplicateImportIds.Count} duplicates found on YNAB. {response.Data.TransactionIds.Count} added.");
            }
            else {
                Logger.LogInformation("No transactions to send to YNAB.");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
