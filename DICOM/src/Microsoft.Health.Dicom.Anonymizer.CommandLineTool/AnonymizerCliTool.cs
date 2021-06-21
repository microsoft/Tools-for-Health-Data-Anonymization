// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Tool
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
            await Parser.Default.ParseArguments<AnonymizerOptions>(args)
               .MapResult(async options => await AnonymizerLogic.AnonymizeAsync(options).ConfigureAwait(false), _ => Task.FromResult(1)).ConfigureAwait(false);
        }
    }
}