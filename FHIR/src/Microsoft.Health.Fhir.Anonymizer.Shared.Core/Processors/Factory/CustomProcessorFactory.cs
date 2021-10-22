// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Fhir.Anonymizer.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Processors
{
    public class CustomProcessorFactory :IAnonymizerProcessorFactory
    {
        private readonly Dictionary<string, Type> CustomProcessors = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase) { };

        public IAnonymizerProcessor CreateProcessor(string method, JObject settingObject = null)
        {
            EnsureArg.IsNotNullOrEmpty(method, nameof(method));

            if (CustomProcessors.ContainsKey(method))
            {
                return (IAnonymizerProcessor)Activator.CreateInstance(
                   CustomProcessors[method],
                   new object[] { settingObject });
            }

            return null;
        }


        public void AddProcessors(params Type[] processors)
        {
            if (processors != null)
            {
                AddProcessors(processors.AsEnumerable());
            }
        }

        public void AddProcessors(IEnumerable<Type> processors)
        {
            foreach (Type processor in processors)
            {
                var method = GetMethodName(processor.ToString());
                if (Constants.BuiltInMethods.Contains(method))
                {
                    throw new AddCustomProcessorException( $"Anonymization method {method} is a built-in method. Please add custom processor with unique method name.");
                }

                CustomProcessors.Add(method, processor);
            }
        }

        private string GetMethodName(string processor)
        {
            return processor.Split(".").Last().Replace("Processor", string.Empty);
        }
    }
}
