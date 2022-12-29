// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.DeIdentification.Contract;

namespace Microsoft.Health.DeIdentification.Web
{
    public class DeIdConfigurationRegistration : IDeIdConfigurationRegistration
    {
        private IArtifactStore _artifactStore;
        private Dictionary<string, DeIdConfiguration> _deIdConfigurations;

        public DeIdConfigurationRegistration(
            IArtifactStore artifactStore, 
            IOptions<DeIdConfigurationSection> configuration /* web service configuration */)
        {
            _artifactStore = EnsureArg.IsNotNull(artifactStore, nameof(artifactStore));

            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _deIdConfigurations = configuration.Value.DeIdConfigurations.ToDictionary(x => x.Name, x=>x);
        }

        // Possible to have minutes level cache for further improvement
        public DeIdConfiguration GetByName(string name)
        {
            if (_deIdConfigurations.ContainsKey(name))
            {
                return _deIdConfigurations[name];
            }
            else
            {
                throw new Exception($"The configuration {name} does not exist.");
            }
        }
    }
}
