using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KCatalog
{
	/// <summary>
	/// Represents a 256-bit hash of some input data.
	/// </summary>
	public struct Hash256 : IEquatable<Hash256>
	{
		#region Fields

		private readonly long hashPart1;
		private readonly long hashPart2;
		private readonly long hashPart3;
		private readonly long hashPart4;

		#endregion Fields

		#region Constructors

		/// <summary>Constructor.</summary>
		public Hash256(long hashPart1, long hashPart2, long hashPart3, long hashPart4)
		{
			this.hashPart1 = hashPart1;
			this.hashPart2 = hashPart2;
			this.hashPart3 = hashPart3;
			this.hashPart4 = hashPart4;
		}

		/// <summary>Constructor.</summary>
		public Hash256(byte[] hashBytes)
		{
			if (hashBytes == null) { throw new ArgumentNullException(nameof(hashBytes)); }
			if (hashBytes.Length != 32) { throw new ArgumentException($"{nameof(hashBytes)} must be 256 bits."); }
			this.hashPart1 = BitConverter.ToInt64(hashBytes, 0);
			this.hashPart2 = BitConverter.ToInt64(hashBytes, 8);
			this.hashPart3 = BitConverter.ToInt64(hashBytes, 16);
			this.hashPart4 = BitConverter.ToInt64(hashBytes, 24);
		}

		#endregion Constructors

		#region Methods

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0:x16}{1:x16}{2:x16}{3:x16}", this.hashPart1, this.hashPart2, this.hashPart3, this.hashPart4);
		}

		public override int GetHashCode()
		{
			return this.hashPart1.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Hash256)) { return false; }
			return this.Equals((Hash256)obj);
		}

		public bool Equals(Hash256 other)
		{
			return (this.hashPart1 == other.hashPart1 &&
				this.hashPart2 == other.hashPart2 &&
				this.hashPart3 == other.hashPart3 &&
				this.hashPart4 == other.hashPart4);
		}

		public static Hash256 Parse(string s)
		{
			Hash256 hash = new Hash256();
			if (s.Length != 64) { throw new FormatException("Not a hash"); }
			if (!long.TryParse(s.Substring(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hashPart1)) { throw new FormatException("Not a hash"); }
			if (!long.TryParse(s.Substring(16, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hashPart2)) { throw new FormatException("Not a hash"); }
			if (!long.TryParse(s.Substring(32, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hashPart3)) { throw new FormatException("Not a hash"); }
			if (!long.TryParse(s.Substring(48, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hashPart4)) { throw new FormatException("Not a hash"); }
			hash = new Hash256(hashPart1, hashPart2, hashPart3, hashPart4);
			return hash;
		}

		public static bool TryParse(string s, out Hash256 hash)
		{
			hash = new Hash256();
			if (s.Length != 64) { return false; }
			if (!long.TryParse(s.Substring(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hashPart1)) { return false; }
			if (!long.TryParse(s.Substring(16, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hashPart2)) { return false; }
			if (!long.TryParse(s.Substring(32, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hashPart3)) { return false; }
			if (!long.TryParse(s.Substring(48, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hashPart4)) { return false; }
			hash = new Hash256(hashPart1, hashPart2, hashPart3, hashPart4);
			return true;
		}

		/// <summary>
		/// Gets the hash of a given stream's contents.
		/// </summary>
		public static Hash256 GetContentsHash(System.IO.Stream stream)
		{
			using (System.IO.BufferedStream bufferedStream = new System.IO.BufferedStream(stream, 1200000))
			{
				using (SHA256Managed sha = new SHA256Managed())
				{
					byte[] hashBytes = sha.ComputeHash(bufferedStream);
					return new Hash256(hashBytes);
				}
			}
		}

		#endregion Methods
	}
}
