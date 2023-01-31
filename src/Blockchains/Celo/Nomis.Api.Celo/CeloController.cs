// ------------------------------------------------------------------------------------------------------
// <copyright file="CeloController.cs" company="Nomis">
// Copyright (c) Nomis, 2023. All rights reserved.
// The Application under the MIT license. See LICENSE file in the solution root for full license information.
// </copyright>
// ------------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nomis.Api.Common.Swagger.Examples;
using Nomis.Blockchain.Abstractions.Requests;
using Nomis.Celoscan.Interfaces;
using Nomis.Celoscan.Interfaces.Models;
using Nomis.SoulboundTokenService.Interfaces.Enums;
using Nomis.Utils.Wrapper;
using Swashbuckle.AspNetCore.Annotations;

namespace Nomis.Api.Celo
{
    /// <summary>
    /// A controller to aggregate all Celo-related actions.
    /// </summary>
    [Route(BasePath)]
    [ApiVersion("1")]
    [SwaggerTag("Celo blockchain.")]
    public sealed class CeloController :
        ControllerBase
    {
        /// <summary>
        /// Base path for routing.
        /// </summary>
        internal const string BasePath = "api/v{version:apiVersion}/celo";

        /// <summary>
        /// Common tag for Celo actions.
        /// </summary>
        internal const string CeloTag = "Celo";

        private readonly ILogger<CeloController> _logger;
        private readonly ICeloScoringService _scoringService;

        /// <summary>
        /// Initialize <see cref="CeloController"/>.
        /// </summary>
        /// <param name="scoringService"><see cref="ICeloScoringService"/>.</param>
        /// <param name="logger"><see cref="ILogger{T}"/>.</param>
        public CeloController(
            ICeloScoringService scoringService,
            ILogger<CeloController> logger)
        {
            _scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get Nomis Score for given wallet address.
        /// </summary>
        /// <param name="request">Request for getting the wallet stats.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>An Nomis Score value and corresponding statistical data.</returns>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/celo/wallet/0x6e4E4B8D7de9190d66BE96ED746DD4C3a83ab51e/score?scoreType=1&amp;tokenAddress=0x9995cc8f20db5896943afc8ee0ba463259c931ed&amp;nonce=0&amp;deadline=133160867380732039
        ///
        /// <para>Eco tokens:</para>
        /// <para>0x27cd006548df7c8c8e9fdc4a67fa05c2e3ca5cf9 - PLASTIKS</para>
        /// <para>0x32A9FE697a32135BFd313a6Ac28792DaE4D9979d - MCO2</para>
        /// <para>0x9995cc8f20db5896943afc8ee0ba463259c931ed - ETHIX</para>
        /// </remarks>
        /// <response code="200">Returns Nomis Score and stats.</response>
        /// <response code="400">Address not valid.</response>
        /// <response code="404">No data found.</response>
        /// <response code="500">Unknown internal error.</response>
        [HttpGet("wallet/{address}/score", Name = "GetCeloWalletScore")]
        [AllowAnonymous]
        [SwaggerOperation(
            OperationId = "GetCeloWalletScore",
            Tags = new[] { CeloTag })]
        [ProducesResponseType(typeof(Result<CeloWalletScore>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResult<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(RateLimitResult), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ErrorResult<string>), StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> GetCeloWalletScoreAsync(
            [Required(ErrorMessage = "Request should be set")] WalletStatsRequest request,
            CancellationToken cancellationToken = default)
        {
            switch (request.ScoreType)
            {
                case ScoreType.Finance:
                    return Ok(await _scoringService.GetWalletStatsAsync<CeloWalletScore, CeloWalletStats, CeloTransactionIntervalData>(request, cancellationToken));
                case ScoreType.Eco:
                    return Ok(await _scoringService.GetWalletStatsAsync<CeloWalletEcoScore, CeloWalletEcoStats, CeloTransactionIntervalData>(request, cancellationToken));
                default:
                    throw new NotImplementedException();
            }
        }
    }
}