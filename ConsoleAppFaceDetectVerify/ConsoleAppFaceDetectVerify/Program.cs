using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleAppFaceDetectVerify
{
    class Program
    {
        private const string faceEndpoint =
            "https://<enter your location here>.api.cognitive.microsoft.com";

        private const string apiKey = "<enter your api key here>";

        private const string url1 = "http://www.example.com/ImageWithFace1.jpg";
        private const string url2 = "http://www.example.com/ImageWithFace2.jpg";

        private static readonly FaceAttributeType[] faceAttributes = { FaceAttributeType.Age,
                                                              FaceAttributeType.Gender,
                                                              FaceAttributeType.Emotion };

        static async Task Main()
        {
            // Init FaceClient
            var faceClient = new FaceClient(new ApiKeyServiceClientCredentials(apiKey),
                                         new System.Net.Http.DelegatingHandler[] { })
            {
                Endpoint = faceEndpoint
            };

            // Detect faces in URL1
            var faceResult1 = await faceClient.Face.DetectWithUrlAsync(url1,
                                    true,
                                    false,
                                    faceAttributes);

            // Detect faces in URL2
            var faceResult2 = await faceClient.Face.DetectWithUrlAsync(url2,
                                    true,
                                    false,
                                    faceAttributes);

            // Output results of Face Detect URL1
            Console.WriteLine("Detected FaceIDs in URL 1:");
            DisplayFaceInfo(faceResult1);

            // Output results of Face Detect URL2
            Console.WriteLine("Detected FaceIDs in URL 2:");
            DisplayFaceInfo(faceResult2);

            // Verify first face in each URL, if any and not null
            if (faceResult1.Any() && faceResult1.First().FaceId != null &&
                faceResult2.Any() && faceResult2.First().FaceId != null)
            {
                var verifyResult = await faceClient.Face.VerifyFaceToFaceAsync((Guid)faceResult1.First().FaceId, (Guid)faceResult2.First().FaceId);
                Console.WriteLine($"Verify faces call got: {verifyResult.IsIdentical} with confidence {verifyResult.Confidence}");
            }
        }

        private static void DisplayFaceInfo(IList<DetectedFace> faces)
        {
            foreach (var face in faces)
            {
                Console.WriteLine($"FaceID: {face.FaceId} Age: {face.FaceAttributes.Age} Gender: {face.FaceAttributes.Gender}");
            }
        }
    }
}
