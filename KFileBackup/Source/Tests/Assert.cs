using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFileBackup.Tests
{
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

		public static void AreEqual(AddOrMergeResult expected, AddOrMergeResult actual)
		{
			if (!expected.Equals(actual)) { throw new ApplicationException("expected result should equal actual but doesn't"); }
			if (!actual.Equals(expected)) { throw new ApplicationException("actual result should equal expected but doesn't"); }
		}

		public static void AreEqual(CatalogFileResult expected, CatalogFileResult actual)
		{
			if (!expected.Equals(actual)) { throw new ApplicationException("expected result should equal actual but doesn't"); }
			if (!actual.Equals(expected)) { throw new ApplicationException("actual result should equal expected but doesn't"); }
		}
		
		public static void AreEqual(CheckFileResult expected, CheckFileResult actual)
		{
			if (!expected.Equals(actual)) { throw new ApplicationException("expected result should equal actual but doesn't"); }
			if (!actual.Equals(expected)) { throw new ApplicationException("actual result should equal expected but doesn't"); }
		}

		public static void SequenceEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual)
		{
			if (!expected.SequenceEqual(actual)) { throw new ApplicationException("expected sequence should equal actual but doesn't"); }
			if (!actual.SequenceEqual(expected)) { throw new ApplicationException("actual sequence should equal expected but doesn't"); }
		}

		public static void IsTrue(bool actual)
		{
			if (!actual) { throw new ApplicationException("expected true but wasn't"); }
		}

		public static void IsFalse(bool actual)
		{
			if (actual) { throw new ApplicationException("expected false but wasn't"); }
		}

		public static void Throws<T>(Action action)
			where T : Exception
		{
			try
			{
				action();
			}
			catch (T)
			{
				return;
			}
			catch (Exception exception)
			{
				throw new ApplicationException(string.Format("expected exception {0} but got {1}", typeof(T).Name, exception.GetType().Name));
			}
			throw new ApplicationException(string.Format("expected exception {0} but got no exception", typeof(T).Name));
		}
	}
}
