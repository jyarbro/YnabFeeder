using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nrrdio.Utilities.Loggers;
using Nrrdio.Utilities.Loggers.Contracts;
using Nrrdio.YnabClient;
using Nrrdio.YnabClient.Options;
using Nrrdio.YnabClient.Services;
using YNAB.SDK;

await Host
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
    })
    .Build()
    .RunAsync();
