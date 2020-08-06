using System.Collections.Generic;
using System.Text;
using MicrosoftFhir.Anonymizer.Core.Processors;
using MicrosoftFhir.Anonymizer.Core.Utility;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Xunit;

namespace MicrosoftFhir.Anonymizer.Core.UnitTests.Processors
{
    public class EncryptProcessorTests
    {
        private const string TestEncryptKey = "704ab12c8e3e46d4bea600ef62a6bec7";

        public static IEnumerable<object[]> GetNodesForEncryption()
        {
            yield return new object[] { new Code(null) };
            yield return new object[] { new Code(string.Empty) };
            yield return new object[] { new Code("abc") };
            yield return new object[] { new Code("!@)(*&%^!@#$%@") };
            yield return new object[] { new Code("ͶΆΈΞξτϡϿῧῄᾴѶѾ") };
        }

        [Theory]
        [MemberData(nameof(GetNodesForEncryption))]
        public void GivenANode_WhenEncrypt_ValueShouldBeEncryptedCorrectly(Element element)
        {
            var processor = new EncryptProcessor(TestEncryptKey);
            var node = CreateNodeFromElement(element);
            var originalText = node.Value?.ToString();
            processor.Process(node);

            // Here we only check the cipher text can be correctly decrypted since we are using a random IV during encryption
            Assert.Equal(originalText, DecryptText(node.Value?.ToString()));
        }

        private static ElementNode CreateNodeFromElement(Element element)
        {
            return ElementNode.FromElement(element.ToTypedElement());
        }

        private string DecryptText(string text)
        {
            return EncryptUtility.DecryptTextFromBase64WithAes(text, Encoding.UTF8.GetBytes(TestEncryptKey));
        }
    }
}
