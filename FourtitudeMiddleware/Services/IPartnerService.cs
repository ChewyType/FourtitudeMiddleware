namespace FourtitudeMiddleware.Services
{
    public interface IPartnerService
    {
        bool ValidatePartner(string partnerKey, string password);
        bool ValidateSignature(Dictionary<string, string> parameters, string timestamp, string signature);
    }
}
