using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFileBackup.Tests
{
	public static class FileItemTest
	{
		#region Tests

		public static void HashValue()
		{
			{
				FileItem fileItem1 = new FileItem(new Hash());
				Assert.AreEqual(0, fileItem1.Hash.Value);
				Assert.AreEqual(new Hash(), fileItem1.Hash);
			}
			{
				FileItem fileItem2 = new FileItem(new Hash(1512));
				Assert.AreEqual(1512, fileItem2.Hash.Value);
				Assert.AreEqual(new Hash(1512), fileItem2.Hash);
			}
		}

		public static void EqualsAndGetHashCode()
		{
			{
				FileItem fileItem1 = new FileItem(new Hash());
				FileItem fileItem2 = new FileItem(new Hash());
				Assert.AreEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(new Hash(123));
				FileItem fileItem2 = new FileItem(new Hash(123));
				Assert.AreEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(new Hash(123));
				FileItem fileItem2 = new FileItem(new Hash(456));
				Assert.AreNotEqual(fileItem1, fileItem2);
				Assert.AreNotEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(new Hash(123), new FileLocation("asd", false));
				FileItem fileItem2 = new FileItem(new Hash(123));
				Assert.AreEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(new Hash(123));
				FileItem fileItem2 = new FileItem(new Hash(456), new FileLocation("ffff", true));
				Assert.AreNotEqual(fileItem1, fileItem2);
				Assert.AreNotEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
		}

		public static void FileLocations()
		{
			{
				FileItem fileItem1 = new FileItem(new Hash(), new FileLocation(@"C:\folder\file1", false));
				Assert.AreEqual(1, fileItem1.FileLocations.Count);
				Assert.IsTrue(fileItem1.FileLocations.Contains(new FileLocation(@"C:\folder\file1", false)));
				Assert.IsTrue(fileItem1.FileLocations.Contains(new FileLocation(@"C:\FOLDER\File1", true)));
				Assert.IsFalse(fileItem1.FileLocations.Contains(new FileLocation(@"C:\folder\file2", false)));
				Assert.IsFalse(fileItem1.FileLocations.Contains(new FileLocation(@"C:\FOLDER\File2", true)));
				Assert.AreEqual(@"C:\folder\file1", fileItem1.FileLocations.Single().FullPath);
				Assert.IsFalse(fileItem1.FileLocations.Single().IsFromReadOnlyLocation);
				Assert.AreNotEqual(@"C:\FOLDER\File1", fileItem1.FileLocations.Single().FullPath);
			}
			{
				FileItem fileItem2 = new FileItem(new Hash(), new FileLocation(@"C:\folder\file", true));
				Assert.IsFalse(fileItem2.FileLocations.Add(new FileLocation(@"C:\folder\file", false)));
				Assert.IsFalse(fileItem2.FileLocations.Add(new FileLocation(@"C:\FOLDER\File", false)));
				Assert.AreEqual(1, fileItem2.FileLocations.Count);
				Assert.IsTrue(fileItem2.FileLocations.Contains(new FileLocation(@"C:\folder\file", false)));
				Assert.AreEqual(@"C:\folder\file", fileItem2.FileLocations.Single().FullPath);
				Assert.IsTrue(fileItem2.FileLocations.Single().IsFromReadOnlyLocation);
				Assert.AreNotEqual(@"C:\FOLDER\File", fileItem2.FileLocations.Single().FullPath);
			}
			{
				FileItem fileItem3 = new FileItem(new Hash());
				Assert.AreEqual(0, fileItem3.FileLocations.Count);
				fileItem3.FileLocations.Add(new FileLocation(@"C:\folder\file1", false));
				Assert.AreEqual(1, fileItem3.FileLocations.Count);
				Assert.IsTrue(fileItem3.FileLocations.Contains(new FileLocation(@"C:\folder\file1", false)));
				Assert.IsTrue(fileItem3.FileLocations.Contains(new FileLocation(@"C:\FOLDER\File1", true)));
				Assert.IsFalse(fileItem3.FileLocations.Contains(new FileLocation(@"C:\folder\file2", false)));
				Assert.IsFalse(fileItem3.FileLocations.Contains(new FileLocation(@"C:\FOLDER\File2", true)));
			}
			{
				FileItem fileItem4 = new FileItem(new Hash(), new FileLocation(@"C:\folder\file1", false));
				fileItem4.FileLocations.Add(new FileLocation(@"C:\folder\file2", false));
				fileItem4.FileLocations.Add(new FileLocation(@"C:\folder\file3", false));
				Assert.AreEqual(3, fileItem4.FileLocations.Count);
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\folder\file1", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\folder\file2", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\folder\file3", false)));
				Assert.IsFalse(fileItem4.FileLocations.Contains(new FileLocation(@"C:\folder\file4", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\FOLDER\file1", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\FOLDER\file2", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\FOLDER\file3", false)));
				Assert.IsFalse(fileItem4.FileLocations.Contains(new FileLocation(@"C:\FOLDER\file4", false)));
			}
		}

		public static void CreateFromPath()
		{
			{
				FileItem fileItem1 = FileItem.CreateFromPath(@"C:\Windows\System32\user32.dll", false);
				Assert.AreEqual(Hash.GetFileHash(@"C:\Windows\System32\user32.dll"), fileItem1.Hash);
				Assert.AreEqual(1, fileItem1.FileLocations.Count);
				Assert.IsTrue(fileItem1.FileLocations.Contains(new FileLocation(@"C:\Windows\System32\user32.dll", false)));
				Assert.AreEqual(@"C:\Windows\System32\user32.dll", fileItem1.FileLocations.Single().FullPath);
			}
		}

		#endregion Tests
	}
}
