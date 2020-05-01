﻿using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using System;
using System.Net;
using System.Threading.Tasks;
using Vs.Rules.Core;
using Vs.Rules.OpenApi.v1.Dto;
using ParseResult = Vs.Rules.OpenApi.v1.Dto.ParseResult;

namespace Vs.Rules.OpenApi.v1.Features.discipl.Controllers
{
    /// <summary>
    /// Rules API integrates the rule engine and exposes it as OAS3.
    /// Uses best practices from: https://github.com/RicoSuter/NSwag/wiki/AspNetCoreOpenApiDocumentGenerator
    /// </summary>
    /// <seealso cref="ControllerBase" />
    [ApiVersion("1.0-discipl")]
    [Route("api/v{version:apiVersion}/rules")]
    [OpenApiTag("Rules Engine", Description = "This is current api with feature 1 implementation")]
    [ApiController]
    public class RulesControllerDiscipl : ControllerBase
    {
        /// <summary>
        /// Generates the content template for a given yaml rule file.
        /// </summary>
        /// <param name="yaml"></param>
        /// <returns>The content template in yaml format</returns>
        /// <response code="200">Yaml content Template Parsed</response>
        /// <response code="400">Yaml rule set contains errors</response>
        /// <response code="404">Yaml rule set could not be found</response>
        /// <response code="500">Server error</response>
        [ProducesResponseType(typeof(ParseResult), 400)]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 500)]
        [HttpPost("generate-content-template")]
        public async Task<IActionResult> GenerateContentTemplate(Uri url)
        {
            try
            {
                YamlScriptController controller = new YamlScriptController();
                string yaml = null;
                using (var client = new WebClient())
                {
                    try
                    {
                        yaml = client.DownloadString(url);
                    }
                    catch (WebException ex)
                    {
                        return StatusCode(404, ex.Message);
                    }
                }

                var result = controller.Parse(yaml);
                ParseResult parseResult = new ParseResult()
                {
                    IsError = result.IsError,
                    Message = result.Message
                };
                if (parseResult.IsError)
                    return StatusCode(400, parseResult);

                return StatusCode(200, controller.CreateYamlContentTemplate());
            }
            catch (Exception exception)
            {
                return StatusCode(500, exception.Message);
            }
        }

        /// <summary>
        /// For debugging purposes you can parse the yaml rule without executing it.
        /// </summary>
        /// <param name="yaml">The url pointing to the rule yaml</param>
        /// <returns>ParesResult</returns>
        /// <response code="200">Parsed</response>
        /// <response code="404">Yaml rule set could not be found</response>
        /// <response code="500">Server error</response>
        [HttpPost("validate-rule")]
        [ProducesResponseType(typeof(v1.Dto.ParseResult), 200)]
        [ProducesResponseType(typeof(ConfigurationInvalidResponse), 404)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> ValidateRuleYaml(Uri url)
        {
            try
            {
                YamlScriptController controller = new YamlScriptController();
                string yaml = null;
                using (var client = new WebClient())
                {
                    try
                    {
                        yaml = client.DownloadString(url);
                    }
                    catch (WebException ex)
                    {
                        return StatusCode(404, ex.Message);
                    }
                }

                var result = controller.Parse(yaml);
                ParseResult parseResult = new ParseResult()
                {
                    IsError = result.IsError,
                    Message = result.Message
                };
                return StatusCode(200, parseResult);
            }
            catch (Exception exception)
            {
                return StatusCode(500, exception.Message);
            }
        }
    }
}