// ------------------------------------------------------------------------------------------------------
// <copyright file="CeloExtensions.cs" company="Nomis">
// Copyright (c) Nomis, 2023. All rights reserved.
// The Application under the MIT license. See LICENSE file in the solution root for full license information.
// </copyright>
// ------------------------------------------------------------------------------------------------------

using Nomis.Api.Celo.Settings;
using Nomis.Api.Common.Extensions;
using Nomis.Celoscan.Interfaces;
using Nomis.ScoringService.Interfaces.Builder;

namespace Nomis.Api.Celo.Extensions
{
    /// <summary>
    /// Celo extension methods.
    /// </summary>
    public static class CeloExtensions
    {
        /// <summary>
        /// Add Celo blockchain.
        /// </summary>
        /// <typeparam name="TServiceRegistrar">The service registrar type.</typeparam>
        /// <param name="optionsBuilder"><see cref="IScoringOptionsBuilder"/>.</param>
        /// <returns>Returns <see cref="IScoringOptionsBuilder"/>.</returns>
        public static IScoringOptionsBuilder WithCeloBlockchain<TServiceRegistrar>(
            this IScoringOptionsBuilder optionsBuilder)
            where TServiceRegistrar : ICeloServiceRegistrar, new()
        {
            return optionsBuilder
                .With<CeloAPISettings, TServiceRegistrar>();
        }
    }
}