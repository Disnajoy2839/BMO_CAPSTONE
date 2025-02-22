using System;
using System.IO;
using System.Text;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace BOMLink.Services
{
    /// <summary>
    /// Service for integrating with Microsoft Azure's OCR (Computer Vision) API.
    /// Handles text extraction from images and PDF documents.
    /// </summary>
    public class AzureOCRService
    {
        private readonly string _endpoint; // Azure Computer Vision API Endpoint
        private readonly string _apiKey; // API Key for authentication
        private readonly ComputerVisionClient _client; // Azure Computer Vision Client

        /// <summary>
        /// Initializes the AzureOCRService with API credentials from configuration.
        /// </summary>
        /// <param name="configuration">Application configuration to retrieve API credentials.</param>
        public AzureOCRService(IConfiguration configuration)
        {
            // Retrieve API credentials from appsettings.json
            _endpoint = configuration["AzureOCR:Endpoint"];
            _apiKey = configuration["AzureOCR:ApiKey"];

            // Initialize the Azure Computer Vision Client with API credentials
            _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(_apiKey))
            {
                Endpoint = _endpoint
            };
        }

        /// <summary>
        /// Extracts text from an image or PDF file using Azure OCR.
        /// </summary>
        /// <param name="imageStream">Input image/PDF as a stream.</param>
        /// <returns>Extracted text from the image as a string.</returns>
        public async Task<string> ExtractTextFromImageAsync(Stream imageStream)
        {
            try
            {
                // Step 1: Send the image stream to Azure OCR for text extraction
                var textHeaders = await _client.ReadInStreamAsync(imageStream);

                // Step 2: Retrieve the operation ID from the response header
                string operationLocation = textHeaders.OperationLocation;
                string operationId = operationLocation.Substring(operationLocation.Length - 36); // Extract the GUID

                ReadOperationResult results;

                // Step 3: Polling mechanism - Wait until OCR processing is complete
                do
                {
                    results = await _client.GetReadResultAsync(Guid.Parse(operationId));
                    Thread.Sleep(1000); // Wait for OCR to finish processing
                }
                while (results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted);

                // Step 4: Extract text from the OCR result
                StringBuilder extractedText = new StringBuilder();
                foreach (ReadResult page in results.AnalyzeResult.ReadResults)
                {
                    foreach (Line line in page.Lines)
                    {
                        extractedText.AppendLine(line.Text); // Append extracted text line by line
                    }
                }

                return extractedText.ToString();
            }
            catch (Exception ex)
            {
                // Return error message if OCR processing fails
                return $"Error: {ex.Message}";
            }
        }
    }
}
