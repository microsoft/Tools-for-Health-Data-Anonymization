using System;
using System.IO;
using Microsoft.Health.Fhir.Anonymizer.Core.AnonymizerConfigurations.TextAnalytics;

namespace Microsoft.Health.Fhir.Anonymizer.Core.Models.TextAnalytics
{
    public class Cache
    {
        private string _cachePath;
        private bool _enabled;

        public Cache()
        {
            _enabled = false;
        }

        public Cache(CacheConfiguration cacheConfiguration, string apiVersion)
        {
            _cachePath = Path.Combine(cacheConfiguration.Path, apiVersion);
            _enabled = cacheConfiguration.Enable;
        }

        public string Get(string documentId, int offset)
        {
            if (!_enabled)
            {
                throw new Exception("Cache is not enabled");
            }
            var path = GetFilePath(documentId, offset);
            if (!File.Exists(path))
            {
                throw new Exception("The object does not exist in cache");
            }
            var resultString = File.ReadAllText(path);
            return resultString;
        }

        public void Set(string documentId, int offset, string content)
        {
            if (_enabled)
            {
                var path = GetFilePath(documentId, offset);
                Directory.CreateDirectory(_cachePath);
                File.WriteAllText(path, content);
            }
        }

        public string GetFilePath(string documentId, int offset)
        {
            return Path.Combine(_cachePath, $"{documentId}-{offset}.json");
        }
    }
}
