using FourtitudeMiddleware.Dtos;

namespace FourtitudeMiddleware.Services
{
    public interface ITransactionService
    {
        ServiceResponse<SubmitTransactionResponse> ProcessTransaction(SubmitTransactionRequest request);
    }
} 