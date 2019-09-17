using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog
{
	public sealed class FileHash : IEquatable<FileHash>
	{
		#region Constructors

		public FileHash(string relativePath, long fileSize, Hash hash)
		{
			this.RelativePath = relativePath;
			this.FileSize = fileSize;
			this.Hash = hash;
		}

		#endregion Constructors

		#region Properties

		public string RelativePath { get; }

		public long FileSize { get; }

		public Hash Hash { get; }

		#endregion Properties

		#region Methods

		public override int GetHashCode()
		{
			return this.Hash.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as FileHash);
		}

		public bool Equals(FileHash other)
		{
			if (other == null) { return false; }
			return (this.FileSize.Equals(other.FileSize) &&
				this.Hash.Equals(other.Hash) &&
				this.RelativePath.Equals(other.RelativePath, StringComparison.OrdinalIgnoreCase));
		}

		#endregion Methods
	}
}
