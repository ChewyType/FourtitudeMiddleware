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
        public ActionResult<SubmitTransactionResponse> SubmitTransaction(SubmitTransactionRequest request)
        {
            var response = _transactionService.ProcessTransaction(request);
            return HandleResponse<SubmitTransactionResponse>(response);
        }
    }
}
