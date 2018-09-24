using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KFileBackup
{
	/// <summary>
	/// Represents a single instance of a file (a <see cref="FileItem"/>) at a specific location.
	/// </summary>
	public class FileLocation : IEquatable<FileLocation>, IComparable<FileLocation>
	{
		#region Constructors

		/// <summary>Constructor.</summary>
		public FileLocation(string fullPath, string volumeName, bool isFromReadOnlyVolume)
		{
			this.FullPath = fullPath;
			this.VolumeName = volumeName;
			this.IsFromReadOnlyVolume = isFromReadOnlyVolume;
		}

		#endregion Constructors

		#region Properties

		/// <summary>Gets the full path to this instance of a file on its volume.</summary>
		public string FullPath { get; }

		/// <summary>Gets the name of the volume that this instance of a file resides on.</summary>
		public string VolumeName { get; }

		/// <summary>Gets whether this file location is from a read-only volume (drive, CD, etc.), and thus unable to change.</summary>
		/// <remarks>This is not the read-only NTFS attribute, this is if the file lives on truly read-only media (DVD/CD, ROM, etc.).</remarks>
		public bool IsFromReadOnlyVolume { get; }

		/// <summary>Gets the file name (not the full path) of this instance of a file.</summary>
		public string FileName
		{
			get { return Path.GetFileName(this.FullPath); }
		}

		#endregion Properties

		#region Methods

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode = hashCode * 31 + this.FullPath.ToLowerInvariant().GetHashCode();
			hashCode = hashCode * 31 + this.VolumeName.GetHashCode();
			return hashCode;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as FileLocation);
		}

		public bool Equals(FileLocation other)
		{
			if (other == null) { return false; }
			return (this.FullPath.ToLowerInvariant() == other.FullPath.ToLowerInvariant() &&
				this.VolumeName == other.VolumeName);
		}

		public int CompareTo(FileLocation other)
		{
			int volumeNameComparison = string.Compare(this.VolumeName, other.VolumeName);
			if (volumeNameComparison != 0) { return volumeNameComparison; }
			return string.Compare(this.FullPath.ToLowerInvariant(), other.FullPath.ToLowerInvariant());
		}

		#endregion Methods
	}
}
