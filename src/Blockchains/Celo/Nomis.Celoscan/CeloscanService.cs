// ------------------------------------------------------------------------------------------------------
// <copyright file="CeloscanService.cs" company="Nomis">
// Copyright (c) Nomis, 2023. All rights reserved.
// The Application under the MIT license. See LICENSE file in the solution root for full license information.
// </copyright>
// ------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Options;
using Nethereum.Util;
using Nomis.Blockchain.Abstractions;
using Nomis.Blockchain.Abstractions.Contracts;
using Nomis.Blockchain.Abstractions.Extensions;
using Nomis.Blockchain.Abstractions.Models;
using Nomis.Blockchain.Abstractions.Requests;
using Nomis.Blockchain.Abstractions.Stats;
using Nomis.Celoscan.Calculators;
using Nomis.Celoscan.Interfaces;
using Nomis.Celoscan.Interfaces.Extensions;
using Nomis.Celoscan.Interfaces.Models;
using Nomis.Celoscan.Settings;
using Nomis.Coingecko.Interfaces;
using Nomis.Domain.Scoring.Entities;
using Nomis.ScoringService.Interfaces;
using Nomis.SoulboundTokenService.Interfaces;
using Nomis.SoulboundTokenService.Interfaces.Enums;
using Nomis.Utils.Contracts.Services;
using Nomis.Utils.Exceptions;
using Nomis.Utils.Wrapper;

namespace Nomis.Celoscan
{
    /// <inheritdoc cref="ICeloScoringService"/>
    internal sealed class CeloscanService :
        BlockchainDescriptor,
        ICeloScoringService,
        ITransientService
    {
        private readonly ICeloscanClient _client;
        private readonly ICoingeckoService _coingeckoService;
        private readonly IScoringService _scoringService;
        private readonly IEvmSoulboundTokenService _soulboundTokenService;

        /// <summary>
        /// Initialize <see cref="CeloscanService"/>.
        /// </summary>
        /// <param name="settings"><see cref="CeloscanSettings"/>.</param>
        /// <param name="client"><see cref="ICeloscanClient"/>.</param>
        /// <param name="coingeckoService"><see cref="ICoingeckoService"/>.</param>
        /// <param name="scoringService"><see cref="IScoringService"/>.</param>
        /// <param name="soulboundTokenService"><see cref="IEvmSoulboundTokenService"/>.</param>
        public CeloscanService(
            IOptions<CeloscanSettings> settings,
            ICeloscanClient client,
            ICoingeckoService coingeckoService,
            IScoringService scoringService,
            IEvmSoulboundTokenService soulboundTokenService)
            : base(settings.Value.BlockchainDescriptor)
        {
            _client = client;
            _coingeckoService = coingeckoService;
            _scoringService = scoringService;
            _soulboundTokenService = soulboundTokenService;
        }

        /// <inheritdoc/>
        public async Task<Result<TWalletScore>> GetWalletStatsAsync<TWalletScore, TWalletStats, TTransactionIntervalData>(
            WalletStatsRequest request,
            CancellationToken cancellationToken = default)
            where TWalletScore : IWalletScore<TWalletStats, TTransactionIntervalData>, new()
            where TWalletStats : class, IWalletCommonStats<TTransactionIntervalData>, new()
            where TTransactionIntervalData : class, ITransactionIntervalData
        {
            if (!new AddressUtil().IsValidAddressLength(request.Address) || !new AddressUtil().IsValidEthereumAddressHexFormat(request.Address))
            {
                throw new CustomException("Invalid address", statusCode: HttpStatusCode.BadRequest);
            }

            string tokenName = "celo";
            string? balanceWei;
            decimal usdBalance = 0;
            decimal multiplier = 1;
            var transactions = new List<CeloscanAccountNormalTransaction>();
            var internalTransactions = new List<CeloscanAccountInternalTransaction>();
            var tokenTransfers = new List<ICeloscanAccountNftTokenEvent>();
            var erc20Tokens = (await _client.GetTransactionsAsync<CeloscanAccountERC20TokenEvents, CeloscanAccountERC20TokenEvent>(request.Address)).ToList();
            await Task.Delay(100, cancellationToken);
            switch (request.ScoreType)
            {
                case ScoreType.Finance:
                    balanceWei = (await _client.GetBalanceAsync(request.Address)).Balance;
                    usdBalance = await _coingeckoService.GetUsdBalanceAsync(balanceWei?.ToCelo() ?? 0, "celo");
                    transactions = (await _client.GetTransactionsAsync<CeloscanAccountNormalTransactions, CeloscanAccountNormalTransaction>(request.Address)).ToList();
                    internalTransactions = (await _client.GetTransactionsAsync<CeloscanAccountInternalTransactions, CeloscanAccountInternalTransaction>(request.Address)).ToList();
                    tokenTransfers = (await _client.GetTransactionsAsync<CeloscanAccountERC721TokenEvents, CeloscanAccountERC721TokenEvent>(request.Address)).Cast<ICeloscanAccountNftTokenEvent>().ToList();
                    break;
                case ScoreType.Eco:
                default:
                    if (string.IsNullOrWhiteSpace(request.TokenAddress))
                    {
                        throw new CustomException("Token contract address should be set", statusCode: HttpStatusCode.BadRequest);
                    }

                    erc20Tokens = erc20Tokens.Where(t =>
                        t.ContractAddress?.Equals(request.TokenAddress, StringComparison.InvariantCultureIgnoreCase) == true).ToList();

                    balanceWei = (await _client.GetTokenBalanceAsync(request.Address, request.TokenAddress)).Balance;
                    decimal.TryParse(balanceWei, NumberStyles.AllowDecimalPoint, new NumberFormatInfo { CurrencyDecimalSeparator = "." }, out decimal balance);
                    var tokenData = await _coingeckoService.GetTokenDataAsync("celo", request.TokenAddress);
                    if (tokenData != null && tokenData.DetailPlatforms.ContainsKey("celo") && !string.IsNullOrWhiteSpace(tokenData.Id))
                    {
                        tokenName = tokenData.Name ?? string.Empty;
                        int decimals = tokenData.DetailPlatforms["celo"].DecimalPlace;
                        multiplier = 1;
                        for (int i = 0; i < decimals; i++)
                        {
                            multiplier /= 10;
                        }

                        usdBalance =
                            await _coingeckoService.GetUsdBalanceAsync(balance.ToTokenValue(multiplier), tokenData.Id);
                    }
                    else
                    {
                        tokenName = "unknown";
                    }

                    break;
            }

            var calculator = new CeloStatCalculator(
                request.Address,
                decimal.TryParse(balanceWei, out decimal wei) ? wei : 0,
                usdBalance,
                transactions,
                internalTransactions,
                tokenTransfers,
                erc20Tokens);
            TWalletStats? walletStats = new();
            switch (request.ScoreType)
            {
                case ScoreType.Finance:
                    walletStats = calculator.GetStats() as TWalletStats;
                    break;
                case ScoreType.Eco:
                    walletStats = calculator.GetEcoStats(tokenName, multiplier) as TWalletStats;
                    break;
            }

            double score = walletStats!.GetScore<TWalletStats, TTransactionIntervalData>();
            var scoringData = new ScoringData(request.Address, request.Address, ChainId, score, JsonSerializer.Serialize(walletStats));
            await _scoringService.SaveScoringDataToDatabaseAsync(scoringData, cancellationToken);

            // getting signature
            ushort mintedScore = (ushort)(score * 10000);
            var signatureResult = _soulboundTokenService.GetSoulboundTokenSignature(new()
            {
                Score = mintedScore,
                ScoreType = request.ScoreType,
                To = request.Address,
                Nonce = request.Nonce,
                ChainId = ChainId,
                ContractAddress = SBTContractAddresses?.ContainsKey(request.ScoreType) == true ? SBTContractAddresses?[request.ScoreType] : null,
                Deadline = request.Deadline
            });

            var messages = signatureResult.Messages;
            messages.Add($"Got {ChainName} wallet {request.ScoreType.ToString()} score.");
            return await Result<TWalletScore>.SuccessAsync(
                new()
                {
                    Address = request.Address,
                    Stats = walletStats,
                    Score = score,
                    MintedScore = mintedScore,
                    Signature = signatureResult.Data.Signature
                }, messages);
        }
    }
}