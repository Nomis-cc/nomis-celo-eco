// ------------------------------------------------------------------------------------------------------
// <copyright file="WalletStatsRequest.cs" company="Nomis">
// Copyright (c) Nomis, 2023. All rights reserved.
// The Application under the MIT license. See LICENSE file in the solution root for full license information.
// </copyright>
// ------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Nomis.SoulboundTokenService.Interfaces.Enums;

namespace Nomis.Blockchain.Abstractions.Requests
{
    /// <summary>
    /// Request for getting the wallet stats.
    /// </summary>
    public sealed class WalletStatsRequest
    {
        private string _address = string.Empty;
        private string? _tokenAddress;

        /// <summary>
        /// Wallet address.
        /// </summary>
        [FromRoute(Name = "address")]
        public string Address
        {
            get
            {
                return _address;
            }

            set
            {
                _address = value.Trim();
            }
        }

        /// <summary>
        /// Contract nonce.
        /// </summary>
        /// <remarks>
        /// Should be get from the contract.
        /// </remarks>
        /// <example>0</example>
        [FromQuery]
        public ulong Nonce { get; set; }

        /// <summary>
        /// Verifying deadline block.
        /// </summary>
        /// <example>133160867380732039</example>
        [FromQuery]
        public ulong Deadline { get; set; }

        /// <summary>
        /// Score type.
        /// </summary>
        /// <example>0</example>
        [FromQuery]
        public ScoreType ScoreType { get; set; } = ScoreType.Finance;

        /// <summary>
        /// Token contract address.
        /// </summary>
        /// <remarks>
        /// If set, scoring calculate for this token.
        /// </remarks>
        /// <example>null</example>
        [FromQuery]
        public string? TokenAddress
        {
            get
            {
                return _tokenAddress;
            }

            set
            {
                _tokenAddress = value?.Trim();
            }
        }
    }
}