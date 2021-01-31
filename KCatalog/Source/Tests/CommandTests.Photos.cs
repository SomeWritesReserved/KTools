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

		#region Photo

		public void PhotoArchiveValidate_NoWarnings()
		{
			MockFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
			{
				{ @"C:\Archive\Photos\2001\01-January\01-01-2001 First Folder\file1.jpg", new MockFileData("file1") },
				{ @"C:\Archive\Photos\2004\02-February\02-29-2004 Second Folder\file2.jpg", new MockFileData("file2") },
				{ @"C:\Archive\Photos\2020\03-March\03-24-2020 Third Folder\file3.jpg", new MockFileData("file3") },
				{ @"C:\Archive\Photos\2019\04-April\04-30-2019 Fourth Folder\file4.jpg", new MockFileData("file4") },
				{ @"C:\Archive\Photos\2010\05-May\05-05-2010 Five Folder\file5.jpg", new MockFileData("file5") },
				{ @"C:\Archive\Photos\2012\06-June\06-06-2012 Six Folder\file6.jpg", new MockFileData("file6") },
				{ @"C:\Archive\Photos\2000\07-July\07-31-2000 7th Folder\file7.jpg", new MockFileData("file7") },
				{ @"C:\Archive\Photos\1999\08-August\08-20-1999 8th Folder\file8.jpg", new MockFileData("file8") },
				{ @"C:\Archive\Photos\2008\09-September\09-20-2008 9th Folder\file9.jpg", new MockFileData("file9") },
				{ @"C:\Archive\Photos\2003\10-October\10-09-2003 Tenth Folder\file10.jpg", new MockFileData("file10") },
				{ @"C:\Archive\Photos\2003\11-November\11-09-2003 11th Folder\file11.jpg", new MockFileData("file11") },
				{ @"C:\Archive\Photos\2003\12-December\12-09-2003 Twelfth Folder\file12.jpg", new MockFileData("file12") },
				{ @"C:\Archive\Photos\2003\12-December\12-09-2003 Twelfth Folder\IMG_20031227_134135627.jpg", new MockFileData("file12b") },
				{ @"C:\Archive\Photos\2003\12-December\12-09-2003 Twelfth Folder\20031228_134135627.jpg", new MockFileData("file12c") },
			}, @"C:\KCatalog");
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "photo-archive-validate", "--log", @"C:\Archive\Photos" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(0, logLines.Length);
		}

		public void PhotoArchiveValidate_Warnings()
		{
			MockFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
			{
				{ @"C:\Archive\Photos\Unknown\file0.jpg", new MockFileData("file0") },
				{ @"C:\Archive\Photos\2001\01-January2\01-01-2001 First Folder\file1.jpg", new MockFileData("file1") },
				{ @"C:\Archive\Photos\2003\02-February\02-29-2004 Second Folder\file2.jpg", new MockFileData("file2") },
				{ @"C:\Archive\Photos\2004\02-February\Second Folder\file2.jpg", new MockFileData("file2") },
				{ @"C:\Archive\Photos\2020\03-March\04-03-2020\file3.jpg", new MockFileData("file3") },
				{ @"C:\Archive\Photos\2003\12-December\12-09-2003 Twelfth Folder\IMG_20060427_134135627.jpg", new MockFileData("file4") },
			}, @"C:\KCatalog");
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "photo-archive-validate", "--log", @"C:\Archive\Photos" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(8, logLines.Length);
			Assert.IsTrue(logLines.Contains(@"Bad year folder: C:\Archive\Photos\Unknown"));
			Assert.IsTrue(logLines.Contains(@"Bad month folder: C:\Archive\Photos\2001\01-January2"));
			Assert.IsTrue(logLines.Contains(@"Day folder in wrong year: C:\Archive\Photos\2003\02-February\02-29-2004 Second Folder"));
			Assert.IsTrue(logLines.Contains(@"Bad day folder: C:\Archive\Photos\2004\02-February\Second Folder"));
			Assert.IsTrue(logLines.Contains(@"Day folder in wrong month: C:\Archive\Photos\2020\03-March\04-03-2020"));
			Assert.IsTrue(logLines.Contains(@"Day folder has no description: C:\Archive\Photos\2020\03-March\04-03-2020"));
			Assert.IsTrue(logLines.Contains(@"Photo in wrong month: C:\Archive\Photos\2003\12-December\12-09-2003 Twelfth Folder\IMG_20060427_134135627.jpg"));
			Assert.IsTrue(logLines.Contains(@"Photo in wrong year: C:\Archive\Photos\2003\12-December\12-09-2003 Twelfth Folder\IMG_20060427_134135627.jpg"));
		}

		public void PhotoArchive()
		{
			MockFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
			{
				{ @"C:\NewPhotos\1\IMG_20191227_134135627.jpg", new MockFileData("SameFileContents") },
				{ @"C:\NewPhotos\1\IMG_20200305_121959365_HDR.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\1\IMG_20200305_122004044_HDR.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\1\20190308_094641268.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\1\19991231_235941268.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\1\IMG_20200308_142308693_HDR.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\1\IMG_20200308_210249636_b.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\1\IMG_20200317_211008298.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\1\IMG_20200317_214609286~2.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\1\IMG_20200408_180116912.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\1\VID_20200222_094636243.mp4", new MockFileData("") },
				{ @"C:\NewPhotos\1\VID_20200408_184313175.mp4", new MockFileData("") },
				{ @"C:\NewPhotos\2\IMG_20191227_134135627.jpg", new MockFileData("SameFileContents") }, // this will be deleted because it is the same as above
				{ @"C:\NewPhotos\2\IMG_20200308_163333584.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\2\IMG_20200303_213129184.jpg", new MockFileData("") },
				{ @"C:\NewPhotos\2\IMG_20190830_172847_01.jpg", new MockFileData("") },
			}, @"C:\KCatalog");
			fileSystem.AddDirectory(@"C:\Archive\Photos");
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\Archive\Photos" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "photo-archive", "--log", @"C:\NewPhotos", @"C:\Archive\Photos\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(0, logLines.Length);
			Assert.AreEqual(0, fileSystem.Directory.GetFiles(@"C:\NewPhotos", "*", System.IO.SearchOption.AllDirectories).Length);
			string[] archivedFiles = fileSystem.Directory.GetFiles(@"C:\Archive\Photos", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(16, archivedFiles.Length);
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\.kcatalog"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2019\12-December\12-27-2019\20191227_134135627.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-05-2020\20200305_121959365_HDR.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-05-2020\20200305_122004044_HDR.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2019\03-March\03-08-2019\20190308_094641268.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\1999\12-December\12-31-1999\19991231_235941268.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-08-2020\20200308_142308693_HDR.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-08-2020\20200308_210249636_b.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-17-2020\20200317_211008298.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-17-2020\20200317_214609286~2.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\04-April\04-08-2020\20200408_180116912.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\02-February\02-22-2020\20200222_094636243.mp4"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\04-April\04-08-2020\20200408_184313175.mp4"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-08-2020\20200308_163333584.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-03-2020\20200303_213129184.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2019\08-August\08-30-2019\20190830_172847_01.jpg"));
		}
		
		public void PhotoArchive_InCatalog()
		{
			MockFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
			{
				{ @"C:\NewPhotos\1\IMG_20191227_134135627.jpg", new MockFileData("FileContents1") },
				{ @"C:\NewPhotos\1\IMG_20200305_121959365_HDR.jpg", new MockFileData("FileContents2") },
				{ @"C:\NewPhotos\1\IMG_20200305_122004044_HDR.jpg", new MockFileData("FileContents3") },
				{ @"C:\NewPhotos\1\20190308_094641268.jpg", new MockFileData("FileContents4") },
				{ @"C:\NewPhotos\1\19991231_235941268.jpg", new MockFileData("FileContents5") },
				{ @"C:\NewPhotos\1\IMG_20200308_142308693_HDR.jpg", new MockFileData("FileContents6") },
				{ @"C:\Archive\Photos\existing\existing6.jpg", new MockFileData("FileContents6") },
			}, @"C:\KCatalog");
			fileSystem.AddDirectory(@"C:\Archive\Photos");
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\Archive\Photos" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "photo-archive", "--log", @"C:\NewPhotos", @"C:\Archive\Photos\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(1, logLines.Length);
			Assert.IsTrue(logLines.Contains(@"Will not archive file, it is already in the catalog elsewhere: C:\NewPhotos\1\IMG_20200308_142308693_HDR.jpg"));
			string[] sourceFiles = fileSystem.Directory.GetFiles(@"C:\NewPhotos", "*", System.IO.SearchOption.AllDirectories);
			Assert.IsTrue(sourceFiles.Contains(@"C:\NewPhotos\1\IMG_20200308_142308693_HDR.jpg"));
			Assert.AreEqual(1, sourceFiles.Length);
			string[] archivedFiles = fileSystem.Directory.GetFiles(@"C:\Archive\Photos", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(7, archivedFiles.Length);
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\.kcatalog"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2019\12-December\12-27-2019\20191227_134135627.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-05-2020\20200305_121959365_HDR.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2020\03-March\03-05-2020\20200305_122004044_HDR.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2019\03-March\03-08-2019\20190308_094641268.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\1999\12-December\12-31-1999\19991231_235941268.jpg"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\existing\existing6.jpg"));
		}

		public void PhotoArchive_Warnings()
		{
			MockFileSystem fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
			{
				{ @"C:\NewPhotos\1\IMG_20191227_134135627.jpg", new MockFileData("DiffFileContents123") },
				{ @"C:\NewPhotos\2\IMG_20191227_134135627.jpg", new MockFileData("DiffFileContents567") },
				{ @"C:\NewPhotos\2\someotherjpg.jpg", new MockFileData("") },
			}, @"C:\KCatalog");
			fileSystem.AddDirectory(@"C:\Archive\Photos");
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "catalog-create", @"C:\Archive\Photos" });
			new CommandRunner(fileSystem, System.IO.TextWriter.Null, System.IO.TextReader.Null).Run(new[] { "photo-archive", "--log", @"C:\NewPhotos", @"C:\Archive\Photos\.kcatalog" });
			string[] logLines = this.getLogLines(fileSystem);
			Assert.AreEqual(2, logLines.Length);
			Assert.IsTrue(logLines.Contains(@"Cannot archive file, identical file name already exists with different file contents: C:\NewPhotos\2\IMG_20191227_134135627.jpg to C:\Archive\Photos\2019\12-December\12-27-2019\20191227_134135627.jpg"));
			Assert.IsTrue(logLines.Contains(@"Cannot archive file, unknown date: C:\NewPhotos\2\someotherjpg.jpg"));
			string[] sourceFiles = fileSystem.Directory.GetFiles(@"C:\NewPhotos", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(2, sourceFiles.Length);
			Assert.IsTrue(sourceFiles.Contains(@"C:\NewPhotos\2\IMG_20191227_134135627.jpg"));
			Assert.IsTrue(sourceFiles.Contains(@"C:\NewPhotos\2\someotherjpg.jpg"));
			Assert.AreEqual(fileSystem.File.ReadAllText(@"C:\NewPhotos\2\IMG_20191227_134135627.jpg"), "DiffFileContents567");
			string[] archivedFiles = fileSystem.Directory.GetFiles(@"C:\Archive\Photos", "*", System.IO.SearchOption.AllDirectories);
			Assert.AreEqual(2, archivedFiles.Length);
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\.kcatalog"));
			Assert.IsTrue(archivedFiles.Contains(@"C:\Archive\Photos\2019\12-December\12-27-2019\20191227_134135627.jpg"));
			Assert.AreEqual(fileSystem.File.ReadAllText(@"C:\Archive\Photos\2019\12-December\12-27-2019\20191227_134135627.jpg"), "DiffFileContents123");
		}

		#endregion Photo

		#endregion Tests
	}
}
