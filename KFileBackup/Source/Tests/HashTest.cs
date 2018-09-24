using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KFileBackup.Tests
{
	public static class HashTest
	{
		#region Tests

		public static void GetHashCodeMethod()
		{
			Hash hash1 = new Hash();
			Hash hash2 = TestHelper.Hash(123);
			Assert.AreEqual(0, hash1.GetHashCode());
			Assert.AreEqual(123, hash2.GetHashCode());
		}

		public static void EmptyHashesEqual()
		{
			Hash hash1 = new Hash();
			Hash hash2 = new Hash();
			Assert.AreEqual(hash1, hash2);
			Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
		}

		public static void HashesEqual()
		{
			Hash hash1 = TestHelper.Hash(456);
			Hash hash2 = TestHelper.Hash(456);
			Assert.AreEqual(hash1, hash2);
			Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
		}

		public static void HashesDiffer()
		{
			Hash hash1 = new Hash();
			Hash hash2 = TestHelper.Hash(123);
			Assert.AreNotEqual(hash1, hash2);
			Assert.AreNotEqual(hash1.GetHashCode(), hash2.GetHashCode());
		}

		public static void GetFileHash()
		{
			{
				Hash hash1 = Hash.GetFileHash(@"C:\Windows\System32\user32.dll");
				Hash hash2 = Hash.GetFileHash(@"C:\Windows\System32\user32.dll");
				Assert.AreEqual(hash1, hash2);
				Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
			}
			{
				Hash hash1 = Hash.GetFileHash(@"C:\Windows\System32\user32.dll");
				Hash hash2 = Hash.GetFileHash(@"C:\Windows\System32\kernel32.dll");
				Assert.AreNotEqual(hash1, hash2);
				Assert.AreNotEqual(hash1.GetHashCode(), hash2.GetHashCode());
			}
			{
				try
				{
					Random random1a = new Random(123151);
					Random random1b = new Random(123151);
					Random random1c = new Random(123151);
					Random random2 = new Random(9876);
					File.WriteAllBytes("getfilehashtest1a.file", Enumerable.Range(0, 10240).Select((i) => (byte)random1a.Next(256)).ToArray());
					File.WriteAllBytes("getfilehashtest1b.file", Enumerable.Range(0, 10240).Select((i) => (byte)random1b.Next(256)).ToArray());
					File.WriteAllBytes("getfilehashtest1c.file", Enumerable.Range(0, 10241).Select((i) => (byte)random1c.Next(256)).ToArray());
					File.WriteAllBytes("getfilehashtest2.file", Enumerable.Range(0, 10240).Select((i) => (byte)random2.Next(256)).ToArray());
					Hash hash1a = Hash.GetFileHash("getfilehashtest1a.file");
					Hash hash1b = Hash.GetFileHash("getfilehashtest1b.file");
					Hash hash1c = Hash.GetFileHash("getfilehashtest1c.file");
					Hash hash2 = Hash.GetFileHash("getfilehashtest2.file");
					Assert.AreEqual(hash1a, hash1b);
					Assert.AreNotEqual(hash1a, hash1c);
					Assert.AreNotEqual(hash1a, hash2);
					Assert.AreNotEqual(hash1c, hash2);
					Assert.AreEqual(new Hash(-4503150692253193366, -4589931198477819425, -7393329944829092212, 1525878695725488611), hash1a);
					Assert.AreEqual("c181984dda31ab6ac04d49e4bb3f21df99659d9633341e8c152d0296dd0dd9e3", hash1a.ToString());
					Assert.AreEqual(new Hash(-4503150692253193366, -4589931198477819425, -7393329944829092212, 1525878695725488611), hash1b);
					Assert.AreEqual("c181984dda31ab6ac04d49e4bb3f21df99659d9633341e8c152d0296dd0dd9e3", hash1b.ToString());
					Assert.AreEqual(new Hash(4050519298861383970, -7596970268443003904, -6680004443397598450, -8393995096044701854), hash1c);
					Assert.AreEqual("383655d77c7f4122969223ca94403400a34bdb59d541530e8b8289eef5e27762", hash1c.ToString());
					Assert.AreEqual(new Hash(8844761264859441472, 3207933974862152339, 7733136889014184772, 1800345724947963099), hash2);
					Assert.AreEqual("7abee7824314e9402c84def42a90c2936b519f075528eb4418fc1ce5f06a9cdb", hash2.ToString());
				}
				finally
				{
					File.Delete("getfilehashtest1a.file");
					File.Delete("getfilehashtest1b.file");
					File.Delete("getfilehashtest1c.file");
					File.Delete("getfilehashtest2.file");
				}
			}
		}

		public static void ToStringAndTryParse()
		{
			{
				Hash hash1 = new Hash(141251, 1510092, 9079235, 1234);
				string hashStr = hash1.ToString();
				Assert.IsTrue(Hash.TryParse(hashStr, out Hash hash2));
				Assert.AreEqual(hash1, hash2);
				Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
				Assert.AreEqual(141251, hash1.GetHashCode());
			}
			{
				Hash hash1 = new Hash(1, 2, 3, 4);
				string hashStr = hash1.ToString();
				Assert.AreEqual("0000000000000001000000000000000200000000000000030000000000000004", hashStr);
				Assert.IsTrue(Hash.TryParse(hashStr, out Hash hash2));
				Assert.AreEqual(hash1, hash2);
				Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
				Assert.AreEqual(1, hash1.GetHashCode());
			}
		}

		#endregion Tests
	}
}
