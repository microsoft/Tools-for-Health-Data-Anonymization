﻿using System.Collections.Generic;
using Fhir.Anonymizer.Core.Processors;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;

namespace Fhir.Anonymizer.Core.Models
{
    public class ProcessResult
    {
        public bool IsRedacted
        {
            get
            {
                return ProcessRecords.ContainsKey(AnonymizationOperations.Redact);
            }
        }

        public bool IsAbstracted
        {
            get
            {
                return ProcessRecords.ContainsKey(AnonymizationOperations.Abstract);
            }
        }

        public bool IsCryptoHashed
        {
            get
            {
                return ProcessRecords.ContainsKey(AnonymizationOperations.CryptoHash);
            }
        }

        public bool IsEncrypted
        {
            get
            {
                return ProcessRecords.ContainsKey(AnonymizationOperations.Encrypt);
            }
        }

        public bool IsPerturbed 
        { 
            get 
            {
                return ProcessRecords.ContainsKey(AnonymizationOperations.Perturb);
            }
        }

        public Dictionary<string, HashSet<ITypedElement>> ProcessRecords { get; } = new Dictionary<string, HashSet<ITypedElement>>();

        public void AddProcessRecord(string operationName, ITypedElement node)
        {
            if (ProcessRecords.ContainsKey(operationName))
            {
                ProcessRecords[operationName].Add(node);
            }
            else
            {
                ProcessRecords[operationName] = new HashSet<ITypedElement>() { node };
            }
        }

        public void Update(ProcessResult result)
        {
            if (result == null)
            {
                return;
            }

            foreach (var pair in result.ProcessRecords)
            {
                if (!ProcessRecords.ContainsKey(pair.Key))
                {
                    ProcessRecords[pair.Key] = pair.Value;
                }
                else
                {
                    ProcessRecords[pair.Key].UnionWith(pair.Value);
                }
            }
        }
    }
}
