// ------------------------------------------------------------------------------------------------------
// <copyright file="CeloAPISettings.cs" company="Nomis">
// Copyright (c) Nomis, 2023. All rights reserved.
// The Application under the MIT license. See LICENSE file in the solution root for full license information.
// </copyright>
// ------------------------------------------------------------------------------------------------------

using Nomis.Utils.Contracts.Common;

namespace Nomis.Api.Celo.Settings
{
    /// <summary>
    /// Celo API settings.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal class CeloAPISettings :
        IAPISettings
    {
        /// <inheritdoc/>
        public bool APIEnabled { get; set; }

        /// <inheritdoc/>
        public string APIName => CeloController.CeloTag;

        /// <inheritdoc/>
        public string ControllerName => nameof(CeloController);
    }
}