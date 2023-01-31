﻿// ------------------------------------------------------------------------------------------------------
// <copyright file="IBlockchainScoringService.cs" company="Nomis">
// Copyright (c) Nomis, 2023. All rights reserved.
// The Application under the MIT license. See LICENSE file in the solution root for full license information.
// </copyright>
// ------------------------------------------------------------------------------------------------------

using Nomis.Blockchain.Abstractions.Models;
using Nomis.Blockchain.Abstractions.Requests;
using Nomis.Blockchain.Abstractions.Stats;
using Nomis.Utils.Wrapper;

namespace Nomis.Blockchain.Abstractions
{
    /// <summary>
    /// Blockchain scoring service.
    /// </summary>
    public interface IBlockchainScoringService
    {
        /// <summary>
        /// Get blockchain wallet stats by address.
        /// </summary>
        /// <typeparam name="TWalletScore">The wallet score type.</typeparam>
        /// <typeparam name="TWalletStats">The wallet stats type.</typeparam>
        /// <typeparam name="TTransactionIntervalData"><see cref="ITransactionIntervalData"/>.</typeparam>
        /// <param name="request">Request for getting the wallet stats.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>Returns the wallet score result.</returns>
        Task<Result<TWalletScore>> GetWalletStatsAsync<TWalletScore, TWalletStats, TTransactionIntervalData>(
            WalletStatsRequest request,
            CancellationToken cancellationToken = default)
            where TWalletScore : IWalletScore<TWalletStats, TTransactionIntervalData>, new()
            where TWalletStats : class, IWalletCommonStats<TTransactionIntervalData>, new()
            where TTransactionIntervalData : class, ITransactionIntervalData;
    }
}