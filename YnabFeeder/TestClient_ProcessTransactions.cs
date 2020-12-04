using libfintx;
using libfintx.Data;
using libfintx.Swift;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YnabFeeder.Common;
using YnabFeeder.Common.Models;
using YnabFeeder.Common.Utilities;

namespace YnabFeeder {
    public class TestClient_ProcessTransactions {
        readonly FintsOptions Options;

        public TestClient_ProcessTransactions(
            IOptions<FintsOptions> options
        ) {
            Options = options.Value;
        }

        public void Run() {
            var transactions = FileStorage.ReadFromJsonFile<List<SwiftStatement>>($"{Options.FilePath}\\transactions.json");

            foreach (var transaction in transactions) {
            }
        }
    }
}
