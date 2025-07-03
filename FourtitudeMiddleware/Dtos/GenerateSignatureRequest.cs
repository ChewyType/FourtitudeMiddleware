namespace FourtitudeMiddleware.Dtos
{
    public class GenerateSignatureRequest
    {
        public Dictionary<string, string> Parameters { get; set; }
        public string Timestamp { get; set; } 
    }
}
