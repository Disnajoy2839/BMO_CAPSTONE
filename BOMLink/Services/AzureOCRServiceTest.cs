using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;

namespace BOMLink.Services {
    public class AzureOCRServiceTest {
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly ComputerVisionClient _client;

        public AzureOCRServiceTest(IConfiguration configuration) {
            _endpoint = configuration["AzureOCR:Endpoint"];
            _apiKey = configuration["AzureOCR:ApiKey"];

            _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(_apiKey)) {
                Endpoint = _endpoint
            };
        }

        /// <summary>
        /// Extracts and formats tables from an image or PDF using Azure OCR.
        /// </summary>
        public async Task<List<List<string>>> ExtractTableFromImageAsync(Stream imageStream) {
            try {
                var textHeaders = await _client.ReadInStreamAsync(imageStream);
                string operationLocation = textHeaders.OperationLocation;
                string operationId = operationLocation.Substring(operationLocation.Length - 36);

                ReadOperationResult results;
                do {
                    results = await _client.GetReadResultAsync(Guid.Parse(operationId));
                    Thread.Sleep(1000);
                }
                while (results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted);

                // Step 1: Extract text lines with their coordinates
                List<(string Text, double X, double Y)> extractedLines = new List<(string, double, double)>();

                foreach (ReadResult page in results.AnalyzeResult.ReadResults) {
                    foreach (Line line in page.Lines) {
                        double yCoordinate = line.BoundingBox?[1] ?? 0.0;
                        double xCoordinate = line.BoundingBox?[0] ?? 0.0;
                        extractedLines.Add((line.Text, xCoordinate, yCoordinate));
                    }
                }

                // Step 2: Group by Y-coordinates (assume same row)
                var groupedLines = extractedLines
                    .GroupBy(x => Math.Round(x.Y, 1)) // Group by similar Y positions
                    .OrderBy(g => g.Key) // Sort by Y (top to bottom)
                    .Select(g => g.OrderBy(x => x.X) // Sort each row by X (left to right)
                                  .Select(x => x.Text)
                                  .ToList())
                    .ToList();

                return groupedLines;
            } catch (Exception ex) {
                return new List<List<string>> { new List<string> { $"Error: {ex.Message}" } };
            }
        }
    }
}