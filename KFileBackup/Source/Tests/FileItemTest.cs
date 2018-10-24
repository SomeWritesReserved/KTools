using System;
using System.Collections.Generic;
using System.IO;
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
				Assert.AreEqual(0, fileItem1.Hash.GetHashCode());
				Assert.AreEqual(new Hash(), fileItem1.Hash);
			}
			{
				FileItem fileItem2 = new FileItem(TestHelper.Hash(1512));
				Assert.AreEqual(1512, fileItem2.Hash.GetHashCode());
				Assert.AreEqual(TestHelper.Hash(1512), fileItem2.Hash);
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
				FileItem fileItem1 = new FileItem(TestHelper.Hash(123));
				FileItem fileItem2 = new FileItem(TestHelper.Hash(123));
				Assert.AreEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(TestHelper.Hash(123));
				FileItem fileItem2 = new FileItem(TestHelper.Hash(456));
				Assert.AreNotEqual(fileItem1, fileItem2);
				Assert.AreNotEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(TestHelper.Hash(123), new FileLocation("asd", "V1", false));
				FileItem fileItem2 = new FileItem(TestHelper.Hash(123));
				Assert.AreEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(TestHelper.Hash(123));
				FileItem fileItem2 = new FileItem(TestHelper.Hash(456), new FileLocation("ffff", "V1", true));
				Assert.AreNotEqual(fileItem1, fileItem2);
				Assert.AreNotEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
		}

		public static void EqualsWithFileSizes()
		{
			{
				FileItem fileItem1 = new FileItem(new Hash(), 1, new FileLocation("a", "b", false));
				FileItem fileItem2 = new FileItem(new Hash());
				Assert.AreEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(new Hash());
				FileItem fileItem2 = new FileItem(new Hash(), 1, new FileLocation("a", "b", false));
				Assert.AreEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(new Hash(), 1, new FileLocation("a", "b", false));
				FileItem fileItem2 = new FileItem(new Hash());
				Assert.AreEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(TestHelper.Hash(123));
				FileItem fileItem2 = new FileItem(TestHelper.Hash(123), 1, new FileLocation("a", "b", false));
				Assert.AreEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(TestHelper.Hash(123), 2, new FileLocation("a", "b", false));
				FileItem fileItem2 = new FileItem(TestHelper.Hash(123), 1, new FileLocation("a", "b", false));
				Assert.AreNotEqual(fileItem1, fileItem2);
				Assert.AreEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
			{
				FileItem fileItem1 = new FileItem(TestHelper.Hash(123), 15, new FileLocation("a", "b", false));
				FileItem fileItem2 = new FileItem(TestHelper.Hash(456), 15, new FileLocation("a", "b", false));
				Assert.AreNotEqual(fileItem1, fileItem2);
				Assert.AreNotEqual(fileItem1.GetHashCode(), fileItem2.GetHashCode());
			}
		}

		public static void FileLocations()
		{
			{
				FileItem fileItem1 = new FileItem(new Hash(), new FileLocation(@"C:\folder\file1", "V1", false));
				Assert.AreEqual(1, fileItem1.FileLocations.Count);
				Assert.IsTrue(fileItem1.FileLocations.Contains(new FileLocation(@"C:\folder\file1", "V1", false)));
				Assert.IsTrue(fileItem1.FileLocations.Contains(new FileLocation(@"C:\FOLDER\File1", "V1", true)));
				Assert.IsFalse(fileItem1.FileLocations.Contains(new FileLocation(@"C:\folder\file2", "V1", false)));
				Assert.IsFalse(fileItem1.FileLocations.Contains(new FileLocation(@"C:\FOLDER\File2", "V1", true)));
				Assert.AreEqual(@"C:\folder\file1", fileItem1.FileLocations.Single().FullPath);
				Assert.IsFalse(fileItem1.FileLocations.Single().IsFromReadOnlyVolume);
				Assert.AreNotEqual(@"C:\FOLDER\File1", fileItem1.FileLocations.Single().FullPath);
			}
			{
				FileItem fileItem2 = new FileItem(new Hash(), new FileLocation(@"C:\folder\file", "V1", true));
				Assert.IsFalse(fileItem2.FileLocations.Add(new FileLocation(@"C:\folder\file", "V1", false)));
				Assert.IsFalse(fileItem2.FileLocations.Add(new FileLocation(@"C:\FOLDER\File", "V1", false)));
				Assert.AreEqual(1, fileItem2.FileLocations.Count);
				Assert.IsTrue(fileItem2.FileLocations.Contains(new FileLocation(@"C:\folder\file", "V1", false)));
				Assert.AreEqual(@"C:\folder\file", fileItem2.FileLocations.Single().FullPath);
				Assert.IsTrue(fileItem2.FileLocations.Single().IsFromReadOnlyVolume);
				Assert.AreNotEqual(@"C:\FOLDER\File", fileItem2.FileLocations.Single().FullPath);
			}
			{
				FileItem fileItem3 = new FileItem(new Hash());
				Assert.AreEqual(0, fileItem3.FileLocations.Count);
				fileItem3.FileLocations.Add(new FileLocation(@"C:\folder\file1", "V1", false));
				Assert.AreEqual(1, fileItem3.FileLocations.Count);
				Assert.IsTrue(fileItem3.FileLocations.Contains(new FileLocation(@"C:\folder\file1", "V1", false)));
				Assert.IsTrue(fileItem3.FileLocations.Contains(new FileLocation(@"C:\FOLDER\File1", "V1", true)));
				Assert.IsFalse(fileItem3.FileLocations.Contains(new FileLocation(@"C:\folder\file2", "V1", false)));
				Assert.IsFalse(fileItem3.FileLocations.Contains(new FileLocation(@"C:\FOLDER\File2", "V1", true)));
			}
			{
				FileItem fileItem4 = new FileItem(new Hash(), new FileLocation(@"C:\folder\file1", "V1", false));
				fileItem4.FileLocations.Add(new FileLocation(@"C:\folder\file2", "V1", false));
				fileItem4.FileLocations.Add(new FileLocation(@"C:\folder\file3", "V1", false));
				fileItem4.FileLocations.Add(new FileLocation(@"C:\folder\file4", "V2", false));
				Assert.AreEqual(4, fileItem4.FileLocations.Count);
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\folder\file1", "V1", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\folder\file2", "V1", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\folder\file3", "V1", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\folder\file4", "V2", false)));
				Assert.IsFalse(fileItem4.FileLocations.Contains(new FileLocation(@"C:\folder\file4", "V1", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\FOLDER\file1", "V1", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\FOLDER\file2", "V1", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\FOLDER\file3", "V1", false)));
				Assert.IsTrue(fileItem4.FileLocations.Contains(new FileLocation(@"C:\FOLDER\file4", "V2", false)));
				Assert.IsFalse(fileItem4.FileLocations.Contains(new FileLocation(@"C:\FOLDER\file4", "V1", false)));
			}
		}

		public static void CreateFromPath()
		{
			{
				FileItem fileItem1 = FileItem.CreateFromPath(@"C:\Windows\System32\user32.dll", "V1", false);
				Assert.AreEqual(Hash.GetFileHash(@"C:\Windows\System32\user32.dll"), fileItem1.Hash);
				Assert.AreEqual(1, fileItem1.FileLocations.Count);
				Assert.IsTrue(fileItem1.FileLocations.Contains(new FileLocation(@"C:\Windows\System32\user32.dll", "V1", false)));
				Assert.IsFalse(fileItem1.FileLocations.Contains(new FileLocation(@"C:\Windows\System32\user32.dll", "V2", false)));
				Assert.AreEqual(@"C:\Windows\System32\user32.dll", fileItem1.FileLocations.Single().FullPath);
				Assert.AreEqual(@"V1", fileItem1.FileLocations.Single().VolumeName);
			}
			{
				try
				{
					Assert.IsFalse(Directory.Exists(@"fit_cfp"));
					Directory.CreateDirectory(@"fit_cfp");
					Directory.CreateDirectory(@"fit_cfp\folder1");
					File.WriteAllText(@"fit_cfp\file1", "file of length16", Encoding.ASCII);
					File.WriteAllText(@"fit_cfp\file2", "only5", Encoding.ASCII);
					File.WriteAllText(@"fit_cfp\folder1\file3", "thisisalongone with at least or actually exactly50", Encoding.ASCII);

					FileItem fileItem1 = FileItem.CreateFromPath(@"fit_cfp\file1", "C:", false);
					Assert.IsTrue(fileItem1.FileSize.HasValue);
					Assert.AreEqual(16, fileItem1.FileSize.Value);
					Assert.AreEqual(1, fileItem1.FileLocations.Count);
					Assert.IsTrue(fileItem1.FileLocations.Contains(new FileLocation(@"fit_cfp\file1", "C:", false)));

					FileItem fileItem2 = FileItem.CreateFromPath(@"fit_cfp\file2", "C:", false);
					Assert.IsTrue(fileItem2.FileSize.HasValue);
					Assert.AreEqual(5, fileItem2.FileSize.Value);
					Assert.AreEqual(1, fileItem2.FileLocations.Count);
					Assert.IsTrue(fileItem2.FileLocations.Contains(new FileLocation(@"fit_cfp\file2", "C:", false)));

					FileItem fileItem3 = FileItem.CreateFromPath(@"fit_cfp\folder1\file3", "C:", false);
					Assert.IsTrue(fileItem3.FileSize.HasValue);
					Assert.AreEqual(50, fileItem3.FileSize.Value);
					Assert.AreEqual(1, fileItem3.FileLocations.Count);
					Assert.IsTrue(fileItem3.FileLocations.Contains(new FileLocation(@"fit_cfp\folder1\file3", "C:", false)));
				}
				finally
				{
					Directory.Delete(@"fit_cfp", true);
				}
			}
		}

		#endregion Tests
	}
}
