using System;
using System.Security.Cryptography;
using System.Text;
using Hl7.FhirPath;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace Fhir.Anonymizer.Core.AnonymizerConfigurations
{
    public class AnonymizerConfigurationValidator
    {
        private readonly ILogger _logger = AnonymizerLogging.CreateLogger<AnonymizerConfigurationValidator>();
        
        public void Validate(AnonymizerConfiguration config)
        {   
            if (!string.IsNullOrEmpty(config.FhirVersion))
            {
                var currentFullVersion = ModelInfo.Version;
                if (string.IsNullOrEmpty(currentFullVersion))
                {
                    throw new AnonymizerConfigurationErrorsException("Fail to read supported FHIR version");
                }
                string versionNumber = currentFullVersion.Split('.')[0];
                Constants.allowedVersion versionCode;
                if (Enum.TryParse(versionNumber, true, out versionCode) && Enum.IsDefined(typeof(Constants.allowedVersion), versionCode))
                {
                    if (!string.Equals(versionCode.ToString(), config.FhirVersion))
                    {
                        throw new AnonymizerConfigurationErrorsException($"Configuration of fhirVersion {config.FhirVersion} is not supported. Expected fhirVersion: {versionCode}");
                    }   
                }
                else
                {
                    throw new AnonymizerConfigurationErrorsException("Fail to read supported FHIR version");
                }    
            }
            else
            {
                _logger.LogWarning($"Version is not specified in configuration file. Mistakes may accured by missing version");
            }
            
            if (config.FhirPathRules == null)
            {
                throw new AnonymizerConfigurationErrorsException("The configuration is invalid, please specify any fhirPathRules");
            }

            FhirPathCompiler compiler = new FhirPathCompiler();
            foreach (var rule in config.FhirPathRules)
            {
                if (!rule.ContainsKey(Constants.PathKey) || !rule.ContainsKey(Constants.MethodKey))
                {
                    throw new AnonymizerConfigurationErrorsException("Missing path or method in Fhir path rule config.");
                }

                // Grammar check on FHIR path
                try
                {
                    compiler.Compile(rule[Constants.PathKey]);
                }
                catch (Exception ex)
                {
                    throw new AnonymizerConfigurationErrorsException($"Invalid FHIR path {rule[Constants.PathKey]}", ex);
                }

                // Method validate
                string method = rule[Constants.MethodKey];
                if (!Enum.TryParse<AnonymizerMethod>(method, true, out _))
                {
                    throw new AnonymizerConfigurationErrorsException($"{method} not support.");
                }
            }

            // Check AES key size is valid (16, 24 or 32 bytes).
            if (!string.IsNullOrEmpty(config.ParameterConfiguration?.EncryptKey))
            {
                using Aes aes = Aes.Create();
                var encryptKeySize = Encoding.UTF8.GetByteCount(config.ParameterConfiguration.EncryptKey) * 8;
                if (!IsValidKeySize(encryptKeySize, aes.LegalKeySizes))
                {
                    throw new AnonymizerConfigurationErrorsException($"Invalid encrypt key size : {encryptKeySize} bits! Please provide key sizes of 128, 192 or 256 bits.");
                }
            }
        }

        // The following method takes a bit length input and returns whether that length is a valid size
        // validSizes for AES: MinSize=128, MaxSize=256, SkipSize=64
        private bool IsValidKeySize(int bitLength, KeySizes[] validSizes)
        {
            if (validSizes == null)
            {
                return false;
            }

            for (int i = 0; i < validSizes.Length; i++)
            {
                for (int j = validSizes[i].MinSize; j <= validSizes[i].MaxSize; j += validSizes[i].SkipSize)
                {
                    if (j == bitLength)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
