using Hl7.Fhir.ElementModel;
using Microsoft.Health.Fhir.Anonymizer.Core.Models;
using Microsoft.Health.Fhir.Anonymizer.Core.Processors;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Microsoft.Health.Fhir.Anonymizer.Core.UnitTests
{
    internal class MaskProcessor : IAnonymizerProcessor
    {
        private int _maskedLength;

        public MaskProcessor(JObject setting)
        {
            _maskedLength = int.Parse(setting.GetValue("maskedLength", StringComparison.OrdinalIgnoreCase).ToString());
        }

        public ProcessResult Process(ElementNode node, ProcessContext context = null, Dictionary<string, object> settings = null)
        {
            
            var result = new ProcessResult();
            if (node.Value == null)
            {
                return result;
            }

            var mask = new string('*', this._maskedLength);
            node.Value = node.Value.ToString().Length > _maskedLength ? mask + node.Value.ToString()[this._maskedLength..] : mask;
            return result;
        }
    }
}