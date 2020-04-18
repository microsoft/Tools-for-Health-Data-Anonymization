using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

namespace Fhir.Anonymizer.Core.Utility
{
    public class HashUtility
    {
        public static string GetResourceIdHash(string resourceId, string hashKey)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                return resourceId;
            }

            var key = Encoding.UTF8.GetBytes(hashKey);
            using var hmac = new HMACSHA256(key);
            var plainData = Encoding.UTF8.GetBytes(resourceId);
            var hashData = hmac.ComputeHash(plainData);

            return string.Concat(hashData.Select(b => b.ToString("x2")));
        }
    }
}
