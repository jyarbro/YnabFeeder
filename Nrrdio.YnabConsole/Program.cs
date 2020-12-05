using Microsoft.Extensions.DependencyInjection;
using Nrrdio.YnabClient;

var services = new ServiceCollection();
services.AddYnabClient();

await services
    .BuildServiceProvider()
    .GetService<Client>()
    .Run();