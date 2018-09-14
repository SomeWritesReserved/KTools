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

		public static void InHashSet()
		{
			{
				HashSet<FileLocation> fileLocations1 = new HashSet<FileLocation>();
				fileLocations1.Add(new FileLocation(@"C:\blah\thing.txt", true));
				fileLocations1.Add(new FileLocation(@"c:\blah\thing.txt", true));
				fileLocations1.Add(new FileLocation(@"C:\BLAH\THING.TXT", true));
				fileLocations1.Add(new FileLocation(@"c:\blAH\THING.txt", true));
				Assert.AreEqual(1, fileLocations1.Count);
				Assert.IsTrue(fileLocations1.Contains(new FileLocation(@"c:\BlAh\tHiNg.TxT", false)));
				Assert.IsFalse(fileLocations1.Contains(new FileLocation(@"C:\zlah\thing.txt", false)));
			}
		}

		#endregion Tests
	}
}
