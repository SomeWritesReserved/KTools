using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog
{
	/// <summary>
	/// Represents a single instance of a file in some catalog.
	/// </summary>
	public sealed class FileInstance : IEquatable<FileInstance>
	{
		#region Constructors

		public FileInstance(string relativePath, long fileSize, Hash256 fileContentsHash)
		{
			this.RelativePath = relativePath;
			this.FileSize = fileSize;
			this.FileContentsHash = fileContentsHash;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Gets the relative path in which this file instance exists based on the root cataloged directory.
		/// </summary>
		public string RelativePath { get; }

		/// <summary>
		/// Gets the file size in bytes of this file.
		/// </summary>
		public long FileSize { get; }

		/// <summary>
		/// Gets the hash of this file's contents.
		/// </summary>
		public Hash256 FileContentsHash { get; }

		#endregion Properties

		#region Methods

		public override int GetHashCode()
		{
			return this.FileContentsHash.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as FileInstance);
		}

		public bool Equals(FileInstance other)
		{
			if (other == null) { return false; }
			return (this.FileSize.Equals(other.FileSize) &&
				this.FileContentsHash.Equals(other.FileContentsHash) &&
				this.RelativePath.Equals(other.RelativePath, StringComparison.OrdinalIgnoreCase));
		}

		#endregion Methods
	}
}
