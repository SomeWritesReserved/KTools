using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace KFileBackup
{
	/// <summary>
	/// Represents a collection of unique <see cref="FileItem"/> objects, addressable by each file's contents.
	/// </summary>
	public class FileItemCollection : KeyedCollection<Hash, FileItem>
	{
		#region Methods

		public bool TryGetValue(Hash hash, out FileItem fileItem)
		{
			return this.Dictionary.TryGetValue(hash, out fileItem);
		}

		protected override Hash GetKeyForItem(FileItem item)
		{
			return item.Hash;
		}

		#endregion Methods
	}
}
