using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFileBackup.Tests
{
	public static class FileLocationTest
	{
		#region Tests

		public static void FileName()
		{
			{
				FileLocation fileLocation1 = new FileLocation(@"C:\blah\thing.txt", true);
				Assert.AreEqual(@"C:\blah\thing.txt", fileLocation1.FullPath);
				Assert.AreEqual("thing.txt", fileLocation1.FileName);
				Assert.AreEqual(true, fileLocation1.IsFromReadOnlyLocation);
			}
			{
				FileLocation fileLocation2 = new FileLocation(@"F:\file", false);
				Assert.AreEqual(@"F:\file", fileLocation2.FullPath);
				Assert.AreEqual("file", fileLocation2.FileName);
				Assert.AreEqual(false, fileLocation2.IsFromReadOnlyLocation);
			}
		}

		public static void InHashSetCaseInsensitivity()
		{
			HashSet<FileLocation> fileLocations = new HashSet<FileLocation>();
			fileLocations.Add(new FileLocation(@"C:\blah\thing.txt", true));
			fileLocations.Add(new FileLocation(@"c:\blah\thing.txt", true));
			fileLocations.Add(new FileLocation(@"C:\BLAH\THING.TXT", true));
			fileLocations.Add(new FileLocation(@"c:\blAH\THING.txt", true));
			Assert.AreEqual(1, fileLocations.Count);
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\BlAh\tHiNg.TxT", false)));
			Assert.IsFalse(fileLocations.Contains(new FileLocation(@"C:\zlah\thing.txt", false)));
		}

		public static void InHashSet()
		{
			HashSet<FileLocation> fileLocations = new HashSet<FileLocation>();
			fileLocations.Add(new FileLocation(@"C:\folder1\file1", true));
			fileLocations.Add(new FileLocation(@"C:\folder1\file2", true));
			fileLocations.Add(new FileLocation(@"C:\folder2\file1", true));
			fileLocations.Add(new FileLocation(@"C:\folder2\file2", true));
			Assert.AreEqual(4, fileLocations.Count);
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder1\file1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder1\file2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder2\file1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder2\file2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder1\File1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder1\File2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder2\File1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder2\File2", false)));
		}

		public static void EqualsAndGetHashCode()
		{
			{
				FileLocation fileLocation1 = new FileLocation(@"C:\folder\file", true);
				FileLocation fileLocation2 = new FileLocation(@"C:\folder\file", true);
				Assert.AreEqual(fileLocation1.FullPath, fileLocation2.FullPath);
				Assert.AreEqual(fileLocation1.FileName, fileLocation2.FileName);
				Assert.AreEqual(fileLocation1, fileLocation2);
				Assert.AreEqual(fileLocation1.GetHashCode(), fileLocation2.GetHashCode());
			}
			{
				FileLocation fileLocation1 = new FileLocation(@"C:\folder1\fileA", true);
				FileLocation fileLocation2 = new FileLocation(@"C:\f2\fileB.txt", true);
				Assert.AreNotEqual(fileLocation1.FullPath, fileLocation2.FullPath);
				Assert.AreNotEqual(fileLocation1.FileName, fileLocation2.FileName);
				Assert.AreNotEqual(fileLocation1, fileLocation2);
				Assert.AreNotEqual(fileLocation1.GetHashCode(), fileLocation2.GetHashCode());
			}
			{
				FileLocation fileLocation1 = new FileLocation(@"C:\blah\thing.txt", true);
				FileLocation fileLocation2 = new FileLocation(@"c:\BLAH\THING.TXT", true);
				Assert.AreNotEqual(fileLocation1.FullPath, fileLocation2.FullPath);
				Assert.AreNotEqual(fileLocation1.FileName, fileLocation2.FileName);
				Assert.AreEqual(fileLocation1, fileLocation2);
				Assert.AreEqual(fileLocation1.GetHashCode(), fileLocation2.GetHashCode());
			}
		}

		#endregion Tests
	}
}
