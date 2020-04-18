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
            yield return new object[] { "a", "69e99e82127c1f146f50653e02b92c4bb0c3bc182a6165a5bbce5f4f94e1ccb7" };
            yield return new object[] { "123", "3cafe40f92be6ac77d2792b4b267c2da11e3f3087b93bb19c6c5133786984b44" };
            yield return new object[] { "powjd-nsadja-soidjo-iqwad", "27acbaa2e30ed771abdc1524732f079d25dcce82d34415a35164a8726e024bb1" };
            yield return new object[] { "powqiewqbs-dnd21893720-198/@136.szdbahdbkqjawedwhqe1928372198371298321u98hdaskjdbasdkjhbasdiuasdhwquewq",
                "203b915b2106cbc2d0293f02509a6222fa656318ac17d78c2b19e44defa1fe3e" };
        }

        [Theory]
        [MemberData(nameof(GetResourceIdSample))]
        public void GivenAResourceId_WhenHashing_CorrectIdHashShouldReturn(string resourceId, string expectedHash)
        {
            var idHash = HashUtility.GetResourceIdHash(resourceId, "123");
            Assert.Equal(expectedHash, idHash);
        }
    }
}
