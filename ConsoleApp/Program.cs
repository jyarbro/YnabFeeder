﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using YnabFeeder;
using YnabFeeder.Common;

Console.CancelKeyPress += ConsoleCancelKeyPress;

var services = ConfigureServices();

var test = services.BuildServiceProvider().GetService<YnabFeederClient>();

await test.Run();


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

    services.AddScoped<YnabFeeder.YnabFeederClient>();

    return services;
}

static void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e) {
    e.Cancel = true;

    // ... cleanup and shutdown ...
}