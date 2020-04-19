using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog.Tests
{
	public partial class CommandTests
	{
		#region Tests

		#region Catalog Commands

		public void CatalogCreateA()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			Assert.IsTrue(fileSystem.File.Exists(@"C:\folderA\.kcatalog"));
			Assert.IsFalse(fileSystem.File.Exists(@"C:\folderB\.kcatalog"));
			Catalog catalog = Catalog.Read(fileSystem.FileInfo.FromFileName(@"C:\folderA\.kcatalog"));
			Assert.AreEqual(@"C:\folderA", catalog.BaseDirectoryPath);
			Assert.AreEqual(6, catalog.FileInstances.Count);
			Assert.AreEqual(6, catalog.FileInstancesByPath.Count);
			Assert.AreEqual(4, catalog.FileInstancesByHash.Count);
			Assert.AreEqual(1, catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"file1.txt"].FileContentsHash].Count);
			Assert.AreEqual(1, catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"file2.txt"].FileContentsHash].Count);
			Assert.AreEqual(2, catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"file3.txt"].FileContentsHash].Count);
			Assert.AreEqual(2, catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"file4.txt"].FileContentsHash].Count);
		}

		public void CatalogCreateB()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderB" });
			Assert.IsFalse(fileSystem.File.Exists(@"C:\folderA\.kcatalog"));
			Assert.IsTrue(fileSystem.File.Exists(@"C:\folderB\.kcatalog"));
			Catalog catalog = Catalog.Read(fileSystem.FileInfo.FromFileName(@"C:\folderB\.kcatalog"));
			Assert.AreEqual(@"C:\folderB", catalog.BaseDirectoryPath);
			Assert.AreEqual(3, catalog.FileInstances.Count);
			Assert.AreEqual(3, catalog.FileInstancesByPath.Count);
			Assert.AreEqual(3, catalog.FileInstancesByHash.Count);
			Assert.AreSame(catalog.FileInstancesByPath[@"file1-diffname.txt"], catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"file1-diffname.txt"].FileContentsHash].Single());
			Assert.AreSame(catalog.FileInstancesByPath[@"file2.txt"], catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"file2.txt"].FileContentsHash].Single());
			Assert.AreSame(catalog.FileInstancesByPath[@"subdirY\file4.txt"], catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"subdirY\file4.txt"].FileContentsHash].Single());
		}

		public void CatalogCreateC()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderC" });
			Assert.IsTrue(fileSystem.File.Exists(@"C:\folderC\.kcatalog"));
			Catalog catalog = Catalog.Read(fileSystem.FileInfo.FromFileName(@"C:\folderC\.kcatalog"));
			Assert.AreEqual(@"C:\folderC", catalog.BaseDirectoryPath);
			Assert.AreEqual(4, catalog.FileInstances.Count);
			Assert.AreEqual(4, catalog.FileInstancesByPath.Count);
			Assert.AreEqual(4, catalog.FileInstancesByHash.Count);
			Assert.AreSame(catalog.FileInstancesByPath[@"file1-diffname.txt"], catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"file1-diffname.txt"].FileContentsHash].Single());
			Assert.AreSame(catalog.FileInstancesByPath[@"file2.txt"], catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"file2.txt"].FileContentsHash].Single());
			Assert.AreSame(catalog.FileInstancesByPath[@"subdirY\file4.txt"], catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"subdirY\file4.txt"].FileContentsHash].Single());
			Assert.AreSame(catalog.FileInstancesByPath[@"subdirZ\file5.txt"], catalog.FileInstancesByHash[catalog.FileInstancesByPath[@"subdirZ\file5.txt"].FileContentsHash].Single());
		}

		public void CatalogCreate_DeclineOverwrite()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			Assert.Throws<OperationCanceledException>(() => new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("notyes")).Run(new[] { "catalog-create", @"C:\folderA" }));
		}

		public void CatalogCheckA_NoChanges()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-check", "--log", @"C:\folderA\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(1, logLines.Length);
			Assert.AreEqual(@"Added  : .kcatalog", logLines[0]);
		}

		public void CatalogCheckA_NoChangesTwice()
		{
			// This test catalogs twice, the second time it should include the .kcatalog file in the catalog itself, so the check returns zero
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("yes")).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-check", "--log", @"C:\folderA\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(0, logLines.Length);
		}

		public void CatalogCheckA_Changes()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			fileSystem.File.WriteAllText(@"C:\folderA\newfile1.txt", "newfile1");
			fileSystem.Directory.CreateDirectory(@"C:\folderA\subdirW");
			fileSystem.File.WriteAllText(@"C:\folderA\subdirW\newfile2.txt", "newfile2");
			fileSystem.File.Delete(@"C:\folderA\file3.txt");
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-check", "--log", @"C:\folderA\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(4, logLines.Length);
			Assert.AreEqual(@"Removed: file3.txt", logLines[0]);
			Assert.AreEqual(@"Added  : .kcatalog", logLines[1]);
			Assert.AreEqual(@"Added  : newfile1.txt", logLines[2]);
			Assert.AreEqual(@"Added  : subdirW\newfile2.txt", logLines[3]);
		}

		public void CatalogCompareAB()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderB" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-compare", "--log", @"C:\folderA\.kcatalog", @"C:\folderB\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(4, logLines.Length); // Last line is just a summary line
			Assert.AreEqual(@"file1-diffname.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"subdirY\file4.txt", logLines[2]);
		}

		public void CatalogCompareAB_Delete()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderB" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("yes")).Run(new[] { "catalog-compare", "--log", "--delete", @"C:\folderA\.kcatalog", @"C:\folderB\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(5, logLines.Length); // Last two lines are just summary lines
			Assert.AreEqual(@"file1-diffname.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"subdirY\file4.txt", logLines[2]);
			string[] filesLeft = fileSystem.Directory.GetFiles(@"C:\folderB", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(1, filesLeft.Length);
			Assert.AreEqual(@"C:\folderB\.kcatalog", filesLeft[0]);
		}

		public void CatalogCompareBA()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderB" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-compare", "--log", @"C:\folderB\.kcatalog", @"C:\folderA\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(5, logLines.Length); // Last line is just a summary line
			Assert.AreEqual(@"file1.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"file4.txt", logLines[2]);
			Assert.AreEqual(@"file4-diffname.txt", logLines[3]);
		}

		public void CatalogCompareBA_Delete()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderB" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("yes")).Run(new[] { "catalog-compare", "--log", "--delete", @"C:\folderB\.kcatalog", @"C:\folderA\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(6, logLines.Length); // Last two lines are just summary lines
			Assert.AreEqual(@"file1.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"file4.txt", logLines[2]);
			Assert.AreEqual(@"file4-diffname.txt", logLines[3]);
			string[] filesLeft = fileSystem.Directory.GetFiles(@"C:\folderA", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(3, filesLeft.Length);
			Assert.AreEqual(@"C:\folderA\file3.txt", filesLeft[0]);
			Assert.AreEqual(@"C:\folderA\subdirX\file3.txt", filesLeft[1]);
			Assert.AreEqual(@"C:\folderA\.kcatalog", filesLeft[2]);
		}

		public void CatalogCompareAC()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderC" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-compare", "--log", @"C:\folderA\.kcatalog", @"C:\folderC\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(4, logLines.Length); // Last line is just a summary line
			Assert.AreEqual(@"file1-diffname.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"subdirY\file4.txt", logLines[2]);
		}

		public void CatalogCompareAC_Delete()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderC" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("yes")).Run(new[] { "catalog-compare", "--log", "--delete", @"C:\folderA\.kcatalog", @"C:\folderC\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(5, logLines.Length); // Last two lines are just summary lines
			Assert.AreEqual(@"file1-diffname.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"subdirY\file4.txt", logLines[2]);
			string[] filesLeft = fileSystem.Directory.GetFiles(@"C:\folderC", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(2, filesLeft.Length);
			Assert.AreEqual(@"C:\folderC\subdirZ\file5.txt", filesLeft[0]);
			Assert.AreEqual(@"C:\folderC\.kcatalog", filesLeft[1]);
		}

		public void CatalogCompare_DeclineDelete()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderA" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\folderB" });
			Assert.Throws<OperationCanceledException>(() => new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("notyes")).Run(new[] { "catalog-compare", "--delete", @"C:\folderA\.kcatalog", @"C:\folderB\.kcatalog" }));
		}

		#endregion Catalog Commands

		#region Directory Commands

		public void DirectoryCompareAB()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "dir-compare", "--log", @"C:\folderA", @"C:\folderB" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(4, logLines.Length); // Last line is just a summary line
			Assert.AreEqual(@"file1-diffname.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"subdirY\file4.txt", logLines[2]);
		}

		public void DirectoryCompareAB_Delete()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("yes")).Run(new[] { "dir-compare", "--log", "--delete", @"C:\folderA", @"C:\folderB" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(5, logLines.Length); // Last two lines are just summary lines
			Assert.AreEqual(@"file1-diffname.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"subdirY\file4.txt", logLines[2]);
			string[] filesLeft = fileSystem.Directory.GetFiles(@"C:\folderB", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(0, filesLeft.Length);
		}

		public void DirectoryCompareBA()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "dir-compare", "--log", @"C:\folderB", @"C:\folderA" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(5, logLines.Length); // Last line is just a summary line
			Assert.AreEqual(@"file1.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"file4.txt", logLines[2]);
			Assert.AreEqual(@"file4-diffname.txt", logLines[3]);
		}

		public void DirectoryCompareBA_Delete()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("yes")).Run(new[] { "dir-compare", "--log", "--delete", @"C:\folderB", @"C:\folderA" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(6, logLines.Length); // Last two lines are just summary lines
			Assert.AreEqual(@"file1.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"file4.txt", logLines[2]);
			Assert.AreEqual(@"file4-diffname.txt", logLines[3]);
			string[] filesLeft = fileSystem.Directory.GetFiles(@"C:\folderA", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(2, filesLeft.Length);
			Assert.AreEqual(@"C:\folderA\file3.txt", filesLeft[0]);
			Assert.AreEqual(@"C:\folderA\subdirX\file3.txt", filesLeft[1]);
		}

		public void DirectoryCompareAC()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "dir-compare", "--log", @"C:\folderA", @"C:\folderC" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(4, logLines.Length); // Last line is just a summary line
			Assert.AreEqual(@"file1-diffname.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"subdirY\file4.txt", logLines[2]);
		}

		public void DirectoryCompareAC_Delete()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("yes")).Run(new[] { "dir-compare", "--log", "--delete", @"C:\folderA", @"C:\folderC" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(5, logLines.Length); // Last two lines are just summary lines
			Assert.AreEqual(@"file1-diffname.txt", logLines[0]);
			Assert.AreEqual(@"file2.txt", logLines[1]);
			Assert.AreEqual(@"subdirY\file4.txt", logLines[2]);
			string[] filesLeft = fileSystem.Directory.GetFiles(@"C:\folderC", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(1, filesLeft.Length);
			Assert.AreEqual(@"C:\folderC\subdirZ\file5.txt", filesLeft[0]);
		}

		public void DirectoryCompare_DeclineDelete()
		{
			MockFileSystem fileSystem = this.createMockFileSystem();
			Assert.Throws<OperationCanceledException>(() => new CommandRunner(fileSystem, System.IO.TextWriter.Null, new System.IO.StringReader("notyes")).Run(new[] { "dir-compare", "--delete", @"C:\folderA", @"C:\folderB" }));
		}

		#endregion Directory Commands

		#endregion Tests

		#region Helpers

		private MockFileSystem createMockFileSystem()
		{
			return new MockFileSystem(new Dictionary<string, MockFileData>()
			{
				{ @"C:\folderA\file1.txt", new MockFileData("file1") },
				{ @"C:\folderA\file2.txt", new MockFileData("file2") },
				{ @"C:\folderA\file3.txt", new MockFileData("file3") },
				{ @"C:\folderA\file4.txt", new MockFileData("file4") },
				{ @"C:\folderA\file4-diffname.txt", new MockFileData("file4") },
				{ @"C:\folderA\subdirX\file3.txt", new MockFileData("file3") },

				{ @"C:\folderB\file1-diffname.txt", new MockFileData("file1") },
				{ @"C:\folderB\file2.txt", new MockFileData("file2") },
				{ @"C:\folderB\subdirY\file4.txt", new MockFileData("file4") },

				{ @"C:\folderC\file1-diffname.txt", new MockFileData("file1") },
				{ @"C:\folderC\file2.txt", new MockFileData("file2") },
				{ @"C:\folderC\subdirY\file4.txt", new MockFileData("file4") },
				{ @"C:\folderC\subdirZ\file5.txt", new MockFileData("file5") },
			}, @"C:\KCatalog");
		}

		private string[] getLogLines(IFileSystem fileSystem)
		{
			string[] lines = fileSystem.File.ReadAllLines(fileSystem.Directory.GetFiles(@"C:\KCatalog").Single()).Skip(1).ToArray(); // Skip the first line since it is always the arguments
			return lines;
		}

		#endregion Helpers
	}
}
