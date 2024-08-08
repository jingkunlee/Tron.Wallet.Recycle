using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tron.Wallet.Recycle {
    using Tron;

    public record TronRecord(IServiceProvider ServiceProvider, ITronClient? TronClient, IOptions<TronNetOptions>? Options);

    public static class TronServiceExtension {
        private static IServiceProvider AddTron() {
            IServiceCollection services = new ServiceCollection();
            services.AddTronNet(x => {
                x.Network = TronNetwork.MainNet;
                x.Channel = new GrpcChannelOption { Host = "grpc.trongrid.io", Port = 50051 };
                x.SolidityChannel = new GrpcChannelOption { Host = "grpc.trongrid.io", Port = 50052 };
                x.ApiKey = "faffed07-99b4-41a4-9c30-a614674fdbb4";
            });
            services.AddLogging();
            return services.BuildServiceProvider();
        }

        public static TronRecord GetRecord() {
            var provider = AddTron();
            var client = provider.GetService<ITronClient>();
            var options = provider.GetService<IOptions<TronNetOptions>>();

            return new TronRecord(provider, client, options);
        }
    }
}
