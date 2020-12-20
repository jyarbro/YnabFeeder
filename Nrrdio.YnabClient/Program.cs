using Microsoft.Extensions.Hosting;
using Nrrdio.YnabClient;

await Client.CreateHostBuilder(args).Build().RunAsync();
