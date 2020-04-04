using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KCatalog
{
	/// <summary>
	/// Represents a collection of files that have been cataloged from a specific directory and its subdirectories.
	/// </summary>
	public class Catalog
	{
		#region Fields

		private static readonly Version fileFormatVersion = new Version(1, 1);

		private readonly Dictionary<Hash256, List<FileInstance>> fileInstancesByHash = new Dictionary<Hash256, List<FileInstance>>();

		#endregion Fields

		#region Constructors

		public Catalog(IEnumerable<FileInstance> fileInstances, DateTime catalogedOn)
		{
			this.FileInstances = fileInstances.ToList().AsReadOnly();
			this.CatalogedOn = catalogedOn;

			foreach (FileInstance fileInstance in this.FileInstances)
			{
				if (this.fileInstancesByHash.ContainsKey(fileInstance.FileContentsHash))
				{
					this.fileInstancesByHash[fileInstance.FileContentsHash].Add(fileInstance);
				}
				else
				{
					this.fileInstancesByHash.Add(fileInstance.FileContentsHash, new List<FileInstance>() { fileInstance });
				}
			}
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Gets the collection of files that have been cataloged.
		/// </summary>
		public IReadOnlyList<FileInstance> FileInstances { get; }

		/// <summary>
		/// Gets the date and time the catalog was taken on.
		/// </summary>
		public DateTime CatalogedOn { get; }

		#endregion Properties

		#region Methods

		/// <summary>
		/// Returns the list of files in this catalog that have the specified file contents hash, or an empty list of there are no files with that hash.
		/// </summary>
		public IReadOnlyList<FileInstance> Find(Hash256 fileContentsHash)
		{
			if (this.fileInstancesByHash.ContainsKey(fileContentsHash))
			{
				return this.fileInstancesByHash[fileContentsHash].ToList().AsReadOnly();
			}
			else
			{
				return new List<FileInstance>();
			}
		}

		public void Write(IFileInfo fileInfo)
		{
			using (System.IO.Stream stream = fileInfo.Open(System.IO.FileMode.Create, System.IO.FileAccess.Write))
			{
				new XDocument(
					new XElement("Catalog",
						new XElement("FileFormatVersion", Catalog.fileFormatVersion),
						new XElement("Date", DateTime.Now),
						new XElement("Files",
							this.FileInstances.Select((fileInstance) => new XElement("f", new XAttribute("p", fileInstance.RelativePath), new XAttribute("h", fileInstance.FileContentsHash), new XAttribute("l", fileInstance.FileSize)))
						)
					)
				).Save(stream);
			}
		}

		public static Catalog Read(IFileInfo fileInfo)
		{
			using (System.IO.Stream stream = fileInfo.Open(System.IO.FileMode.Open, System.IO.FileAccess.Read))
			{
				XDocument xDocument = XDocument.Load(stream);
				DateTime catalogedOn = DateTime.Parse(xDocument.Element("Catalog").Element("Date").Value);
				List<FileInstance> fileInstances = xDocument.Element("Catalog").Element("Files").Elements("f")
					.Select((element) => new FileInstance(element.Attribute("p").Value, long.Parse(element.Attribute("l").Value), Hash256.Parse(element.Attribute("h").Value)))
					.ToList();
				return new Catalog(fileInstances, catalogedOn);
			}
		}

		#endregion Methods
	}
}
