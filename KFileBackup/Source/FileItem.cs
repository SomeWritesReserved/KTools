using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KFileBackup
{
	/// <summary>
	/// Represents a file, addressed by its contents, which may exist in multiple locations.
	/// </summary>
	public class FileItem : IEquatable<FileItem>
	{
		#region Constructors

		/// <summary>Constructor.</summary>
		public FileItem(Hash hash)
		{
			this.Hash = hash;
		}

		/// <summary>Constructor.</summary>
		public FileItem(Hash hash, FileLocation fileLocation)
		{
			this.Hash = hash;
			this.FileLocations.Add(fileLocation);
		}

		#endregion Constructors

		#region Properties

		/// <summary>Gets the hash of the file's contents.</summary>
		public Hash Hash { get; }

		/// <summary>Gets the list of full paths that this file exists at.</summary>
		public HashSet<FileLocation> FileLocations { get; } = new HashSet<FileLocation>();

		#endregion Properties

		#region Methods

		/// <summary>
		/// Creates a new <see cref="FileItem"/> based on the given path on disk.
		/// </summary>
		public static FileItem CreateFromPath(string path, bool isFromReadOnlyLocation)
		{
			path = Path.GetFullPath(path);
			return new FileItem(Hash.GetFileHash(path), new FileLocation(path, isFromReadOnlyLocation));
		}

		public override int GetHashCode()
		{
			return this.Hash.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj as FileItem);
		}

		public bool Equals(FileItem other)
		{
			if (other == null) { return false; }
			return this.Hash.Equals(other.Hash);
		}

		#endregion Methods
	}
}
