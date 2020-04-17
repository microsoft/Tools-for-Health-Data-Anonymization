using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

namespace Fhir.Anonymizer.Core.Utility
{
    public class HashUtility
    {
        public static string GetResourceIdHash(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                return resourceId;
            }

            using var sha256 = SHA256.Create();
            var plainData = Encoding.UTF8.GetBytes(resourceId);
            var hashData = sha256.ComputeHash(plainData);

            return string.Concat(hashData.Select(b => b.ToString("x2")));
        }
    }
}
