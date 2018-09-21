using System;
using System.Collections.Generic;
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
		}

		public static void ToStringAndTryParse()
		{
			{
				Hash hash1 = new Hash(141251, 1510092, 9079235, 1234);
				string hashStr = hash1.ToString();
				Assert.IsTrue(Hash.TryParse(hashStr, out Hash hash2));
				Assert.AreEqual(hash1, hash2);
			}
			{
				Hash hash1 = new Hash(1, 2, 3, 4);
				string hashStr = hash1.ToString();
				Assert.AreEqual("0000000000000001000000000000000200000000000000030000000000000004", hashStr);
				Assert.IsTrue(Hash.TryParse(hashStr, out Hash hash2));
				Assert.AreEqual(hash1, hash2);
			}
		}

		#endregion Tests
	}
}
