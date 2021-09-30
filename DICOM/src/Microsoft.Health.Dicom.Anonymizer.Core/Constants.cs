// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;

namespace Microsoft.Health.Dicom.Anonymizer.Core
{
    internal static class Constants
    {
        internal const string TagKey = "tag";
        internal const string MethodKey = "method";
        internal const string Parameters = "params";
        internal const string RuleSetting = "setting";
        internal static readonly HashSet<string> BuiltInMethods = Enum.GetNames(typeof(AnonymizerMethod)).ToHashSet(StringComparer.InvariantCultureIgnoreCase);
    }
}
