// ------------------------------------------------------------------------------------------------------
// <copyright file="CeloStatCalculator.cs" company="Nomis">
// Copyright (c) Nomis, 2023. All rights reserved.
// The Application under the MIT license. See LICENSE file in the solution root for full license information.
// </copyright>
// ------------------------------------------------------------------------------------------------------

using System.Numerics;

using Nomis.Blockchain.Abstractions.Calculators;
using Nomis.Blockchain.Abstractions.Models;
using Nomis.Celoscan.Interfaces.Extensions;
using Nomis.Celoscan.Interfaces.Models;
using Nomis.Utils.Extensions;

namespace Nomis.Celoscan.Calculators
{
    /// <summary>
    /// Celo wallet stats calculator.
    /// </summary>
    internal sealed class CeloStatCalculator :
        IStatCalculator<CeloWalletStats, CeloTransactionIntervalData>
    {
        private readonly string _address;
        private readonly decimal _balance;
        private readonly decimal _usdBalance;
        private readonly IEnumerable<CeloscanAccountNormalTransaction> _transactions;
        private readonly IEnumerable<CeloscanAccountInternalTransaction> _internalTransactions;
        private readonly IEnumerable<ICeloscanAccountNftTokenEvent> _tokenTransfers;
        private readonly IEnumerable<CeloscanAccountERC20TokenEvent> _erc20TokenTransfers;

        public CeloStatCalculator(
            string address,
            decimal balance,
            decimal usdBalance,
            IEnumerable<CeloscanAccountNormalTransaction> transactions,
            IEnumerable<CeloscanAccountInternalTransaction> internalTransactions,
            IEnumerable<ICeloscanAccountNftTokenEvent> tokenTransfers,
            IEnumerable<CeloscanAccountERC20TokenEvent> erc20TokenTransfers)
        {
            _address = address;
            _balance = balance;
            _usdBalance = usdBalance;
            _transactions = transactions;
            _internalTransactions = internalTransactions;
            _tokenTransfers = tokenTransfers;
            _erc20TokenTransfers = erc20TokenTransfers;
        }

        public CeloWalletStats GetStats()
        {
            if (!_transactions.Any())
            {
                return new()
                {
                    NoData = true
                };
            }

            var intervals = IStatCalculator
                .GetTransactionsIntervals(_transactions.Select(x => x.TimeStamp!.ToDateTime())).ToList();
            if (intervals.Count == 0)
            {
                return new()
                {
                    NoData = true
                };
            }

            var monthAgo = DateTime.Now.AddMonths(-1);
            var yearAgo = DateTime.Now.AddYears(-1);

            var soldTokens = _tokenTransfers.Where(x => x.From?.Equals(_address, StringComparison.InvariantCultureIgnoreCase) == true).ToList();
            var soldSum = IStatCalculator
                .GetTokensSum(soldTokens.Select(x => x.Hash!), _internalTransactions.Select(x => (x.Hash!, BigInteger.TryParse(x.Value, out var amount) ? amount : 0)));

            var soldTokensIds = soldTokens.Select(x => x.GetTokenUid());
            var buyTokens = _tokenTransfers.Where(x => x.To?.Equals(_address, StringComparison.InvariantCultureIgnoreCase) == true && soldTokensIds.Contains(x.GetTokenUid()));
            var buySum = IStatCalculator
                .GetTokensSum(buyTokens.Select(x => x.Hash!), _internalTransactions.Select(x => (x.Hash!, BigInteger.TryParse(x.Value, out var amount) ? amount : 0)));

            var buyNotSoldTokens = _tokenTransfers.Where(x => x.To?.Equals(_address, StringComparison.InvariantCultureIgnoreCase) == true && !soldTokensIds.Contains(x.GetTokenUid()));
            var buyNotSoldSum = IStatCalculator
                .GetTokensSum(buyNotSoldTokens.Select(x => x.Hash!), _internalTransactions.Select(x => (x.Hash!, BigInteger.TryParse(x.Value, out var amount) ? amount : 0)));

            int holdingTokens = _tokenTransfers.Count() - soldTokens.Count;
            decimal nftWorth = buySum == 0 ? 0 : (decimal)soldSum / (decimal)buySum * (decimal)buyNotSoldSum;
            int contractsCreated = _transactions.Count(x => !string.IsNullOrWhiteSpace(x.ContractAddress));
            var totalTokens = _erc20TokenTransfers.Select(x => x.TokenSymbol).Distinct();

            var turnoverIntervalsDataList =
                _transactions.Select(x => new TurnoverIntervalsData(
                    x.TimeStamp!.ToDateTime(),
                    BigInteger.TryParse(x.Value, out var value) ? value : 0,
                    x.From?.Equals(_address, StringComparison.InvariantCultureIgnoreCase) == true));
            var turnoverIntervals = IStatCalculator<CeloTransactionIntervalData>
                .GetTurnoverIntervals(turnoverIntervalsDataList, _transactions.Min(x => x.TimeStamp!.ToDateTime())).ToList();

            return new()
            {
                Balance = _balance.ToCelo(),
                BalanceUSD = _usdBalance,
                WalletAge = IStatCalculator
                    .GetWalletAge(_transactions.Select(x => x.TimeStamp!.ToDateTime())),
                TotalTransactions = _transactions.Count(),
                TotalRejectedTransactions = _transactions.Count(t => t.IsError == "1"),
                MinTransactionTime = intervals.Min(),
                MaxTransactionTime = intervals.Max(),
                AverageTransactionTime = intervals.Average(),
                WalletTurnover = _transactions.Sum(x => decimal.TryParse(x.Value, out decimal value) ? value : 0).ToCelo(),
                BalanceChangeInLastMonth = IStatCalculator<CeloTransactionIntervalData>.GetBalanceChangeInLastMonth(turnoverIntervals),
                BalanceChangeInLastYear = IStatCalculator<CeloTransactionIntervalData>.GetBalanceChangeInLastYear(turnoverIntervals),
                TurnoverIntervals = turnoverIntervals,
                LastMonthTransactions = _transactions.Count(x => x.TimeStamp!.ToDateTime() > monthAgo),
                LastYearTransactions = _transactions.Count(x => x.TimeStamp!.ToDateTime() > yearAgo),
                TimeFromLastTransaction = (int)((DateTime.UtcNow - _transactions.OrderBy(x => x.TimeStamp).Last().TimeStamp!.ToDateTime()).TotalDays / 30),
                NftHolding = holdingTokens,
                NftTrading = (soldSum - buySum).ToCelo(),
                NftWorth = nftWorth.ToCelo(),
                DeployedContracts = contractsCreated,
                TokensHolding = totalTokens.Count()
            };
        }

        public CeloWalletEcoStats GetEcoStats(string tokenName, decimal multiplier)
        {
            if (!_erc20TokenTransfers.Any())
            {
                return new()
                {
                    NoData = true
                };
            }

            var intervals = IStatCalculator
                .GetTransactionsIntervals(_erc20TokenTransfers.Select(x => x.TimeStamp!.ToDateTime())).ToList();
            if (intervals.Count == 0)
            {
                return new()
                {
                    NoData = true
                };
            }

            var monthAgo = DateTime.Now.AddMonths(-1);
            var yearAgo = DateTime.Now.AddYears(-1);

            var turnoverIntervalsDataList =
                _erc20TokenTransfers.Select(x => new TurnoverIntervalsData(
                    x.TimeStamp!.ToDateTime(),
                    BigInteger.TryParse(x.Value, out var value) ? value : 0,
                    x.From?.Equals(_address, StringComparison.InvariantCultureIgnoreCase) == true));
            var turnoverIntervals = IStatCalculator<CeloTransactionIntervalData>
                .GetTurnoverIntervals(turnoverIntervalsDataList, _erc20TokenTransfers.Min(x => x.TimeStamp!.ToDateTime())).ToList();

            return new()
            {
                EcoToken = tokenName,
                Balance = _balance.ToTokenValue(multiplier),
                BalanceUSD = _usdBalance,
                WalletAge = IStatCalculator
                    .GetWalletAge(_erc20TokenTransfers.Select(x => x.TimeStamp!.ToDateTime())),
                TotalTransactions = _erc20TokenTransfers.Count(),
                TotalRejectedTransactions = 0,
                MinTransactionTime = intervals.Min(),
                MaxTransactionTime = intervals.Max(),
                AverageTransactionTime = intervals.Average(),
                WalletTurnover = _erc20TokenTransfers.Sum(x => decimal.TryParse(x.Value, out decimal value) ? value : 0).ToTokenValue(multiplier),
                BalanceChangeInLastMonth = IStatCalculator<CeloTransactionIntervalData>.GetBalanceChangeInLastMonth(turnoverIntervals),
                BalanceChangeInLastYear = IStatCalculator<CeloTransactionIntervalData>.GetBalanceChangeInLastYear(turnoverIntervals),
                TurnoverIntervals = turnoverIntervals,
                LastMonthTransactions = _erc20TokenTransfers.Count(x => x.TimeStamp!.ToDateTime() > monthAgo),
                LastYearTransactions = _erc20TokenTransfers.Count(x => x.TimeStamp!.ToDateTime() > yearAgo),
                TimeFromLastTransaction = (int)((DateTime.UtcNow - _erc20TokenTransfers.OrderBy(x => x.TimeStamp).Last().TimeStamp!.ToDateTime()).TotalDays / 30)
            };
        }
    }
}