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

		/// <summary>Constructor.</summary>
		public FileItem(Hash hash, long fileSize, FileLocation fileLocation)
		{
			this.Hash = hash;
			this.FileSize = fileSize;
			this.FileLocations.Add(fileLocation);
		}

		#endregion Constructors

		#region Properties

		/// <summary>Gets the hash of the file's contents.</summary>
		public Hash Hash { get; }

		/// <summary>Gets the size of the file.</summary>
		public long? FileSize { get; }

		/// <summary>Gets the list of full paths that this file exists at.</summary>
		public HashSet<FileLocation> FileLocations { get; } = new HashSet<FileLocation>();

		#endregion Properties

		#region Methods

		public override int GetHashCode()
		{
			return this.Hash.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as FileItem);
		}

		public bool Equals(FileItem other)
		{
			if (other == null) { return false; }

			// We only want to compare file sizes if both files have a size, which won't always be the case
			bool areFileSizesEqual = true;
			if (this.FileSize.HasValue && other.FileSize.HasValue) { areFileSizesEqual = (this.FileSize.Value.Equals(other.FileSize.Value)); }

			return (this.Hash.Equals(other.Hash) && areFileSizesEqual);
		}

		#region Helpers

		/// <summary>
		/// Creates a new <see cref="FileItem"/> based on the given path on disk.
		/// </summary>
		public static FileItem CreateFromPath(string path, string volumeName, bool isFromReadOnlyVolume)
		{
			using (FileStream fileStream = File.OpenRead(path))
			{
				long fileLength = fileStream.Length;
				return new FileItem(Hash.GetFileHash(fileStream), fileLength, new FileLocation(path, volumeName, isFromReadOnlyVolume));
			}
		}

		#endregion Helpers

		#endregion Methods
	}
}
