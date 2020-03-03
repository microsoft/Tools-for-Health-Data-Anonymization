﻿using Hl7.Fhir.ElementModel;

namespace Fhir.Anonymizer.Core.Processors
{
    public interface IAnonymizerProcessor
    {
        public void Process(ElementNode node);
    }
}
