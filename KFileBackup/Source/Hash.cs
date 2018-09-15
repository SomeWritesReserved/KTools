using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KFileBackup
{
	/// <summary>
	/// Represents a hash of some input data.
	/// </summary>
	public struct Hash : IEquatable<Hash>
	{
		#region Constructors

		/// <summary>Constructor.</summary>
		public Hash(long value)
		{
			this.Value = value;
		}

		#endregion Constructors

		#region Properties

		/// <summary>Gets the value of the hash.</summary>
		public long Value { get; }

		#endregion Properties

		#region Methods

		public override int GetHashCode()
		{
			return this.Value.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Hash)) { return false; }
			return this.Equals((Hash)obj);
		}

		public bool Equals(Hash other)
		{
			return (this.Value == other.Value);
		}

		#region Helpers

		/// <summary>
		/// Gets the hash of a given file's contents.
		/// </summary>
		public static Hash GetFileHash(string file)
		{
			using (BufferedStream stream = new BufferedStream(File.OpenRead(file), 1200000))
			{
				SHA1Managed sha = new SHA1Managed();
				byte[] hash = sha.ComputeHash(stream);
				return new Hash(BitConverter.ToInt64(hash, 0));
			}
		}

		#endregion Helpers

		#endregion Methods
	}
}
