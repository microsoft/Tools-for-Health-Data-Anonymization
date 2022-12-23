// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore;

namespace Microsoft.Health.DeIdentification.Web.App
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(Path.GetDirectoryName(typeof(Program).Assembly.Location))
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}