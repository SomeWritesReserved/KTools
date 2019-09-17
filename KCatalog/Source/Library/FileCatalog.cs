using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog
{
	public class FileCatalog
	{
		#region Fields

		private readonly Dictionary<Hash, List<FileHash>> catalogedFiles = new Dictionary<Hash, List<FileHash>>();

		#endregion Fields

		#region Constructors

		public FileCatalog()
		{
		}

		public FileCatalog(IEnumerable<FileHash> fileHashs)
		{
			foreach (FileHash fileHash in fileHashs)
			{
				this.Add(fileHash);
			}
		}

		#endregion Constructors

		#region Methods

		public void Add(FileHash fileHash)
		{
			if (this.catalogedFiles.ContainsKey(fileHash.Hash))
			{
				this.catalogedFiles[fileHash.Hash].Add(fileHash);
			}
			else
			{
				this.catalogedFiles.Add(fileHash.Hash, new List<FileHash>() { fileHash });
			}
		}

		public IList<FileHash> Find(Hash hash)
		{
			if (this.catalogedFiles.ContainsKey(hash))
			{
				return this.catalogedFiles[hash].ToList();
			}
			else
			{
				return new List<FileHash>();
			}
		}

		#endregion Methods
	}
}
