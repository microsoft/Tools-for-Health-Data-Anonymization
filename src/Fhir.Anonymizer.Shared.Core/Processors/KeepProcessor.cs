﻿using Fhir.Anonymizer.Core.Models;
using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Processors
{
    public class KeepProcessor: IAnonymizerProcessor
    {
        public ProcessResult Process(ElementNode node, ProcessSetting setting = null)
        {
            return new ProcessResult();
        }
    }
}
