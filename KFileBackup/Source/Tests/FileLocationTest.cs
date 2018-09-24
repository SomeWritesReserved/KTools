using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFileBackup.Tests
{
	public static class FileLocationTest
	{
		#region Tests

		public static void FileNameAndVolume()
		{
			{
				FileLocation fileLocation1 = new FileLocation(@"C:\blah\thing.txt", "Local Disk", true);
				Assert.AreEqual(@"C:\blah\thing.txt", fileLocation1.FullPath);
				Assert.AreEqual("thing.txt", fileLocation1.FileName);
				Assert.AreEqual("Local Disk", fileLocation1.VolumeName);
				Assert.AreEqual(true, fileLocation1.IsFromReadOnlyVolume);
			}
			{
				FileLocation fileLocation2 = new FileLocation(@"F:\file", "Archived Pics", false);
				Assert.AreEqual(@"F:\file", fileLocation2.FullPath);
				Assert.AreEqual("file", fileLocation2.FileName);
				Assert.AreEqual("Archived Pics", fileLocation2.VolumeName);
				Assert.AreEqual(false, fileLocation2.IsFromReadOnlyVolume);
			}
		}

		public static void InHashSetCaseInsensitivity()
		{
			HashSet<FileLocation> fileLocations = new HashSet<FileLocation>();
			fileLocations.Add(new FileLocation(@"C:\blah\thing.txt", "V1", true));
			fileLocations.Add(new FileLocation(@"c:\blah\thing.txt", "V1", true));
			fileLocations.Add(new FileLocation(@"C:\BLAH\THING.TXT", "V1", true));
			fileLocations.Add(new FileLocation(@"c:\blAH\THING.txt", "V1", true));
			fileLocations.Add(new FileLocation(@"C:\blah\thing.txt", "V2", true));
			fileLocations.Add(new FileLocation(@"c:\blah\thing.txt", "V2", true));
			fileLocations.Add(new FileLocation(@"C:\BLAH\THING.TXT", "V2", true));
			fileLocations.Add(new FileLocation(@"c:\blAH\THING.txt", "V2", true));
			fileLocations.Add(new FileLocation(@"C:\blah\thing2.txt", "V2", true));
			fileLocations.Add(new FileLocation(@"c:\blah\thing2.txt", "V2", true));
			fileLocations.Add(new FileLocation(@"C:\BLAH\THING2.TXT", "V2", true));
			fileLocations.Add(new FileLocation(@"c:\blAH\THING2.txt", "V2", true));
			Assert.AreEqual(3, fileLocations.Count);
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\BlAh\tHiNg.TxT", "V1", false)));
			Assert.IsFalse(fileLocations.Contains(new FileLocation(@"C:\zlah\thing.txt", "V1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\BlAh\tHiNg.TxT", "V2", false)));
			Assert.IsFalse(fileLocations.Contains(new FileLocation(@"C:\zlah\thing.txt", "V2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\BlAh\tHiNg2.TxT", "V2", false)));
			Assert.IsFalse(fileLocations.Contains(new FileLocation(@"C:\zlah\thing2.txt", "V2", false)));
			Assert.IsFalse(fileLocations.Contains(new FileLocation(@"C:\zlah\thing2.txt", "V1", false)));
		}

		public static void InHashSet()
		{
			HashSet<FileLocation> fileLocations = new HashSet<FileLocation>();
			fileLocations.Add(new FileLocation(@"C:\folder1\file1", "V1", true));
			fileLocations.Add(new FileLocation(@"C:\folder1\file2", "V1", true));
			fileLocations.Add(new FileLocation(@"C:\folder2\file1", "V1", true));
			fileLocations.Add(new FileLocation(@"C:\folder2\file2", "V1", true));
			fileLocations.Add(new FileLocation(@"C:\folder1\file1", "V2", true));
			fileLocations.Add(new FileLocation(@"C:\folder1\file2", "V2", true));
			fileLocations.Add(new FileLocation(@"C:\folder2\file1", "V2", true));
			fileLocations.Add(new FileLocation(@"C:\folder2\file2", "V2", true));
			Assert.AreEqual(8, fileLocations.Count);
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder1\file1", "V1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder1\file2", "V1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder2\file1", "V1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder2\file2", "V1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder1\File1", "V1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder1\File2", "V1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder2\File1", "V1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder2\File2", "V1", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder1\file1", "V2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder1\file2", "V2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder2\file1", "V2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"C:\folder2\file2", "V2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder1\File1", "V2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder1\File2", "V2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder2\File1", "V2", false)));
			Assert.IsTrue(fileLocations.Contains(new FileLocation(@"c:\Folder2\File2", "V2", false)));
		}

		public static void EqualsAndGetHashCode()
		{
			{
				FileLocation fileLocation1 = new FileLocation(@"C:\folder\file", "V1", true);
				FileLocation fileLocation2 = new FileLocation(@"C:\folder\file", "V1", true);
				Assert.AreEqual(fileLocation1.FullPath, fileLocation2.FullPath);
				Assert.AreEqual(fileLocation1.FileName, fileLocation2.FileName);
				Assert.AreEqual(fileLocation1, fileLocation2);
				Assert.AreEqual(fileLocation1.GetHashCode(), fileLocation2.GetHashCode());
			}
			{
				FileLocation fileLocation1 = new FileLocation(@"C:\folder\file", "V1", true);
				FileLocation fileLocation2 = new FileLocation(@"C:\folder\file", "v1", true);
				Assert.AreNotEqual(fileLocation1, fileLocation2);
				Assert.AreNotEqual(fileLocation1.GetHashCode(), fileLocation2.GetHashCode());
			}
			{
				FileLocation fileLocation1 = new FileLocation(@"C:\folder1\fileA", "V1", true);
				FileLocation fileLocation2 = new FileLocation(@"C:\f2\fileB.txt", "V1", true);
				Assert.AreNotEqual(fileLocation1.FullPath, fileLocation2.FullPath);
				Assert.AreNotEqual(fileLocation1.FileName, fileLocation2.FileName);
				Assert.AreNotEqual(fileLocation1, fileLocation2);
				Assert.AreNotEqual(fileLocation1.GetHashCode(), fileLocation2.GetHashCode());
			}
			{
				FileLocation fileLocation1 = new FileLocation(@"C:\blah\thing.txt", "V1", true);
				FileLocation fileLocation2 = new FileLocation(@"c:\BLAH\THING.TXT", "V1", true);
				Assert.AreNotEqual(fileLocation1.FullPath, fileLocation2.FullPath);
				Assert.AreNotEqual(fileLocation1.FileName, fileLocation2.FileName);
				Assert.AreEqual(fileLocation1, fileLocation2);
				Assert.AreEqual(fileLocation1.GetHashCode(), fileLocation2.GetHashCode());
			}
		}

		public static void CompareToAndSorting()
		{
			FileLocation[] expectedOrder = new FileLocation[]
				{
					new FileLocation("fileA", "VolumeA", false),
					new FileLocation("fileC", "VolumeA", false),
					new FileLocation("fileZ", "VolumeB", false),
					new FileLocation("fileA", "VolumeC", false),
					new FileLocation("fileB", "VolumeC", false),
					new FileLocation("fileC", "VolumeC", false),
				};
			FileLocation[] randomOrder = expectedOrder.Select((fl) => new FileLocation(fl.FullPath, fl.VolumeName, fl.IsFromReadOnlyVolume)).ToArray();
			Random random = new Random(123);
			for (int i = 0; i < 10; i++)
			{
				int index1 = random.Next(expectedOrder.Length);
				int index2 = random.Next(expectedOrder.Length);
				FileLocation temp = randomOrder[index1];
				randomOrder[index1] = randomOrder[index2];
				randomOrder[index2] = temp;
			}

			Assert.SequenceNotEquals(expectedOrder, randomOrder);
			Assert.SequenceEquals(expectedOrder, randomOrder.OrderBy((fl) => fl));
			Assert.SequenceEquals(expectedOrder.Reverse(), randomOrder.OrderBy((fl) => fl).Reverse());
			List<FileLocation> randomOrderListSorted = new List<FileLocation>(randomOrder);
			randomOrderListSorted.Sort();
			Assert.SequenceEquals(expectedOrder, randomOrderListSorted);
			randomOrderListSorted.Reverse();
			Assert.SequenceEquals(expectedOrder.Reverse(), randomOrderListSorted);
		}

		#endregion Tests
	}
}
