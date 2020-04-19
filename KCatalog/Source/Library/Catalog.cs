using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

		private static readonly Version fileFormatVersion = new Version(2, 0);

		#endregion Fields

		#region Constructors

		public Catalog(string baseDirectoryPath, DateTime catalogedOn, IEnumerable<FileInstance> fileInstances)
			: this(baseDirectoryPath, catalogedOn, catalogedOn, fileInstances)
		{
		}

		public Catalog(string baseDirectoryPath, DateTime catalogedOn, DateTime updatedOn, IEnumerable<FileInstance> fileInstances)
		{
			this.BaseDirectoryPath = baseDirectoryPath;
			this.CatalogedOn = catalogedOn;
			this.UpdatedOn = updatedOn;
			this.FileInstances = fileInstances.ToList().AsReadOnly();

			this.FileInstancesByPath = fileInstances.ToDictionary((fileInstance) => fileInstance.RelativePath, (fileInstance) => fileInstance);
			this.FileInstancesByHash = this.FileInstances.GroupBy((fileInstance) => fileInstance.FileContentsHash)
				.ToDictionary((group) => group.Key, (group) => (IReadOnlyList<FileInstance>)group.ToList().AsReadOnly());
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Gets the base directory where this catalog was taken.
		/// </summary>
		public string BaseDirectoryPath { get; }

		/// <summary>
		/// Gets the date and time the catalog was taken on.
		/// </summary>
		public DateTime CatalogedOn { get; }

		/// <summary>
		/// Gets the date and time the catalog was last updated on.
		/// </summary>
		public DateTime UpdatedOn { get; set; }

		/// <summary>
		/// Gets the collection of all files that have been cataloged.
		/// </summary>
		public IReadOnlyList<FileInstance> FileInstances { get; }

		/// <summary>
		/// Gets the collection of all files that have been cataloged, keyed by their relative file paths.
		/// </summary>
		public IReadOnlyDictionary<string, FileInstance> FileInstancesByPath { get; }

		/// <summary>
		/// Gets the collection of all files that have been cataloged, keyed by their file content hashes. Several files may have the same content
		/// thus the value of this dictionary is a list of files for the same file content hash.
		/// </summary>
		public IReadOnlyDictionary<Hash256, IReadOnlyList<FileInstance>> FileInstancesByHash { get; }

		#endregion Properties

		#region Methods

		/// <summary>
		/// Returns the list of files in this catalog that have the specified file contents hash, or an empty list of there are no files with that hash.
		/// </summary>
		public IReadOnlyList<FileInstance> FindFiles(Hash256 fileContentsHash)
		{
			if (this.FileInstancesByHash.ContainsKey(fileContentsHash))
			{
				return this.FileInstancesByHash[fileContentsHash];
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
						new XElement("SoftwareVersion", Program.SoftwareVersion),
						new XElement("BaseDirectoryPath", this.BaseDirectoryPath),
						new XElement("CatalogedOn", this.CatalogedOn),
						new XElement("UpdatedOn", this.UpdatedOn),
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
				string baseDirectoryPath = (string)xDocument.Element("Catalog").Element("BaseDirectoryPath");
				DateTime catalogedOn = (DateTime)xDocument.Element("Catalog").Element("CatalogedOn");
				DateTime updatedOn = (DateTime)xDocument.Element("Catalog").Element("UpdatedOn");
				List<FileInstance> fileInstances = xDocument.Element("Catalog").Element("Files").Elements("f")
					.Select((element) => new FileInstance(element.Attribute("p").Value, (long)element.Attribute("l"), Hash256.Parse(element.Attribute("h").Value)))
					.ToList();
				return new Catalog(baseDirectoryPath, catalogedOn, updatedOn, fileInstances);
			}
		}

		#endregion Methods
	}
}
