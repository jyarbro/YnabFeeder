using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using YnabFeeder;
using YnabFeeder.Common;
using YnabFeeder.Common.Utilities;

Console.CancelKeyPress += ConsoleCancelKeyPress;

var services = ConfigureServices();

var test = services.BuildServiceProvider().GetService<IOptions<FintsOptions>>();
var options = test.Value;

FileStorage.WriteToJsonFile(options.FilePath + "\\testfile.json", options);


static ServiceCollection ConfigureServices() {
    var configurationBuilder = new ConfigurationBuilder();

    var configuration = configurationBuilder
        .AddJsonFile("appsettings.json")
        .Build();

    var services = new ServiceCollection();

    services.Configure<FintsOptions>((options) => {
        configuration.GetSection("fints").Bind(options);
    });

    services.Configure<YnabOptions>((options) => {
        configuration.GetSection("ynab").Bind(options);
    });

    services.AddScoped<IConfiguration>((services) => configuration);

    services.AddScoped<YnabFeederClient>();

    return services;
}

static void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e) {
    e.Cancel = true;

    // ... cleanup and shutdown ...
}