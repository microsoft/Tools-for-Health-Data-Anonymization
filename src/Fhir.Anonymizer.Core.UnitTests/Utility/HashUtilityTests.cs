using System.Collections.Generic;
using System.Linq;
using Xunit;
using Fhir.Anonymizer.Core.Utility;
using System;

namespace Fhir.Anonymizer.Core.UnitTests.Utility
{
    public class HashUtilityTests
    {
        public static IEnumerable<object[]> GetResourceIdSample()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, string.Empty };
            yield return new object[] { "a", "ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb" };
            yield return new object[] { "123", "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3" };
            yield return new object[] { "powjdnsadjasoidjoiqwad", "23199a7de46c2b79df3199e25a403aaa63d18c6ff1112e6b46b8323a2fd36e04" };
            yield return new object[] { "powqiewqbsdnd218937219836szdbahdbkqjawedwhqe1928372198371298321u98hdaskjdbasdkjhbasdiuasdhwquewq",
                "826046417346b0f6bb3f1ba142271f52bc1075ac9def0e245ab361098dd28966" };
        }

        [Theory]
        [MemberData(nameof(GetResourceIdSample))]
        public void GivenAResourceId_WhenHashing_CorrectIdHashShouldReturn(string resourceId, string expectedHash)
        {
            var idHash = HashUtility.GetResourceIdHash(resourceId);
            Assert.Equal(expectedHash, idHash);
        }
    }
}
