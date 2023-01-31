// ------------------------------------------------------------------------------------------------------
// <copyright file="Celoscan.cs" company="Nomis">
// Copyright (c) Nomis, 2023. All rights reserved.
// The Application under the MIT license. See LICENSE file in the solution root for full license information.
// </copyright>
// ------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Nomis.Celoscan.Extensions;
using Nomis.Celoscan.Interfaces;

namespace Nomis.Celoscan
{
    /// <summary>
    /// Celoscan service registrar.
    /// </summary>
    public sealed class Celoscan :
        ICeloServiceRegistrar
    {
        /// <inheritdoc/>
        public IServiceCollection RegisterService(
            IServiceCollection services)
        {
            return services
                .AddCeloscanService();
        }
    }
}