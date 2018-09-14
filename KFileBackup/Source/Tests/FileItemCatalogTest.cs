using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFileBackup.Tests
{
	public static class FileItemCatalogTest
	{
		#region Tests

		public static void AddAndTryGetValue()
		{
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				Assert.AreEqual(0, fileItemCatalog1.Count);
				fileItemCatalog1.Add(new FileItem(new Hash(1)));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(new FileItem(new Hash(1)), fileItemCatalog1.Single());
				Assert.IsTrue(fileItemCatalog1.TryGetValue(new Hash(1), out FileItem fileItem1));
				Assert.AreEqual(new FileItem(new Hash(1)), fileItem1);
				Assert.SequenceEquals(new[] { new FileItem(new Hash(1)) }, fileItemCatalog1);

			}
			{
				FileItemCatalog fileItemCatalog2 = new FileItemCatalog();
				Assert.AreEqual(0, fileItemCatalog2.Count);
				fileItemCatalog2.Add(new FileItem(new Hash(22)));
				fileItemCatalog2.Add(new FileItem(new Hash(33)));
				fileItemCatalog2.Add(new FileItem(new Hash(44)));
				Assert.AreEqual(3, fileItemCatalog2.Count);
				{
					Assert.AreEqual(new FileItem(new Hash(22)), fileItemCatalog2.First());
					Assert.IsTrue(fileItemCatalog2.TryGetValue(new Hash(22), out FileItem fileItem22));
					Assert.AreEqual(new FileItem(new Hash(22)), fileItem22);
				}
				{
					Assert.AreEqual(new FileItem(new Hash(33)), fileItemCatalog2.Skip(1).First());
					Assert.IsTrue(fileItemCatalog2.TryGetValue(new Hash(33), out FileItem fileItem33));
					Assert.AreEqual(new FileItem(new Hash(33)), fileItem33);
				}
				{
					Assert.AreEqual(new FileItem(new Hash(44)), fileItemCatalog2.Skip(2).First());
					Assert.IsTrue(fileItemCatalog2.TryGetValue(new Hash(44), out FileItem fileItem44));
					Assert.AreEqual(new FileItem(new Hash(44)), fileItem44);
				}
				Assert.SequenceEquals(new[] { new FileItem(new Hash(22)), new FileItem(new Hash(33)), new FileItem(new Hash(44)) }, fileItemCatalog2);
			}
		}

		public static void AddDuplicate()
		{
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				Assert.AreEqual(0, fileItemCatalog1.Count);
				fileItemCatalog1.Add(new FileItem(new Hash(22)));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(new FileItem(new Hash(22)), fileItemCatalog1.Single());
				Assert.Throws<ArgumentException>(() => fileItemCatalog1.Add(new FileItem(new Hash(22))));
			}
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				Assert.AreEqual(0, fileItemCatalog1.Count);
				fileItemCatalog1.Add(new FileItem(new Hash(22)));
				fileItemCatalog1.Add(new FileItem(new Hash(33)));
				fileItemCatalog1.Add(new FileItem(new Hash(44)));
				Assert.AreEqual(3, fileItemCatalog1.Count);
				Assert.Throws<ArgumentException>(() => fileItemCatalog1.Add(new FileItem(new Hash(33))));
			}
		}

		public static void AddOrMerge()
		{
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				Assert.AreEqual(0, fileItemCatalog1.Count);
				fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134)));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(new FileItem(new Hash(134)), fileItemCatalog1.Single());
				Assert.IsTrue(fileItemCatalog1.TryGetValue(new Hash(134), out FileItem fileItem1));
				Assert.AreEqual(new FileItem(new Hash(134)), fileItem1);
				Assert.SequenceEquals(new[] { new FileItem(new Hash(134)) }, fileItemCatalog1);
			}
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				Assert.AreEqual(0, fileItemCatalog1.Count);
				fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", true)));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(new FileItem(new Hash(134)), fileItemCatalog1.Single());
				Assert.AreEqual(@"C:\file1", fileItemCatalog1.Single().FileLocations.Single().FullPath);
				Assert.IsTrue(fileItemCatalog1.TryGetValue(new Hash(134), out FileItem fileItem1));
				Assert.AreEqual(new FileItem(new Hash(134)), fileItem1);
				Assert.AreEqual(@"C:\file1", fileItem1.FileLocations.Single().FullPath);
				Assert.SequenceEquals(new[] { new FileItem(new Hash(134)) }, fileItemCatalog1);
			}
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", true)));
				fileItemCatalog1.AddOrMerge(new FileItem(new Hash(456), new FileLocation(@"C:\filezzz", false)));
				fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file2", false)));
				Assert.AreEqual(2, fileItemCatalog1.Count);
				Assert.AreEqual(new FileItem(new Hash(134)), fileItemCatalog1.First());
				Assert.AreEqual(new FileItem(new Hash(456)), fileItemCatalog1.Skip(1).First());
				{
					Assert.IsTrue(fileItemCatalog1.TryGetValue(new Hash(134), out FileItem fileItem1));
					Assert.AreEqual(2, fileItem1.FileLocations.Count);
					Assert.AreEqual(@"C:\file1", fileItem1.FileLocations.First().FullPath);
					Assert.AreEqual(true, fileItem1.FileLocations.First().IsFromReadOnlyLocation);
					Assert.AreEqual(@"C:\file2", fileItem1.FileLocations.Skip(1).First().FullPath);
					Assert.AreEqual(false, fileItem1.FileLocations.Skip(1).First().IsFromReadOnlyLocation);
				}
				{
					Assert.IsTrue(fileItemCatalog1.TryGetValue(new Hash(456), out FileItem fileItem2));
					Assert.AreEqual(1, fileItem2.FileLocations.Count);
					Assert.AreEqual(@"C:\filezzz", fileItem2.FileLocations.First().FullPath);
					Assert.AreEqual(false, fileItem2.FileLocations.First().IsFromReadOnlyLocation);
				}
			}
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", true)));
				fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", false)));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(new FileItem(new Hash(134)), fileItemCatalog1.First());
				{
					Assert.IsTrue(fileItemCatalog1.TryGetValue(new Hash(134), out FileItem fileItem1));
					Assert.AreEqual(1, fileItem1.FileLocations.Count);
					Assert.AreEqual(@"C:\file1", fileItem1.FileLocations.First().FullPath);
					Assert.AreEqual(true, fileItem1.FileLocations.First().IsFromReadOnlyLocation);
				}
			}
		}

		public static void SaveAndLoad()
		{
			try
			{
				{
					FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
					fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", true)));
					fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file2", false)));
					fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file3", false)));
					fileItemCatalog1.AddOrMerge(new FileItem(new Hash(999), new FileLocation(@"C:\blah", false)));
					fileItemCatalog1.AddOrMerge(new FileItem(new Hash(456), new FileLocation(@"C:\filezzz", false)));
					fileItemCatalog1.AddOrMerge(new FileItem(new Hash(456), new FileLocation(@"C:\filezzz2", true)));
					fileItemCatalog1.SaveCatalogToFile("test_catalog.catbk");
				}
				{
					FileItemCatalog fileItemCatalog2 = new FileItemCatalog();
					fileItemCatalog2.ReadCatalogFromFile("test_catalog.catbk");
					Assert.AreEqual(3, fileItemCatalog2.Count);
					Assert.AreEqual(new FileItem(new Hash(134)), fileItemCatalog2.First());
					Assert.AreEqual(new FileItem(new Hash(999)), fileItemCatalog2.Skip(1).First());
					Assert.AreEqual(new FileItem(new Hash(456)), fileItemCatalog2.Skip(2).First());
					{
						Assert.IsTrue(fileItemCatalog2.TryGetValue(new Hash(134), out FileItem fileItem1));
						Assert.AreEqual(3, fileItem1.FileLocations.Count);
						Assert.AreEqual(@"C:\file1", fileItem1.FileLocations.First().FullPath);
						Assert.AreEqual(true, fileItem1.FileLocations.First().IsFromReadOnlyLocation);
						Assert.AreEqual(@"C:\file2", fileItem1.FileLocations.Skip(1).First().FullPath);
						Assert.AreEqual(false, fileItem1.FileLocations.Skip(1).First().IsFromReadOnlyLocation);
						Assert.AreEqual(@"C:\file3", fileItem1.FileLocations.Skip(2).First().FullPath);
						Assert.AreEqual(false, fileItem1.FileLocations.Skip(1).First().IsFromReadOnlyLocation);
					}
					{
						Assert.IsTrue(fileItemCatalog2.TryGetValue(new Hash(999), out FileItem fileItem2));
						Assert.AreEqual(1, fileItem2.FileLocations.Count);
						Assert.AreEqual(@"C:\blah", fileItem2.FileLocations.First().FullPath);
						Assert.AreEqual(false, fileItem2.FileLocations.First().IsFromReadOnlyLocation);
					}
					{
						Assert.IsTrue(fileItemCatalog2.TryGetValue(new Hash(456), out FileItem fileItem3));
						Assert.AreEqual(2, fileItem3.FileLocations.Count);
						Assert.AreEqual(@"C:\filezzz", fileItem3.FileLocations.First().FullPath);
						Assert.AreEqual(false, fileItem3.FileLocations.First().IsFromReadOnlyLocation);
						Assert.AreEqual(@"C:\filezzz2", fileItem3.FileLocations.Skip(1).First().FullPath);
						Assert.AreEqual(true, fileItem3.FileLocations.Skip(1).First().IsFromReadOnlyLocation);
					}
				}
			}
			finally
			{
				System.IO.File.Delete("test_catalog.catbk");
			}
		}

		#endregion Tests
	}
}
