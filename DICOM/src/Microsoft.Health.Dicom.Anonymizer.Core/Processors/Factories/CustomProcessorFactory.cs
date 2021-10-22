// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;
using Microsoft.Health.Dicom.Anonymizer.Core.Exceptions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Dicom.Anonymizer.Core.Processors
{
    public class CustomProcessorFactory : DicomProcessorFactory
    {
        private readonly Dictionary<string, Type> _customProcessors = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase) { };

        public override IAnonymizerProcessor CreateProcessor(string method, JObject settingObject = null)
        {
            EnsureArg.IsNotNullOrEmpty(method, nameof(method));

            if (Constants.BuiltInMethods.Contains(method))
            {
                return this.CreateProcessor(method, settingObject);
            }

            return CreateCustomProcessor(method, settingObject);
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
                    throw new AddCustomProcessorException(DicomAnonymizationErrorCode.AddCustomProcessorFailed, $"Anonymization method {method} is a built-in method. Please add custom processor with unique method name.");
                }

                _customProcessors.Add(method, processor);
            }
        }

        private IAnonymizerProcessor CreateCustomProcessor(string method, JObject settingObject = null)
        {
            EnsureArg.IsNotNullOrEmpty(method, nameof(method));

            if (_customProcessors.ContainsKey(method))
            {
                return (IAnonymizerProcessor)Activator.CreateInstance(
                   _customProcessors[method],
                   new object[] { settingObject });
            }

            return null;
        }

        private string GetMethodName(string processor)
        {
            return processor.Split(".").Last().Replace("Processor", string.Empty);
        }
    }
}
