using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KFileBackup
{
	/// <summary>
	/// A database of all files that are known.
	/// </summary>
	public class FileDatabase
	{
		#region Fields

		/// <summary>The collection of all files known to the database, addressed by file contents.</summary>
		private readonly FileItemCollection fileItems = new FileItemCollection();

		#endregion Fields

		#region Methods

		public void SaveToDatabaseFile(string databaseFile)
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(databaseFile, FileMode.Create, FileAccess.Write)))
			{
				binaryWriter.Write(this.fileItems.Count);
				foreach (FileItem fileItem in this.fileItems)
				{
					binaryWriter.Write(fileItem.Hash.Value);
					binaryWriter.Write(fileItem.FileLocations.Count);
					foreach (FileLocation fileLocation in fileItem.FileLocations)
					{
						binaryWriter.Write(fileLocation.FullPath);
						binaryWriter.Write(fileLocation.IsFromReadOnlyLocation);
					}
				}
			}
		}

		public void AddFilesFromDatabaseFile(string databaseFile)
		{
			using (BinaryReader binaryReader = new BinaryReader(new FileStream(databaseFile, FileMode.Open, FileAccess.Read)))
			{
				int fileItemCount = binaryReader.ReadInt32();
				foreach (int fileItemIndex in Enumerable.Range(0, fileItemCount))
				{
					FileItem fileItem = new FileItem(new Hash(binaryReader.ReadInt32()));
					int fileLocationCount = binaryReader.ReadInt32();
					foreach (int fileLocationIndex in Enumerable.Range(0, fileLocationCount))
					{
						string fullPath = binaryReader.ReadString();
						bool isFromReadOnlyLocation = binaryReader.ReadBoolean();
						fileItem.FileLocations.Add(new FileLocation(fullPath, isFromReadOnlyLocation));
					}
				}
			}
		}

		public void AddFilesFromPath(string path, string searchPattern, bool isFromReadOnlyLocation)
		{
			foreach (FileItem newFileItem in Directory.EnumerateFiles(path, searchPattern, SearchOption.AllDirectories)
				.Select((file) => FileItem.CreateFromPath(file, isFromReadOnlyLocation)))
			{
				if (this.fileItems.TryGetValue(newFileItem.Hash, out FileItem existingFileItem))
				{
					existingFileItem.FileLocations.Add(newFileItem.FileLocations.Single());
				}
				else
				{
					this.fileItems.Add(newFileItem);
				}
			}
		}

		#endregion Methods
	}
}
