// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Anonymizer.CommandLineTool
{
    public class AnonymizerCliTool
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                await ExecuteCommandsAsync(args);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Process failed: {ex.Message}");
                return -1;
            }
        }

        public static async Task ExecuteCommandsAsync(string[] args)
        {
            var inputFileOption = new Option<string>(
                new[] { "-i", "--inputFile" },
                "Input DICOM file");

            var outputFileOption = new Option<string>(
                new[] { "-o", "--outputFile" },
                "Output DICOM file");

            var configFileOption = new Option<string>(
                new[] { "-c", "--configFile" },
                () => "configuration.json",
                "Anonymization configuration file path.");

            var inputFolderOption = new Option<string>(
                new[] { "-I", "--inputFolder" },
                "Input folder");

            var outputFolderOption = new Option<string>(
                new[] { "-O", "--outputFolder" },
                "Output folder");

            var validateInputOption = new Option<bool>(
                "--validateInput",
                "Validate input DICOM data items.");

            var validateOutputOption = new Option<bool>(
                "--validateOutput",
                "Validate output DICOM data items.");

            var rootCommand = new RootCommand("DICOM Data Anonymization Tool");
            rootCommand.AddOption(inputFileOption);
            rootCommand.AddOption(outputFileOption);
            rootCommand.AddOption(configFileOption);
            rootCommand.AddOption(inputFolderOption);
            rootCommand.AddOption(outputFolderOption);
            rootCommand.AddOption(validateInputOption);
            rootCommand.AddOption(validateOutputOption);

            Exception thrownException = null;

            rootCommand.SetHandler(async (context) =>
            {
                try
                {
                    var inputFile = context.ParseResult.GetValueForOption(inputFileOption);
                    var outputFile = context.ParseResult.GetValueForOption(outputFileOption);
                    var configFile = context.ParseResult.GetValueForOption(configFileOption);
                    var inputFolder = context.ParseResult.GetValueForOption(inputFolderOption);
                    var outputFolder = context.ParseResult.GetValueForOption(outputFolderOption);
                    var validateInput = context.ParseResult.GetValueForOption(validateInputOption);
                    var validateOutput = context.ParseResult.GetValueForOption(validateOutputOption);

                    // Validate command-line argument combinations
                    bool hasInputFile = !string.IsNullOrEmpty(inputFile);
                    bool hasOutputFile = !string.IsNullOrEmpty(outputFile);
                    bool hasInputFolder = !string.IsNullOrEmpty(inputFolder);
                    bool hasOutputFolder = !string.IsNullOrEmpty(outputFolder);

                    // Check for invalid combinations
                    if ((hasInputFile && !hasOutputFile) ||
                        (!hasInputFile && hasOutputFile) ||
                        (hasInputFolder && !hasOutputFolder) ||
                        (!hasInputFolder && hasOutputFolder) ||
                        (hasInputFile && hasInputFolder) ||
                        (hasOutputFile && hasOutputFolder))
                    {
                        throw new ArgumentException("Invalid parameters. Please specify inputFile (or inputFolder) and outputFile (or outputFolder) at the same time.\r\nSamples:\r\n [-i inputFile -o outputFile]\r\nor\r\n [-I inputFolder -O outputFolder]");
                    }

                    if (!hasInputFile && !hasInputFolder)
                    {
                        throw new ArgumentException("Invalid parameters. Please specify inputFile (or inputFolder) and outputFile (or outputFolder) at the same time.\r\nSamples:\r\n [-i inputFile -o outputFile]\r\nor\r\n [-I inputFolder -O outputFolder]");
                    }

                    var options = new AnonymizerOptions
                    {
                        InputFile = inputFile,
                        OutputFile = outputFile,
                        ConfigurationFilePath = configFile,
                        InputFolder = inputFolder,
                        OutputFolder = outputFolder,
                        ValidateInput = validateInput,
                        ValidateOutput = validateOutput,
                    };

                    await AnonymizerLogic.AnonymizeAsync(options).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    thrownException = ex;
                    throw;
                }
            });

            var result = await rootCommand.InvokeAsync(args);

            // For test compatibility, re-throw the exception
            if (thrownException != null)
            {
                throw thrownException;
            }
        }
    }
}
