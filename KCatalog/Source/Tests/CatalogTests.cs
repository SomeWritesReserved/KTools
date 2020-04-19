using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog.Tests
{
	public class CatalogTests
	{
		#region Tests

		public void Create()
		{
			Catalog catalog = this.createCatalog();
			this.verifyCatalog(catalog);
		}

		public void Serialization()
		{
			MockFileSystem fileSystem = new MockFileSystem();
			Catalog catalogWritten = this.createCatalog();
			catalogWritten.Write(fileSystem.FileInfo.FromFileName(@"C:\.kcatalog"));
			Catalog catalogRead = Catalog.Read(fileSystem.FileInfo.FromFileName(@"C:\.kcatalog"));
			this.verifyCatalog(catalogRead);
		}

		public void ReadFileFormat_2_0()
		{
			const string catalogFileContents1dot1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Catalog>
  <FileFormatVersion>2.0</FileFormatVersion>
  <SoftwareVersion>1.1.0.0</SoftwareVersion>
  <BaseDirectoryPath>C:\files\archive</BaseDirectoryPath>
  <CatalogedOn>2020-04-05T01:19:14.9181074-04:00</CatalogedOn>
  <UpdatedOn>2020-04-05T01:19:14.9181074-04:00</UpdatedOn>
  <Files>
    <f p=""file1.txt"" h=""00000000000003e7000000000000000000000000000000de0000000000000000"" l=""100"" />
    <f p=""file2.txt"" h=""00000000000003780000000000000002000000000000006f0000000000000005"" l=""105"" />
    <f p=""subfolder1\file1-diffname.txt"" h=""00000000000003e7000000000000000000000000000000de0000000000000000"" l=""100"" />
    <f p=""subfolder2\file3-diffname.txt"" h=""000000000000000000000000000003fc00000000000000de000000000000029a"" l=""200"" />
  </Files>
</Catalog>";

			MockFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(){ { @"C:\.kcatalog", new MockFileData(catalogFileContents1dot1) } });
			Catalog catalogRead = Catalog.Read(fileSystem.FileInfo.FromFileName(@"C:\.kcatalog"));
			this.verifyCatalog(catalogRead);
		}

		public void FindFiles()
		{
			Catalog catalog = this.createCatalog();
			{
				IReadOnlyList<FileInstance> files = catalog.FindFiles(new Hash256());
				Assert.AreEqual(0, files.Count);
			}
			{
				IReadOnlyList<FileInstance> files = catalog.FindFiles(new Hash256(1, 2, 3, 4));
				Assert.AreEqual(0, files.Count);
			}
			{
				IReadOnlyList<FileInstance> files = catalog.FindFiles(new Hash256(999, 0, 222, 0));
				Assert.AreEqual(2, files.Count);
				Assert.AreEqual(@"file1.txt", files[0].RelativePath);
				Assert.AreEqual(@"subfolder1\file1-diffname.txt", files[1].RelativePath);
			}
			{
				IReadOnlyList<FileInstance> files = catalog.FindFiles(new Hash256(888, 2, 111, 5));
				Assert.AreEqual(1, files.Count);
				Assert.AreEqual(@"file2.txt", files[0].RelativePath);
			}
			{
				IReadOnlyList<FileInstance> files = catalog.FindFiles(new Hash256(0, 1020, 222, 666));
				Assert.AreEqual(1, files.Count);
				Assert.AreEqual(@"subfolder2\file3-diffname.txt", files[0].RelativePath);
			}
		}

		#endregion Tests

		#region Methods

		private Catalog createCatalog()
		{
			return new Catalog(@"C:\files\archive", DateTime.Now, new[]
			{
				new FileInstance(@"file1.txt", 100, new Hash256(999, 0, 222, 0)),
				new FileInstance(@"file2.txt", 105, new Hash256(888, 2, 111, 5)),
				new FileInstance(@"subfolder1\file1-diffname.txt", 100, new Hash256(999, 0, 222, 0)),
				new FileInstance(@"subfolder2\file3-diffname.txt", 200, new Hash256(0, 1020, 222, 666)),
			});
		}

		private void verifyCatalog(Catalog catalog)
		{
			Assert.AreEqual(@"C:\files\archive", catalog.BaseDirectoryPath);

			Assert.AreEqual(4, catalog.FileInstances.Count);
			Assert.AreEqual(@"file1.txt", catalog.FileInstances[0].RelativePath);
			Assert.AreEqual(100, catalog.FileInstances[0].FileSize);
			Assert.AreEqual(new Hash256(999, 0, 222, 0), catalog.FileInstances[0].FileContentsHash);
			Assert.AreEqual(@"file2.txt", catalog.FileInstances[1].RelativePath);
			Assert.AreEqual(105, catalog.FileInstances[1].FileSize);
			Assert.AreEqual(new Hash256(888, 2, 111, 5), catalog.FileInstances[1].FileContentsHash);
			Assert.AreEqual(@"subfolder1\file1-diffname.txt", catalog.FileInstances[2].RelativePath);
			Assert.AreEqual(100, catalog.FileInstances[2].FileSize);
			Assert.AreEqual(new Hash256(999, 0, 222, 0), catalog.FileInstances[2].FileContentsHash);
			Assert.AreEqual(@"subfolder2\file3-diffname.txt", catalog.FileInstances[3].RelativePath);
			Assert.AreEqual(new Hash256(0, 1020, 222, 666), catalog.FileInstances[3].FileContentsHash);
			Assert.AreEqual(200, catalog.FileInstances[3].FileSize);

			Assert.AreEqual(4, catalog.FileInstancesByPath.Count);
			Assert.AreSame(catalog.FileInstances[0], catalog.FileInstancesByPath[@"file1.txt"]);
			Assert.AreSame(catalog.FileInstances[1], catalog.FileInstancesByPath[@"file2.txt"]);
			Assert.AreSame(catalog.FileInstances[2], catalog.FileInstancesByPath[@"subfolder1\file1-diffname.txt"]);
			Assert.AreSame(catalog.FileInstances[3], catalog.FileInstancesByPath[@"subfolder2\file3-diffname.txt"]);

			Assert.AreEqual(3, catalog.FileInstancesByHash.Count);
			Assert.AreEqual(2, catalog.FileInstancesByHash[new Hash256(999, 0, 222, 0)].Count);
			Assert.AreSame(catalog.FileInstances[0], catalog.FileInstancesByHash[new Hash256(999, 0, 222, 0)][0]);
			Assert.AreSame(catalog.FileInstances[2], catalog.FileInstancesByHash[new Hash256(999, 0, 222, 0)][1]);
			Assert.AreEqual(1, catalog.FileInstancesByHash[new Hash256(888, 2, 111, 5)].Count);
			Assert.AreSame(catalog.FileInstances[1], catalog.FileInstancesByHash[new Hash256(888, 2, 111, 5)][0]);
			Assert.AreEqual(1, catalog.FileInstancesByHash[new Hash256(0, 1020, 222, 666)].Count);
			Assert.AreSame(catalog.FileInstances[3], catalog.FileInstancesByHash[new Hash256(0, 1020, 222, 666)][0]);
		}

		#endregion Methods
	}
}
