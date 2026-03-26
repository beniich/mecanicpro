using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace MecaPro.API.Endpoints.Modules;

public class AiEngineEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/ai-agent").RequireAuthorization().WithTags("Artificial Intelligence");
        var config = app.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var aiAgentUrl = config["AiAgent:Url"] ?? "http://localhost:3001";

        // We act as a proxy between the Blazor Client and the local Node.js Sub-Agent
        grp.MapPost("/stream", async (HttpContext ctx, IHttpClientFactory clientFactory) =>
        {
            var requestBody = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
            var client = clientFactory.CreateClient();

            // Typically the Node.js sub-agent runs on port 3001 or 3002
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{aiAgentUrl}/api/invoke")
            {
                Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            ctx.Response.StatusCode = (int)response.StatusCode;
            ctx.Response.ContentType = "text/event-stream";

            var stream = await response.Content.ReadAsStreamAsync();
            await stream.CopyToAsync(ctx.Response.Body);
        });
        
        // Vision endpoint for damage assessment (PhotoExpertiseIA)
        grp.MapPost("/vision", async (HttpContext ctx, IHttpClientFactory clientFactory) => 
        {
            var body = await ctx.Request.ReadFromJsonAsync<JsonElement>();
            var imageUrl = body.TryGetProperty("imageUrl", out var imgUrl) ? imgUrl.GetString() : null;
            var base64Image = body.TryGetProperty("base64Image", out var b64) ? b64.GetString() : null;
            var partType = body.GetProperty("partType").GetString();

            var client = clientFactory.CreateClient();
            var payload = new { 
                message = $"Analyse cette image de {partType} pour détecter des dommages.", 
                sessionId = $"vision_{Guid.NewGuid():N}",
                context = new { imageUrl, base64Image, partType, tool = "analyze_part_image" } 
            };
            
            // Invoke the agent via the sync endpoint to get JSON directly
            var response = await client.PostAsJsonAsync($"{aiAgentUrl}/api/invoke-sync", payload);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                // The agent returns { output: "JSON_STRING", success: true }
                // We need to parse the JSON_STRING inside output if it's JSON
                var outputStr = result.GetProperty("output").GetString();
                try {
                    return Results.Content(outputStr, "application/json");
                } catch {
                    return Results.Ok(new { rawOutput = outputStr });
                }
            }
            return Results.BadRequest("AI Agent unavailable");
        });
    }
}
