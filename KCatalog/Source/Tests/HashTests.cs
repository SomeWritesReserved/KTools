using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog.Tests
{
	public class Hash256Tests
	{
		#region Tests

		public void GetHashCodeMethod()
		{
			Hash256 hash1 = new Hash256();
			Hash256 hash2 = new Hash256(123, 0, 0, 0);
			Assert.AreEqual(0, hash1.GetHashCode());
			Assert.AreEqual(123, hash2.GetHashCode());
		}

		public void EmptyHashesEqual()
		{
			Hash256 hash1 = new Hash256();
			Hash256 hash2 = new Hash256();
			Assert.AreEqual(hash1, hash2);
			Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
		}

		public void HashesEqual()
		{
			Hash256 hash1 = new Hash256(456, 0, 1, 0);
			Hash256 hash2 = new Hash256(456, 0, 1, 0);
			Assert.AreEqual(hash1, hash2);
			Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
		}

		public void HashesDiffer()
		{
			Hash256 hash1 = new Hash256();
			Hash256 hash2 = new Hash256(123, 0, 0, 0);
			Assert.AreNotEqual(hash1, hash2);
			Assert.AreNotEqual(hash1.GetHashCode(), hash2.GetHashCode());
		}

		public void GetContentsHash()
		{
			{
				System.IO.Stream stream1 = this.getRandomStream(54611);
				System.IO.Stream stream2 = this.getRandomStream(54611);
				Hash256 hash1 = Hash256.GetContentsHash(stream1);
				Hash256 hash2 = Hash256.GetContentsHash(stream2);
				Assert.AreEqual(hash1, hash2);
				Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
			}
			{
				System.IO.Stream stream1 = this.getRandomStream(54600);
				System.IO.Stream stream2 = this.getRandomStream(54611);
				Hash256 hash1 = Hash256.GetContentsHash(stream1);
				Hash256 hash2 = Hash256.GetContentsHash(stream2);
				Assert.AreNotEqual(hash1, hash2);
				Assert.AreNotEqual(hash1.GetHashCode(), hash2.GetHashCode());
			}
			{
				Random random1a = new Random(123151);
				Random random1b = new Random(123151);
				Random random1c = new Random(123151);
				Random random2 = new Random(9876);
				MockFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
				{
					{ "getfilehashtest1a.file", new MockFileData(Enumerable.Range(0, 10240).Select((i) => (byte)random1a.Next(256)).ToArray()) },
					{ "getfilehashtest1b.file", new MockFileData(Enumerable.Range(0, 10240).Select((i) => (byte)random1b.Next(256)).ToArray()) },
					{ "getfilehashtest1c.file", new MockFileData(Enumerable.Range(0, 10241).Select((i) => (byte)random1c.Next(256)).ToArray()) },
					{ "getfilehashtest2.file", new MockFileData(Enumerable.Range(0, 10240).Select((i) => (byte)random2.Next(256)).ToArray()) },
				});
				Hash256 hash1a = Hash256.GetContentsHash(fileSystem.File.OpenRead("getfilehashtest1a.file"));
				Hash256 hash1b = Hash256.GetContentsHash(fileSystem.File.OpenRead("getfilehashtest1b.file"));
				Hash256 hash1c = Hash256.GetContentsHash(fileSystem.File.OpenRead("getfilehashtest1c.file"));
				Hash256 hash2 = Hash256.GetContentsHash(fileSystem.File.OpenRead("getfilehashtest2.file"));
				Assert.AreEqual(hash1a, hash1b);
				Assert.AreNotEqual(hash1a, hash1c);
				Assert.AreNotEqual(hash1a, hash2);
				Assert.AreNotEqual(hash1c, hash2);
				Assert.AreEqual(new Hash256(-4503150692253193366, -4589931198477819425, -7393329944829092212, 1525878695725488611), hash1a);
				Assert.AreEqual("c181984dda31ab6ac04d49e4bb3f21df99659d9633341e8c152d0296dd0dd9e3", hash1a.ToString());
				Assert.AreEqual(new Hash256(-4503150692253193366, -4589931198477819425, -7393329944829092212, 1525878695725488611), hash1b);
				Assert.AreEqual("c181984dda31ab6ac04d49e4bb3f21df99659d9633341e8c152d0296dd0dd9e3", hash1b.ToString());
				Assert.AreEqual(new Hash256(4050519298861383970, -7596970268443003904, -6680004443397598450, -8393995096044701854), hash1c);
				Assert.AreEqual("383655d77c7f4122969223ca94403400a34bdb59d541530e8b8289eef5e27762", hash1c.ToString());
				Assert.AreEqual(new Hash256(8844761264859441472, 3207933974862152339, 7733136889014184772, 1800345724947963099), hash2);
				Assert.AreEqual("7abee7824314e9402c84def42a90c2936b519f075528eb4418fc1ce5f06a9cdb", hash2.ToString());
			}
		}

		public void ToStringAndTryParse()
		{
			{
				Hash256 hash1 = new Hash256(141251, 1510092, 9079235, 1234);
				string hashStr = hash1.ToString();
				Assert.IsTrue(Hash256.TryParse(hashStr, out Hash256 hash2));
				Assert.AreEqual(hash1, hash2);
				Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
				Assert.AreEqual(141251, hash1.GetHashCode());
			}
			{
				Hash256 hash1 = new Hash256(1, 2, 3, 4);
				string hashStr = hash1.ToString();
				Assert.AreEqual("0000000000000001000000000000000200000000000000030000000000000004", hashStr);
				Assert.IsTrue(Hash256.TryParse(hashStr, out Hash256 hash2));
				Assert.AreEqual(hash1, hash2);
				Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
				Assert.AreEqual(1, hash1.GetHashCode());
			}
		}

		#endregion Tests

		#region Helpers

		private System.IO.Stream getRandomStream(int? seed = null)
		{
			Random random = seed.HasValue ? new Random(seed.Value) : new Random();
			byte[] bytes = new byte[10240];
			random.NextBytes(bytes);
			return new System.IO.MemoryStream(bytes);
		}

		#endregion Helpers
	}
}
