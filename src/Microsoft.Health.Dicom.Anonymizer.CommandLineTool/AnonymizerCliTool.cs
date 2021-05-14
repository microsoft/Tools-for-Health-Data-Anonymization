// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Tool
{
    public class AnonymizerCliTool
    {
        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<AnonymizerOptions>(args)
               .MapResult(async options => await AnonymizerLogic.AnonymizeAsync(options).ConfigureAwait(false), _ => Task.FromResult(1)).ConfigureAwait(false);
        }
    }
}