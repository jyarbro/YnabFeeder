using Microsoft.Extensions.DependencyInjection;
using Nrrdio.YnabClient;
using System;

Console.CancelKeyPress += ConsoleCancelKeyPress;

var services = new ServiceCollection();
services.AddYnabClient();

await services
    .BuildServiceProvider()
    .GetService<Client>()
    .Run();

static void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e) {
    e.Cancel = true;

    // ... cleanup and shutdown ...
}