using Amazon.Lambda.Core;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public string FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogLine($"Received: {input}");

        string? issueUrl;
        try
        {
            var json = JObject.Parse(input.ToString()!);
            issueUrl = json["issue"]?["html_url"]?.ToString();
        }
        catch (JsonReaderException)
        {
            return "ignored: not a JSON object";
        }

        // GitHub's "ping" event and anything else without an issue ends up here
        if (string.IsNullOrEmpty(issueUrl))
            return "ignored: no issue in payload";

        var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
        if (string.IsNullOrEmpty(slackUrl))
            return "error: SLACK_URL not configured";

        string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, slackUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        using var response = client.Send(request);
        using var reader = new StreamReader(response.Content.ReadAsStream());
        
        return reader.ReadToEnd();
    }
}
