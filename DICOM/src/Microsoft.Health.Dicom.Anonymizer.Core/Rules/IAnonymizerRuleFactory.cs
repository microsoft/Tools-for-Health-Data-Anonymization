// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Anonymizer.Core.Models;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Rules
{
    public interface IAnonymizerRuleFactory
    {
        AnonymizerRule CreateDicomAnonymizationRule(AnonymizerRuleModel rule);

        AnonymizerRule[] CreateDicomAnonymizationRules(AnonymizerRuleModel[] rule);
    }
}
