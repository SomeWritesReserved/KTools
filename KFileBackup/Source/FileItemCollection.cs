using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace KFileBackup
{
	/// <summary>
	/// Represents a collection of unique <see cref="FileItem"/> objects, addressable by each file's contents.
	/// </summary>
	public class FileItemCollection : IEnumerable<FileItem>, IEnumerable
	{
		#region Fields

		private readonly Dictionary<Hash, FileItem> items = new Dictionary<Hash, FileItem>();

		#endregion Fields

		#region Properties

		public int Count { get { return this.items.Count; } }

		#endregion Properties

		#region Methods

		public void Add(FileItem fileItem)
		{
			this.items.Add(fileItem.Hash, fileItem);
		}

		public bool TryGetValue(Hash hash, out FileItem fileItem)
		{
			return this.items.TryGetValue(hash, out fileItem);
		}

		public IEnumerator<FileItem> GetEnumerator()
		{
			return this.items.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#endregion Methods
	}
}
