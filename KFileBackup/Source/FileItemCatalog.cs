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

		/// <summary>
		/// Adds a new <see cref="FileItem"/> to this catalog, throwing an exception if it already exists.
		/// </summary>
		public void Add(FileItem fileItem)
		{
			this.fileItems.Add(fileItem.Hash, fileItem);
		}

		/// <summary>
		/// Adds or merges the given <see cref="FileItem"/> into this catalog, adding if it doesn't exist or combining
		/// the paths of the given <see cref="FileItem"/> with the <see cref="FileItem"/> already in the catalog.
		/// </summary>
		public void AddOrMerge(FileItem fileItem)
		{
			if (this.TryGetValue(fileItem.Hash, out FileItem existingFileItem))
			{
				existingFileItem.FileLocations.UnionWith(fileItem.FileLocations);
			}
			else
			{
				this.Add(fileItem);
			}
		}

		/// <summary>
		/// Gets the <see cref="FileItem"/> associated with the specified <see cref="Hash"/>, returning whether or not
		/// the <see cref="FileItem"/> exists.
		/// </summary>
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

		/// <summary>
		/// Saves the catalog to a file.
		/// </summary>
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

		/// <summary>
		/// Reads a catalog from a file and merged it with this catalog.
		/// </summary>
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
					this.AddOrMerge(fileItem);
				}
			}
		}

		#endregion Helpers

		#endregion Methods
	}
}
