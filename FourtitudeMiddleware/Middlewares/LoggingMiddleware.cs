using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FourtitudeMiddleware.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log Request
            context.Request.EnableBuffering();
            var requestBody = await ReadStreamAsync(context.Request.Body);
            context.Request.Body.Position = 0;
            var maskedRequestBody = MaskSensitiveFields(requestBody);
            log.Info($"HTTP Request: {context.Request.Method} {context.Request.Path} Body: {maskedRequestBody}");

            // Log Response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var maskedResponseBody = MaskSensitiveFields(responseText);
            log.Info($"HTTP Response: {context.Request.Method} {context.Request.Path} Status: {context.Response.StatusCode} Body: {maskedResponseBody}");

            await responseBody.CopyToAsync(originalBodyStream);
        }

        private async Task<string> ReadStreamAsync(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }

        // Mask or encrypt any 'password' fields in JSON
        private string MaskSensitiveFields(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
                return json;
            try
            {
                var node = JsonNode.Parse(json);
                MaskNode(node);
                return node.ToJsonString();
            }
            catch
            {
                return json; // If not JSON, return as is
            }
        }

        private void MaskNode(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                foreach (var property in obj.ToList())
                {
                    var key = property.Key;
                    if (key.ToLower().Contains("password"))
                    {
                        obj[key] = EncryptForLog(property.Value?.ToString() ?? "");
                    }
                    else if (property.Value is JsonObject || property.Value is JsonArray)
                    {
                        MaskNode(property.Value);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    MaskNode(item);
                }
            }
        }

        // Simple encryption for logging (for demonstration, use a real encryption in production)
        private string EncryptForLog(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainBytes.Reverse().ToArray()); // Simple reverse + base64
        }
    }

    public static class LoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoggingMiddleware>();
        }
    }
} 