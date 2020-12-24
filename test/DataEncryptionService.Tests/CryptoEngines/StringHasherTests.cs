using System;
using System.Collections.Generic;
using DataEncryptionService.Core.CryptoEngines;
using Xunit;

namespace DataEncryptionService.Tests.CryptoEngines
{
    public class StringHasherTests
    {
        StringHasher _sut;

        public StringHasherTests()
        {
            // Arrange (for all tests in this class)
            _sut = CreateTestsStringHasher();
        }

        static public StringHasher CreateTestsStringHasher()
        {
            return new StringHasher();
        }

        [Theory]
        [MemberData(nameof(HashData))]
        public void Hash_Data_ReturnsExpectedValue(HashMethod hashMethod, string clearText, string hashKey, string expectedBase64, string expectedByteSeq)
        {
            byte[] hash = _sut.ComputeHash(clearText, hashMethod, hashKey);
            byte[] expected = Convert.FromBase64String(expectedBase64);
            Assert.Equal(expected, hash);

            // Compare the base64 versions
            string hashBase64 = hash.ToBase64();
            Assert.Equal(expectedBase64, hashBase64);

            // Compare the byte sequence versions
            string hashByteSeq = hash.ToByteSequence();
            Assert.Equal(expectedByteSeq, hashByteSeq);
        }

        [Fact]
        public void Hash_InvalidMethod_ThrowsException()
        {
            var ex = Assert.Throws<NotSupportedException>(() => _sut.ComputeHash("some-data", (HashMethod)999));
        }

        public static IEnumerable<object[]> HashData()
        {
            yield return new object[] { HashMethod.SHA2_256, "Some_Sample_Text_Data", null, "VLzfJovzpx8VE9WpMdHT/e5ADjt9XXUT3mpnF7nbHTg=", "54bcdf268bf3a71f1513d5a931d1d3fdee400e3b7d5d7513de6a6717b9db1d38" };
            yield return new object[] { HashMethod.SHA2_384, "Some_Sample_Text_Data", null, "8OdkcK6GCbf22HokCmFf2iz50+gZGNkD7cR0v9LD//hpxa/LsGXSvj7t+4YYMM7D", "f0e76470ae8609b7f6d87a240a615fda2cf9d3e81918d903edc474bfd2c3fff869c5afcbb065d2be3eedfb861830cec3" };
            yield return new object[] { HashMethod.SHA2_512, "Some_Sample_Text_Data", null, "U/0nsmjJ9zpcDW03rity2x3VRGvGV6iT0WEfx6rMW5p/o7xlRa7LBngUXx/8TQ4nkiiQd5xb8fxjS/U1Khw9cw==", "53fd27b268c9f73a5c0d6d37ae2b72db1dd5446bc657a893d1611fc7aacc5b9a7fa3bc6545aecb0678145f1ffc4d0e27922890779c5bf1fc634bf5352a1c3d73" };
            yield return new object[] { HashMethod.HMAC256, "Some_Sample_Text_Data", "my_hash_key", "OoFpaThOf5elvFROrR3uUBnAYO2eTbJWuT/oo5g81+0=", "3a816969384e7f97a5bc544ead1dee5019c060ed9e4db256b93fe8a3983cd7ed" };
            yield return new object[] { HashMethod.HMAC384, "Some_Sample_Text_Data", "my_hash_key", "2BW/Wzeuq+q4r4eUq8qQ2yD7Y6BNys459x6vi7+xcRET5M6aV7irEB+cc4xBorRC", "d815bf5b37aeabeab8af8794abca90db20fb63a04dcace39f71eaf8bbfb1711113e4ce9a57b8ab101f9c738c41a2b442" };
            yield return new object[] { HashMethod.HMAC512, "Some_Sample_Text_Data", "my_hash_key", "buisboXZGRiOREFGQfcyntLfC2fnt2eRIe2k8VinCsHyj4bqbkxc/U9BmU86/3nItUYUPlaNi4BOynSfHqhHeQ==", "6ee8ac6e85d919188e44414641f7329ed2df0b67e7b7679121eda4f158a70ac1f28f86ea6e4c5cfd4f41994f3aff79c8b546143e568d8b804eca749f1ea84779" };
        }
    }
}
