using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

namespace MecaPro.Blazor.Services;

public class AiStreamService(HttpClient http, AuthStateProvider authState)
{
    public async Task StreamAiAnalyseAsync(string message, Action<AiEventDto> onEvent)
    {
        var request = new { message, sessionId = Guid.NewGuid().ToString() };
        
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ai-agent/stream")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        // We need to read the response as a stream
        using var response = await http.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
        
        if (!response.IsSuccessStatusCode)
        {
            onEvent(new AiEventDto("error", new { message = "Impossible de contacter l'agent IA." }));
            return;
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6);
                try {
                    var ev = JsonSerializer.Deserialize<AiEventDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (ev != null) onEvent(ev);
                } catch { /* parse error */ }
            }
        }
    }
}

public record AiEventDto(string Event, object Data);
