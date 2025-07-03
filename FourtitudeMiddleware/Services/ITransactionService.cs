using FourtitudeMiddleware.Dtos;

namespace FourtitudeMiddleware.Services
{
    public interface ITransactionService
    {
        SubmitTransactionResponse ProcessTransaction(SubmitTransactionRequest request);
    }
} 