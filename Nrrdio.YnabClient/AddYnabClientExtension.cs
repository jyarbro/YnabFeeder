using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nrrdio.YnabClient.Options;
using YNAB.SDK;

namespace Nrrdio.YnabClient {
    public static class AddYnabClientExtension {
        public static void AddYnabClient(this ServiceCollection services) {
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

            services.AddScoped<Client>();
        }
    }
}
