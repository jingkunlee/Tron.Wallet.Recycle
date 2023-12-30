using Microsoft.Extensions.DependencyInjection;

namespace Tron.Wallet.Recycle {
    using Newtonsoft.Json;
    using Tron.Wallet;
    using Tron.Wallet.Accounts;
    using Tron.Wallet.Contracts;

    internal class Program {
        private static async Task Main() {
            Console.WriteLine(" Program begin..\r\n");

            try {
                Config? config;
                var configPath = Path.GetFullPath("./config.json");
                if (!File.Exists(configPath)) throw new Exception(" config.json 配置文件不存在..");

                using (var streamReader = new StreamReader(configPath)) {
                    config = JsonConvert.DeserializeObject<Config>(streamReader.ReadToEnd());
                }

                if (config == null) throw new Exception(" 配置读取失败..");
                if (string.IsNullOrEmpty(config.TargetAddress)) throw new Exception(" 未设置回收地址..");

                Console.WriteLine($" 转出类型\t{config.Token}\r\n 目的地址\t{config.TargetAddress}\r\n");
                Console.WriteLine(" Press any key to continue.\r\n");
                Console.ReadKey();

                if (config.Accounts != null && config.Accounts.Count > 0) {
                    foreach (var account in config.Accounts) {
                        if (string.IsNullOrEmpty(account.PrivateKey)) continue;

                        var tronAccount = new TronAccount(account.PrivateKey, TronNetwork.MainNet);
                        account.Address = tronAccount.Address;

                        var tuple = GetBalanceByAddressByOnline(tronAccount.Address);
                        account.TrxBalance = tuple.Item1;
                        account.EtherBalance = tuple.Item2;

                        Console.WriteLine($" {tronAccount.PrivateKey}\t{tronAccount.Address}\t{account.TrxBalance} TRX\t{account.EtherBalance} USDT");

                        switch ((Token)config.Token) {
                            case Token.Trx: {
                                    if (tuple.Item1 > 0) {
                                        var result = await TrxTransferAsync(account.PrivateKey, config.TargetAddress, (long)(account.TrxBalance * 1000000L));
                                        Console.WriteLine($" {JsonConvert.SerializeObject(result)}");
                                    }
                                }
                                break;
                            case Token.Ether: {
                                    if (tuple.Item2 > 0) {
                                        var transactionId = await EtherTransferAsync(account.PrivateKey, config.TargetAddress, account.EtherBalance);
                                        Console.WriteLine($" {transactionId}");
                                    }
                                }
                                break;
                        }
                    }
                }
            } catch (Exception exception) {
                Console.WriteLine(exception.Message, exception);
            }

            Console.WriteLine("\r\n Program end..");
            Console.WriteLine(" Press any key to exit.");
            Console.ReadKey();
        }

        #region GetBalanceByAddressByOnline

        private static Tuple<decimal, decimal> GetBalanceByAddressByOnline(string address) {
            var tuple = new Tuple<decimal, decimal>(0, 0);

            var responseString = HttpClientHelper.Get($"https://api.trongrid.io/v1/accounts/{address}");
            if (string.IsNullOrEmpty(responseString)) return tuple;

            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
            if (responseObject == null) return tuple;

            if ((bool)responseObject.success != true) return tuple;
            if (responseObject.data == null || responseObject.data.Count == 0) return tuple;

            var obj = responseObject.data[0];
            if (obj == null) return tuple;

            var trxBalance = new decimal(0);

            var balance = obj.balance;
            if (balance != null) trxBalance = (long)balance / new decimal(1000000);

            var etherBalance = new decimal(0);

            var trc20Tokens = obj.trc20;
            if (trc20Tokens != null) {
                foreach (var trc20Token in trc20Tokens) {
                    var tokenBalance = trc20Token.TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t;
                    if (tokenBalance != null) etherBalance = (long)tokenBalance / new decimal(1000000);
                }
            }

            return new Tuple<decimal, decimal>(trxBalance, etherBalance);
        }

        #endregion

        #region TrxTransferAsync

        private static async Task<dynamic> TrxTransferAsync(string privateKey, string to, long amount) {
            var record = TronServiceExtension.GetRecord();
            var transactionClient = record.TronClient?.GetTransaction();

            var account = new TronAccount(privateKey, TronNetwork.MainNet);

            var transactionExtension = await transactionClient?.CreateTransactionAsync(account.Address, to, amount)!;
            var transactionId = transactionExtension.Txid.ToStringUtf8();

            var transactionSigned = transactionClient.GetTransactionSign(transactionExtension.Transaction, privateKey);
            var returnObj = await transactionClient.BroadcastTransactionAsync(transactionSigned);

            return new { Result = returnObj.Result, Message = returnObj.Message, TransactionId = transactionId };
        }

        #endregion

        #region EtherTransferAsync

        private static async Task<string> EtherTransferAsync(string privateKey, string toAddress, decimal amount) {
            const string contractAddress = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";

            var record = TronServiceExtension.GetRecord();
            var contractClientFactory = record.ServiceProvider.GetService<IContractClientFactory>();
            var contractClient = contractClientFactory?.CreateClient(ContractProtocol.TRC20);

            var account = new TronAccount(privateKey, TronNetwork.MainNet);

            const long feeAmount = 30 * 1000000L;

#pragma warning disable CS8602 // 解引用可能出现空引用。
            return await contractClient.TransferAsync(contractAddress, account, toAddress, amount, string.Empty, feeAmount);
#pragma warning restore CS8602 // 解引用可能出现空引用。
        }

        #endregion
    }

    #region Config

    [Serializable]
    internal class Config {
        public sbyte Token { get; set; }

        public string? TargetAddress { get; set; }

        public IList<Account>? Accounts { get; set; }
    }

    #endregion

    #region Token

    internal enum Token {
        Trx = 1,

        Ether = 2
    }

    #endregion

    #region Account

    [Serializable]
    internal class Account {
        public string? PrivateKey { get; set; }

        public string? Address { get; set; }

        public decimal EtherBalance { get; set; }

        public decimal TrxBalance { get; set; }
    }

    #endregion
}