using System;
using System.Collections.Generic;
using System.IO;
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
				Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134))));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(new FileItem(new Hash(134)), fileItemCatalog1.Single());
				Assert.IsTrue(fileItemCatalog1.TryGetValue(new Hash(134), out FileItem fileItem1));
				Assert.AreEqual(new FileItem(new Hash(134)), fileItem1);
				Assert.SequenceEquals(new[] { new FileItem(new Hash(134)) }, fileItemCatalog1);
			}
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				Assert.AreEqual(0, fileItemCatalog1.Count);
				Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", true))));
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
				Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", true))));
				Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(456), new FileLocation(@"C:\filezzz", false))));
				Assert.AreEqual(AddOrMergeResult.Merged, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file2", false))));
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
				Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", true))));
				Assert.AreEqual(AddOrMergeResult.Same, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", true))));
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

		public static void AddOrMergeWithMultipleLocations()
		{
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				FileItem fileItem1 = new FileItem(new Hash(123));
				fileItem1.FileLocations.Add(new FileLocation(@"C:\file1", true));
				fileItem1.FileLocations.Add(new FileLocation(@"C:\file2", false));
				FileItem fileItem2 = new FileItem(new Hash(123));
				fileItem2.FileLocations.Add(new FileLocation(@"C:\file2", false));
				Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(fileItem1));
				Assert.AreEqual(AddOrMergeResult.Same, fileItemCatalog1.AddOrMerge(fileItem2));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(2, fileItemCatalog1.Single().FileLocations.Count);
			}
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				FileItem fileItem1 = new FileItem(new Hash(123));
				fileItem1.FileLocations.Add(new FileLocation(@"C:\file1", true));
				fileItem1.FileLocations.Add(new FileLocation(@"C:\file2", false));
				FileItem fileItem2 = new FileItem(new Hash(123));
				fileItem2.FileLocations.Add(new FileLocation(@"C:\file1", true));
				fileItem2.FileLocations.Add(new FileLocation(@"C:\file2", false));
				Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(fileItem1));
				Assert.AreEqual(AddOrMergeResult.Same, fileItemCatalog1.AddOrMerge(fileItem2));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(2, fileItemCatalog1.Single().FileLocations.Count);
			}
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				FileItem fileItem1 = new FileItem(new Hash(123));
				fileItem1.FileLocations.Add(new FileLocation(@"C:\file1", true));
				fileItem1.FileLocations.Add(new FileLocation(@"C:\file2", false));
				FileItem fileItem2 = new FileItem(new Hash(123));
				fileItem2.FileLocations.Add(new FileLocation(@"C:\file1", true));
				fileItem2.FileLocations.Add(new FileLocation(@"C:\file3", false));
				Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(fileItem1));
				Assert.AreEqual(AddOrMergeResult.Merged, fileItemCatalog1.AddOrMerge(fileItem2));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(3, fileItemCatalog1.Single().FileLocations.Count);
			}
			{
				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				FileItem fileItem1 = new FileItem(new Hash(123));
				fileItem1.FileLocations.Add(new FileLocation(@"C:\file1", true));
				fileItem1.FileLocations.Add(new FileLocation(@"C:\file2", false));
				FileItem fileItem2 = new FileItem(new Hash(123));
				fileItem2.FileLocations.Add(new FileLocation(@"C:\file3", true));
				fileItem2.FileLocations.Add(new FileLocation(@"C:\file4", false));
				Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(fileItem1));
				Assert.AreEqual(AddOrMergeResult.Merged, fileItemCatalog1.AddOrMerge(fileItem2));
				Assert.AreEqual(1, fileItemCatalog1.Count);
				Assert.AreEqual(4, fileItemCatalog1.Single().FileLocations.Count);
			}
		}

		public static void SaveAndLoad()
		{
			try
			{
				{
					FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
					Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file1", true))));
					Assert.AreEqual(AddOrMergeResult.Merged, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file2", false))));
					Assert.AreEqual(AddOrMergeResult.Merged, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(134), new FileLocation(@"C:\file3", false))));
					Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(999), new FileLocation(@"C:\blah", false))));
					Assert.AreEqual(AddOrMergeResult.Added, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(456), new FileLocation(@"C:\filezzz", false))));
					Assert.AreEqual(AddOrMergeResult.Merged, fileItemCatalog1.AddOrMerge(new FileItem(new Hash(456), new FileLocation(@"C:\filezzz2", true))));
					fileItemCatalog1.SaveCatalogToFile("unittestcatalog.bkc");
				}
				{
					FileItemCatalog fileItemCatalog2 = new FileItemCatalog();
					fileItemCatalog2.ReadCatalogFromFile("unittestcatalog.bkc");
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
				File.Delete("unittestcatalog.bkc");
			}
		}

		public static void CatalogFilesInDirectory_AllAdded_ThenAllSame()
		{
			try
			{
				Assert.IsFalse(Directory.Exists(@"fict_ctid"));
				Directory.CreateDirectory(@"fict_ctid");
				Directory.CreateDirectory(@"fict_ctid\folder1");
				File.WriteAllText(@"fict_ctid\file1", "sometext1");
				File.WriteAllText(@"fict_ctid\file2", "sometext2");
				File.WriteAllText(@"fict_ctid\folder1\file3", "sometext3");

				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Same, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Same, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Same, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
			}
			finally
			{
				Directory.Delete(@"fict_ctid", true);
			}
		}

		public static void CatalogFilesInDirectory_AllAdded_ThenMoreAdded()
		{
			try
			{
				Assert.IsFalse(Directory.Exists(@"fict_ctid"));
				Directory.CreateDirectory(@"fict_ctid");
				Directory.CreateDirectory(@"fict_ctid\folder1");
				File.WriteAllText(@"fict_ctid\file1", "sometext1");
				File.WriteAllText(@"fict_ctid\file2", "sometext2");
				File.WriteAllText(@"fict_ctid\folder1\file3", "sometext3");

				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
				File.WriteAllText(@"fict_ctid\file4", "sometext4");
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(4, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Same, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Same, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Same, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file4"]);
					Assert.AreEqual(4, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(3).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file4", fileItemCatalog1.Skip(3).First().FileLocations.First().FullPath);
					}
				}
			}
			finally
			{
				Directory.Delete(@"fict_ctid", true);
			}
		}

		public static void CatalogFilesInDirectory_AllAdded_ThenMerged()
		{
			try
			{
				Assert.IsFalse(Directory.Exists(@"fict_ctid"));
				Directory.CreateDirectory(@"fict_ctid");
				Directory.CreateDirectory(@"fict_ctid\folder1");
				File.WriteAllText(@"fict_ctid\file1", "sometext1");
				File.WriteAllText(@"fict_ctid\file2", "sometext2");
				File.WriteAllText(@"fict_ctid\folder1\file3", "sometext3");

				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
				File.WriteAllText(@"fict_ctid\folder1\file4", "sometext2");
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(4, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Same, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Same, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Same, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(CatalogFileResult.Merged, catalogFileResults1[@"fict_ctid\folder1\file4"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(2, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
						Assert.AreEqual(@"fict_ctid\folder1\file4", fileItemCatalog1.Skip(1).First().FileLocations.Skip(1).First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
			}
			finally
			{
				Directory.Delete(@"fict_ctid", true);
			}
		}

		public static void CheckFilesInDirectory_AllSame()
		{
			try
			{
				Assert.IsFalse(Directory.Exists(@"fict_ctid"));
				Directory.CreateDirectory(@"fict_ctid");
				Directory.CreateDirectory(@"fict_ctid\folder1");
				File.WriteAllText(@"fict_ctid\file1", "sometext1");
				File.WriteAllText(@"fict_ctid\file2", "sometext2");
				File.WriteAllText(@"fict_ctid\folder1\file3", "sometext3");

				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
				{
					var checkFileResults1 = fileItemCatalog1.CheckFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, checkFileResults1.Count);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\folder1\file3"]);
				}
			}
			finally
			{
				Directory.Delete(@"fict_ctid", true);
			}
		}

		public static void CheckFilesInDirectory_NewFileSame()
		{
			try
			{
				Assert.IsFalse(Directory.Exists(@"fict_ctid"));
				Directory.CreateDirectory(@"fict_ctid");
				Directory.CreateDirectory(@"fict_ctid\folder1");
				File.WriteAllText(@"fict_ctid\file1", "sometext1");
				File.WriteAllText(@"fict_ctid\file2", "sometext2");
				File.WriteAllText(@"fict_ctid\folder1\file3", "sometext3");

				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
				{
					File.WriteAllText(@"fict_ctid\folder1\file4", "sometext2");
					var checkFileResults1 = fileItemCatalog1.CheckFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(4, checkFileResults1.Count);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\folder1\file4"]);
				}
			}
			finally
			{
				Directory.Delete(@"fict_ctid", true);
			}
		}

		public static void CheckFilesInDirectory_NewFileDifferent()
		{
			try
			{
				Assert.IsFalse(Directory.Exists(@"fict_ctid"));
				Directory.CreateDirectory(@"fict_ctid");
				Directory.CreateDirectory(@"fict_ctid\folder1");
				File.WriteAllText(@"fict_ctid\file1", "sometext1");
				File.WriteAllText(@"fict_ctid\file2", "sometext2");
				File.WriteAllText(@"fict_ctid\folder1\file3", "sometext3");

				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
				{
					File.WriteAllText(@"fict_ctid\folder1\file4", "sometext4");
					var checkFileResults1 = fileItemCatalog1.CheckFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(4, checkFileResults1.Count);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(CheckFileResult.New, checkFileResults1[@"fict_ctid\folder1\file4"]);
				}
			}
			finally
			{
				Directory.Delete(@"fict_ctid", true);
			}
		}

		public static void CheckFilesInDirectory_ChangedFile()
		{
			try
			{
				Assert.IsFalse(Directory.Exists(@"fict_ctid"));
				Directory.CreateDirectory(@"fict_ctid");
				Directory.CreateDirectory(@"fict_ctid\folder1");
				File.WriteAllText(@"fict_ctid\file1", "sometext1");
				File.WriteAllText(@"fict_ctid\file2", "sometext2");
				File.WriteAllText(@"fict_ctid\folder1\file3", "sometext3");

				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
				{
					File.WriteAllText(@"fict_ctid\file2", "sometextXzzz");
					var checkFileResults1 = fileItemCatalog1.CheckFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, checkFileResults1.Count);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CheckFileResult.New, checkFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\folder1\file3"]);
				}
			}
			finally
			{
				Directory.Delete(@"fict_ctid", true);
			}
		}
		
		public static void CheckFilesInDirectory_ChangedFileIdenticalToAnother()
		{
			try
			{
				Assert.IsFalse(Directory.Exists(@"fict_ctid"));
				Directory.CreateDirectory(@"fict_ctid");
				Directory.CreateDirectory(@"fict_ctid\folder1");
				File.WriteAllText(@"fict_ctid\file1", "sometext1");
				File.WriteAllText(@"fict_ctid\file2", "sometext2");
				File.WriteAllText(@"fict_ctid\folder1\file3", "sometext3");

				FileItemCatalog fileItemCatalog1 = new FileItemCatalog();
				{
					var catalogFileResults1 = fileItemCatalog1.CatalogFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, catalogFileResults1.Count);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CatalogFileResult.Added, catalogFileResults1[@"fict_ctid\folder1\file3"]);
					Assert.AreEqual(3, fileItemCatalog1.Count);
					{
						Assert.AreEqual(1, fileItemCatalog1.First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file1", fileItemCatalog1.First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(1).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\file2", fileItemCatalog1.Skip(1).First().FileLocations.First().FullPath);
					}
					{
						Assert.AreEqual(1, fileItemCatalog1.Skip(2).First().FileLocations.Count);
						Assert.AreEqual(@"fict_ctid\folder1\file3", fileItemCatalog1.Skip(2).First().FileLocations.First().FullPath);
					}
				}
				{
					File.WriteAllText(@"fict_ctid\file2", "sometext3");
					var checkFileResults1 = fileItemCatalog1.CheckFilesInDirectory(@"fict_ctid", "*", false, (str) => { });
					Assert.AreEqual(3, checkFileResults1.Count);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\file1"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\file2"]);
					Assert.AreEqual(CheckFileResult.Exists, checkFileResults1[@"fict_ctid\folder1\file3"]);
				}
			}
			finally
			{
				Directory.Delete(@"fict_ctid", true);
			}
		}

		#endregion Tests
	}
}
