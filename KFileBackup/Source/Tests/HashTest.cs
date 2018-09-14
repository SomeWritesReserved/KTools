using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFileBackup.Tests
{
	public static class HashTest
	{
		#region Tests

		public static void GetHashCodeValue()
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

		#endregion Tests
	}

	public static class Assert
	{
		public static void AreEqual<T>(T expected, T actual)
			where T : IEquatable<T>
		{
			if (!expected.Equals(actual)) { throw new ApplicationException("expected should equal actual but doesn't"); }
			if (!actual.Equals(expected)) { throw new ApplicationException("actual should equal expected but doesn't"); }
		}

		public static void AreNotEqual<T>(T expected, T actual)
			where T : IEquatable<T>
		{
			if (expected.Equals(actual)) { throw new ApplicationException("expected incorrectly equals actual"); }
			if (actual.Equals(expected)) { throw new ApplicationException("actual incorrectly equals expected"); }
		}

		public static void IsTrue(bool actual)
		{
			if (!actual) { throw new ApplicationException("expected true but wasn't"); }
		}

		public static void IsFalse(bool actual)
		{
			if (actual) { throw new ApplicationException("expected false but wasn't"); }
		}
	}
}
