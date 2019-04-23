using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;

namespace ContentModeratorFunction
{
    public static class ScreenText
    {
        /// <summary>
        /// The base URL fragment for Content Moderator calls.
        /// </summary>
        private static readonly string AzureBaseURL =
            "https://<your location here>.api.cognitive.microsoft.com";

        /// <summary>
        /// Your Content Moderator subscription key.
        /// </summary>
        private static readonly string ApiKey = "<your api key here>";

        /// <summary>
        /// Convert string to Stream object
        /// </summary>
        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        [FunctionName("ScreenText")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"Screening text: '{requestBody}' using term list with ID 557.");

            var client = NewClient();
            var screen = client.TextModeration.ScreenText(textContentType: "text/plain", textContent: GenerateStreamFromString(requestBody), listId: "557");
            var result = string.Empty;

            if (null == screen.Terms)
            {
                result = "No terms from the term list were detected in the text.";
            }
            else
            {
                foreach (DetectedTerms term in screen.Terms)
                {
                    result += $"Found term: \"{term.Term}\" from list ID {term.ListId} at index {term.Index}.\n";
                }
                
            }
            log.LogInformation(result);

            return (ActionResult)new OkObjectResult(result);
        }

        /// <summary>
        /// Returns a new Content Moderator client for your subscription.
        /// </summary>
        public static ContentModeratorClient NewClient()
        {
            // Create and initialize an instance of the Content Moderator API wrapper.
            return  new ContentModeratorClient(new ApiKeyServiceClientCredentials(ApiKey))
            {
                Endpoint = AzureBaseURL
            };
        }
    }
}
