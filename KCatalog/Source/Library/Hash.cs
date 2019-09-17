using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace KCatalog
{
	/// <summary>
	/// Represents a hash of some input data.
	/// </summary>
	public struct Hash : IEquatable<Hash>
	{
		#region Fields

		private readonly long hashPart1;
		private readonly long hashPart2;
		private readonly long hashPart3;
		private readonly long hashPart4;

		#endregion Fields

		#region Constructors

		/// <summary>Constructor.</summary>
		public Hash(long hashPart1, long hashPart2, long hashPart3, long hashPart4)
		{
			this.hashPart1 = hashPart1;
			this.hashPart2 = hashPart2;
			this.hashPart3 = hashPart3;
			this.hashPart4 = hashPart4;
		}

		/// <summary>Constructor.</summary>
		public Hash(byte[] hashBytes)
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
			return $"{this.hashPart1:x16}{this.hashPart2:x16}{this.hashPart3:x16}{this.hashPart4:x16}";
		}

		public override int GetHashCode()
		{
			return this.hashPart1.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Hash)) { return false; }
			return this.Equals((Hash)obj);
		}

		public bool Equals(Hash other)
		{
			return (this.hashPart1 == other.hashPart1 &&
				this.hashPart2 == other.hashPart2 &&
				this.hashPart3 == other.hashPart3 &&
				this.hashPart4 == other.hashPart4);
		}

		#region Helpers

		public static bool TryParse(string s, out Hash hash)
		{
			hash = new Hash();
			if (s.Length != 64) { return false; }
			if (!long.TryParse(s.Substring(0, 16), NumberStyles.HexNumber, null, out long hashPart1)) { return false; }
			if (!long.TryParse(s.Substring(16, 16), NumberStyles.HexNumber, null, out long hashPart2)) { return false; }
			if (!long.TryParse(s.Substring(32, 16), NumberStyles.HexNumber, null, out long hashPart3)) { return false; }
			if (!long.TryParse(s.Substring(48, 16), NumberStyles.HexNumber, null, out long hashPart4)) { return false; }
			hash = new Hash(hashPart1, hashPart2, hashPart3, hashPart4);
			return true;
		}

		/// <summary>
		/// Gets the hash of a given file's contents.
		/// </summary>
		public static Hash GetFileHash(string file)
		{
			using (FileStream fileStream = File.OpenRead(file))
			{
				return Hash.GetFileHash(fileStream);
			}
		}

		/// <summary>
		/// Gets the hash of a given file's contents.
		/// </summary>
		public static Hash GetFileHash(FileStream fileStream)
		{
			using (BufferedStream stream = new BufferedStream(fileStream, 1200000))
			{
				using (SHA256Managed sha = new SHA256Managed())
				{
					byte[] hashBytes = sha.ComputeHash(stream);
					return new Hash(hashBytes);
				}
			}
		}

		/// <summary>
		/// Write a hash value to a <see cref="BinaryWriter"/>.
		/// </summary>
		public static void Write(Hash hash, BinaryWriter binaryWriter)
		{
			binaryWriter.Write(hash.hashPart1);
			binaryWriter.Write(hash.hashPart2);
			binaryWriter.Write(hash.hashPart3);
			binaryWriter.Write(hash.hashPart4);
		}

		/// <summary>
		/// Reads a hash value from a <see cref="BinaryReader"/>
		/// </summary>
		public static Hash Read(BinaryReader binaryReader)
		{
			return new Hash(binaryReader.ReadInt64(), binaryReader.ReadInt64(), binaryReader.ReadInt64(), binaryReader.ReadInt64());
		}

		#endregion Helpers

		#endregion Methods
	}
}
