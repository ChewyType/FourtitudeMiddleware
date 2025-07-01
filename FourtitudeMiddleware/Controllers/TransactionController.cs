using Microsoft.AspNetCore.Mvc;
using FourtitudeMiddleware.Services;
using System.Text.Json;
using FourtitudeMiddleware.Dtos;

namespace FourtitudeMiddleware.Controllers
{
    [ApiController]
    [Route("api")]
    public class TransactionController : BaseController
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(
            ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("submittrxmessage")]
        public ActionResult<ServiceResponse<SubmitTransactionResponse>> SubmitTransaction(SubmitTransactionRequest request)
        {
            var serviceResponse = _transactionService.ProcessTransaction(request);
            return HandleServiceResponse(serviceResponse);
        }
    }
}
