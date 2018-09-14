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
	public class FileLocation : IEquatable<FileLocation>
	{
		#region Constructors

		/// <summary>Constructor.</summary>
		public FileLocation(string fullPath, bool isFromReadOnlyLocation)
		{
			this.FullPath = fullPath;
			this.IsFromReadOnlyLocation = isFromReadOnlyLocation;
		}

		#endregion Constructors

		#region Properties

		/// <summary>Gets the full path to this instance of a file.</summary>
		public string FullPath { get; }

		/// <summary>Gets whether this file location is from a read-only location (drive, folder, etc.), and thus unable to change.</summary>
		/// <remarks>This is not the read-only NTFS attribute, this is if the file lives on truly read-only media (DVD/CD, ROM, etc.).</remarks>
		public bool IsFromReadOnlyLocation { get; }

		/// <summary>Gets the file name (not the full path) of this instance of a file.</summary>
		public string FileName
		{
			get { return Path.GetFileName(this.FullPath); }
		}

		#endregion Properties

		#region Methods

		public override int GetHashCode()
		{
			return this.FullPath.ToLower().GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as FileLocation);
		}

		public bool Equals(FileLocation other)
		{
			if (other == null) { return false; }
			return this.FullPath.Equals(other.FullPath, StringComparison.OrdinalIgnoreCase);
		}

		#endregion Methods
	}
}
