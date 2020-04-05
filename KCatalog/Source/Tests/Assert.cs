using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog.Tests
{
	public static class Assert
	{
		#region Methods

		public static void AreSame(object expected, object actual)
		{
			if (!object.ReferenceEquals(expected, actual)) { throw new AssertTestException($"Expected and actual are not the same object."); }
		}

		public static void AreEqual<T>(T expected, T actual)
			where T : IEquatable<T>
		{
			if (!expected.Equals(actual)) { throw new AssertTestException($"Expected {expected} but got {actual}."); }
			if (!actual.Equals(expected)) { throw new AssertTestException($"Expected {expected} but got {actual}."); }
			if (expected.GetHashCode() != actual.GetHashCode()) { throw new AssertTestException("Expected and actual are equal but their hashcodes differ."); }
		}

		public static void AreNotEqual<T>(T expected, T actual)
			where T : IEquatable<T>
		{
			if (expected.Equals(actual)) { throw new AssertTestException("expected incorrectly equals actual"); }
			if (actual.Equals(expected)) { throw new AssertTestException("actual incorrectly equals expected"); }
		}

		public static void SequenceEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual)
		{
			if (!expected.SequenceEqual(actual)) { throw new AssertTestException("expected sequence should equal actual but doesn't"); }
			if (!actual.SequenceEqual(expected)) { throw new AssertTestException("actual sequence should equal expected but doesn't"); }
		}

		public static void SequenceNotEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual)
		{
			if (expected.SequenceEqual(actual)) { throw new AssertTestException("expected sequence should not equal actual but does"); }
			if (actual.SequenceEqual(expected)) { throw new AssertTestException("actual sequence should not equal expected but does"); }
		}

		public static void IsTrue(bool actual)
		{
			if (!actual) { throw new AssertTestException("expected true but wasn't"); }
		}

		public static void IsFalse(bool actual)
		{
			if (actual) { throw new AssertTestException("expected false but wasn't"); }
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
				throw new AssertTestException(string.Format("expected exception {0} but got {1}", typeof(T).Name, exception.GetType().Name));
			}
			throw new AssertTestException(string.Format("expected exception {0} but got no exception", typeof(T).Name));
		}

		#endregion Methods
	}

	public class AssertTestException : Exception
	{
		#region Constructors

		public AssertTestException(string message) : base(message)
		{
		}

		#endregion Constructors
	}
}
