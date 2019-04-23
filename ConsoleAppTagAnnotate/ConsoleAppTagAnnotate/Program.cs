using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleAppTagAnnotate
{
    class Program
    {
        private const string computerVisionEndpoint =
            "https://<enter your endpoint location here>.api.cognitive.microsoft.com";

        private const string apiKey = "<enter your api key here>";

        private static List<VisualFeatureTypes> visualFeatures = new List<VisualFeatureTypes>();

        static ComputerVisionClient client;

        static void Init()
        {
            client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(apiKey))
            {
                Endpoint = computerVisionEndpoint
            };

            visualFeatures.Add(VisualFeatureTypes.Description);
            visualFeatures.Add(VisualFeatureTypes.Tags);
        }

        static async Task Main()
        {
            Init();
            await ScanFolderAsync("images");
        }

        private static async Task ScanFolderAsync(string folderName)
        {
            foreach (var filePath in Directory.EnumerateFiles(folderName,
                "*.jpg", SearchOption.TopDirectoryOnly)
            )
            {
                using (Stream imageFileStream = File.OpenRead(filePath))
                {
                    var result = await client.AnalyzeImageInStreamAsync(imageFileStream, visualFeatures);
                    ProcessResult(filePath, result);
                }
            }
        }

        private static void ProcessResult(string fileName, ImageAnalysis result)
        {
            Console.WriteLine($"Processed file: {fileName}");

            Console.WriteLine($"Description: {result.Description.Captions.FirstOrDefault()?.Text}");

            Console.Write("Tags:");
            foreach (var tag in result.Tags)
            {
                Console.Write($"[{tag.Name}] ");
            }
            Console.WriteLine("");
            Console.WriteLine("--------------------");
        }
    }
}
