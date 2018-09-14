using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace KFileBackup
{
	/// <summary>
	/// Represents a catalog of unique <see cref="FileItem"/> objects, addressable by each file's contents.
	/// </summary>
	public class FileItemCatalog : IEnumerable<FileItem>, IEnumerable
	{
		#region Fields

		private readonly Dictionary<Hash, FileItem> fileItems = new Dictionary<Hash, FileItem>();

		#endregion Fields

		#region Properties

		public int Count { get { return this.fileItems.Values.Count; } }

		#endregion Properties

		#region Methods

		public void Add(FileItem fileItem)
		{
			this.fileItems.Add(fileItem.Hash, fileItem);
		}

		public bool TryGetValue(Hash hash, out FileItem fileItem)
		{
			return this.fileItems.TryGetValue(hash, out fileItem);
		}

		public IEnumerator<FileItem> GetEnumerator()
		{
			return this.fileItems.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#region Helpers

		public void SaveCatalogToFile(string catalogFile)
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(catalogFile, FileMode.Create, FileAccess.Write)))
			{
				binaryWriter.Write(this.Count);
				foreach (FileItem fileItem in this)
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

		public void ReadCatalogFromFile(string catalogFile)
		{
			using (BinaryReader binaryReader = new BinaryReader(new FileStream(catalogFile, FileMode.Open, FileAccess.Read)))
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
					this.Add(fileItem);
				}
			}
		}

		#endregion Helpers

		#endregion Methods
	}
}
