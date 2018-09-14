using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFileBackup.Tests
{
	public static class HashTest
	{
		#region Tests

		public static void GetHashCodeAndValue()
		{
			Hash hash1 = new Hash();
			Hash hash2 = new Hash(123);
			Assert.AreEqual(0, hash1.GetHashCode());
			Assert.AreEqual(123, hash2.GetHashCode());
			Assert.AreEqual(0, hash1.Value);
			Assert.AreEqual(123, hash2.Value);
		}

		public static void EmptyHashesEqual()
		{
			Hash hash1 = new Hash();
			Hash hash2 = new Hash();
			Assert.AreEqual(hash1, hash2);
			Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
			Assert.AreEqual(hash1.Value, hash2.Value);
		}

		public static void HashesEqual()
		{
			Hash hash1 = new Hash(456);
			Hash hash2 = new Hash(456);
			Assert.AreEqual(hash1, hash2);
			Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
			Assert.AreEqual(hash1.Value, hash2.Value);
		}

		public static void HashesDiffer()
		{
			Hash hash1 = new Hash();
			Hash hash2 = new Hash(123);
			Assert.AreNotEqual(hash1, hash2);
			Assert.AreNotEqual(hash1.GetHashCode(), hash2.GetHashCode());
			Assert.AreNotEqual(hash1.Value, hash2.Value);
		}

		public static void GetFileHash()
		{
			{
				Hash hash1 = Hash.GetFileHash(@"C:\Windows\System32\user32.dll");
				Hash hash2 = Hash.GetFileHash(@"C:\Windows\System32\user32.dll");
				Assert.AreEqual(hash1, hash2);
				Assert.AreEqual(hash1.GetHashCode(), hash2.GetHashCode());
				Assert.AreEqual(hash1.Value, hash2.Value);
			}
			{
				Hash hash1 = Hash.GetFileHash(@"C:\Windows\System32\user32.dll");
				Hash hash2 = Hash.GetFileHash(@"C:\Windows\System32\kernel32.dll");
				Assert.AreNotEqual(hash1, hash2);
				Assert.AreNotEqual(hash1.GetHashCode(), hash2.GetHashCode());
				Assert.AreNotEqual(hash1.Value, hash2.Value);
			}
		}

		#endregion Tests
	}
}
