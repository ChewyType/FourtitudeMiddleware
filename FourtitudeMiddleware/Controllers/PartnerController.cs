using Microsoft.AspNetCore.Mvc;
using FourtitudeMiddleware.Services;
using System;
using FourtitudeMiddleware.Dtos;

namespace FourtitudeMiddleware.Controllers
{
    [ApiController]
    [Route("api/partner")]
    public class PartnerController : ControllerBase
    {
        private readonly IPartnerService _partnerService;

        public PartnerController(IPartnerService partnerService)
        {
            _partnerService = partnerService;
        }

        // For testing signature validation
        [HttpPost("generatesignature")]
        public ActionResult<GenerateSignatureResponse> GenerateSignature([FromBody] GenerateSignatureRequest request)
        {
            if (request == null || request.Parameters == null)
                return BadRequest("Invalid request.");

            var response = _partnerService.GenerateSignature(request.Parameters);
            return Ok(response);
        }
    }
} 