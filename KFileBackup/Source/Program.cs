using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KFileBackup
{
	public class Program
	{
		#region Fields

		private const string catalogFileName = "Catalog.bkc";
		private static string logFileName = "Output.log";

		#endregion Fields

		#region Methods

		public static void Main(string[] args)
		{
			try
			{
				Program.log("Running args: {0}", string.Join(" ", args.Select((arg) => $"\"{arg}\"")));
				if (args.FirstOrDefault() == "test")
				{
					Program.runTests();
				}
				else if (args.FirstOrDefault() == "catalog")
				{
					Program.logFileName = "Catalog.log";

					bool isFromReadOnlyVolume = args.Contains("--readonly");
					string directory = args.Last();
					if (!Path.IsPathRooted(directory)) { throw new ArgumentException("Directory must be a full, rooted path."); }
					if (!Directory.Exists(directory)) { throw new ArgumentException("Directory does not exist."); }

					FileItemCatalog fileItemCatalog = new FileItemCatalog();
					if (File.Exists(Program.catalogFileName))
					{
						Program.log("Reading catalog from saved file...");
						fileItemCatalog.ReadCatalogFromFile(Program.catalogFileName);
					}

					string volumeName;
					if (args.Any((arg) => arg.StartsWith("--volumename:", StringComparison.OrdinalIgnoreCase)))
					{
						volumeName = args.Single((arg) => arg.StartsWith("--volumename:")).Substring("--volumename:".Length);
					}
					else
					{
						DriveInfo drive = DriveInfo.GetDrives().Single((driveInfo) => driveInfo.Name.Equals(Path.GetPathRoot(directory), StringComparison.OrdinalIgnoreCase));
						volumeName = drive.VolumeLabel;
					}

					Program.log($"Cataloging {directory} (on '{volumeName}', format version {FileItemCatalog.FileFormatVersion})...");
					if (isFromReadOnlyVolume) { Program.log("Treating as readonly volume"); }
					fileItemCatalog.CatalogFilesInDirectory(directory, "*", volumeName, isFromReadOnlyVolume, Program.log);
					fileItemCatalog.WriteCatalogToFile(Program.catalogFileName);
				}
				else if (args.FirstOrDefault() == "check")
				{
					bool showAllFiles = args.Contains("--showall");
					bool shouldDeleteDuplicates = args.Contains("--delete");
					string directory = args.Last();
					if (!Path.IsPathRooted(directory)) { throw new ArgumentException("Directory must be a full, rooted path."); }
					if (!Directory.Exists(directory)) { throw new ArgumentException("Directory does not exist."); }

					if (!File.Exists(Program.catalogFileName)) { throw new ArgumentException("No saved catalog exists, nothing to check against. Run 'catalog' command."); }

					if (shouldDeleteDuplicates)
					{
						Program.log("Really delete files already in catalog?");
						if (Console.ReadKey().Key != ConsoleKey.Y) { throw new OperationCanceledException("User cancelled the --delete option."); }
						Program.log("Will delete all files already in catalog");
						showAllFiles = true; // --delete implies --showall so the hashes of all deleted files are logged
					}

					FileItemCatalog fileItemCatalog = new FileItemCatalog();
					Program.log("Reading catalog from saved file...");
					fileItemCatalog.ReadCatalogFromFile(Program.catalogFileName);

					Program.log($"Checking {directory}...");
					List<FileResult<CheckFileResult>> checkFileResults = fileItemCatalog.CheckFilesInDirectory(directory, "*", showAllFiles, Program.log);
					if (shouldDeleteDuplicates)
					{
						Program.log("Deleting files already in catalog...");
						foreach (string file in checkFileResults.Where((res) => res.Result == CheckFileResult.Exists)
							.Select((res) => res.File))
						{
							Program.log(file);
							File.Delete(file);
						}
					}
				}
				else if (args.FirstOrDefault() == "compare")
				{
					bool showAllFiles = args.Contains("--showall");
					bool shouldDeleteDuplicates = args.Contains("--delete");
					string baseDirectory = args[args.Length - 2];
					string compareDirectory = args[args.Length - 1];
					if (!Path.IsPathRooted(baseDirectory)) { throw new ArgumentException("Base directory must be a full, rooted path."); }
					if (!Directory.Exists(baseDirectory)) { throw new ArgumentException("Base directory does not exist."); }
					if (!Path.IsPathRooted(compareDirectory)) { throw new ArgumentException("Compare directory must be a full, rooted path."); }
					if (!Directory.Exists(compareDirectory)) { throw new ArgumentException("Compare directory does not exist."); }

					if (shouldDeleteDuplicates)
					{
						Program.log("Really delete files duplicated in compare directory?");
						if (Console.ReadKey().Key != ConsoleKey.Y) { throw new OperationCanceledException("User cancelled the --delete option."); }
						Program.log("Will delete all files duplicated in compare directory");
						showAllFiles = true; // --delete implies --showall so the hashes of all deleted files are logged
					}

					FileItemCatalog fileItemCatalog = new FileItemCatalog();
					Program.log($"Cataloging base directory {baseDirectory}...");
					fileItemCatalog.CatalogFilesInDirectory(baseDirectory, "*", "Base directory", false, (str) => { });

					Program.log($"Comparing compare directory {compareDirectory}...");
					List<FileResult<CheckFileResult>> checkFileResults = fileItemCatalog.CheckFilesInDirectory(compareDirectory, "*", showAllFiles, Program.log);
					if (shouldDeleteDuplicates)
					{
						Program.log("Deleting files already in catalog...");
						foreach (string file in checkFileResults.Where((res) => res.Result == CheckFileResult.Exists)
							.Select((res) => res.File))
						{
							Program.log(file);
							File.Delete(file);
						}
					}
				}
				else if (args.FirstOrDefault() == "view")
				{
					string fileHashHex = args.Last();
					if (!Hash.TryParse(fileHashHex, out Hash fileHash)) { throw new ArgumentException($"{fileHashHex} is not a hash."); }

					if (!File.Exists(Program.catalogFileName)) { throw new ArgumentException("No saved catalog exists, nothing to view. Run 'catalog' command."); }

					FileItemCatalog fileItemCatalog = new FileItemCatalog();
					Program.log("Reading catalog from saved file...");
					fileItemCatalog.ReadCatalogFromFile(Program.catalogFileName);

					Program.log($"Viewing hash '{fileHashHex}'");
					if (!fileItemCatalog.TryGetValue(fileHash, out FileItem fileItem))
					{
						Program.log($" No files.");
					}
					else
					{
						foreach (FileLocation fileLocation in fileItem.FileLocations)
						{
							Program.log(" {0}{1}\t{2}", fileLocation.IsFromReadOnlyVolume ? "*" : " ", fileLocation.VolumeName, fileLocation.FullPath);
						}
					}
				}
				else if (args.FirstOrDefault() == "ls")
				{
					bool showAllFileLocations = args.Contains("--showall");
					if (!File.Exists(Program.catalogFileName)) { throw new ArgumentException("No saved catalog exists, nothing to list. Run 'catalog' command."); }

					FileItemCatalog fileItemCatalog = new FileItemCatalog();
					Program.log("Reading catalog from saved file...");
					fileItemCatalog.ReadCatalogFromFile(Program.catalogFileName);

					Program.log($"There are {fileItemCatalog.Count} unique file items cataloged.");
					foreach (FileItem fileItem in fileItemCatalog.OrderBy((fi) => fi.FileLocations.First()))
					{
						Program.log($"{fileItem.Hash}:");
						foreach (FileLocation fileLocation in fileItem.FileLocations.OrderBy((fl) => fl))
						{
							Program.log(" {0}{1}\t{2}", fileLocation.IsFromReadOnlyVolume ? "*" : " ", fileLocation.VolumeName, fileLocation.FullPath);
							if (!showAllFileLocations) { break; }
						}
					}
				}
				else if (args.FirstOrDefault() == "find")
				{
					string fileToFind = args.Last();
					if (!File.Exists(Program.catalogFileName)) { throw new ArgumentException("No saved catalog exists, nothing to list. Run 'catalog' command."); }

					FileItemCatalog fileItemCatalog = new FileItemCatalog();
					Program.log("Reading catalog from saved file...");
					fileItemCatalog.ReadCatalogFromFile(Program.catalogFileName);

					var matchedFileItems = fileItemCatalog.Where((fi) => fi.FileLocations.Any((fl) => fl.FullPath.IndexOf(fileToFind, StringComparison.OrdinalIgnoreCase) >= 0));
					Program.log($"Found {matchedFileItems.Count()} matching file items.");
					foreach (FileItem fileItem in matchedFileItems)
					{
						Program.log($"{fileItem.Hash}:");
						foreach (FileLocation fileLocation in fileItem.FileLocations.OrderBy((fl) => fl))
						{
							Program.log(" {0}{1}\t{2}", fileLocation.IsFromReadOnlyVolume ? "*" : " ", fileLocation.VolumeName, fileLocation.FullPath);
						}
					}
				}
				else if (args.FirstOrDefault() == "gc")
				{
				}
				else if (args.FirstOrDefault() == "selfcompare")
				{
					bool shouldDeleteDuplicates = args.Contains("--delete");
					string directory = args.Last();
					if (!Path.IsPathRooted(directory)) { throw new ArgumentException("Directory must be a full, rooted path."); }
					if (!Directory.Exists(directory)) { throw new ArgumentException("Directory does not exist."); }

					if (shouldDeleteDuplicates)
					{
						Program.log("Really delete files duplicated in directory?");
						if (Console.ReadKey().Key != ConsoleKey.Y) { throw new OperationCanceledException("User cancelled the --delete option."); }
						Program.log("Will delete all files duplicated in directory");
					}

					FileItemCatalog fileItemCatalog = new FileItemCatalog();
					Program.log($"Cataloging base directory {directory}...");
					List<FileResult<CatalogFileResult>> catalogFileResults = fileItemCatalog.CatalogFilesInDirectory(directory, "*", "Self directory", false, Program.log);
					if (shouldDeleteDuplicates)
					{
						Program.log("Deleting files duplicated in the directory...");
						foreach (string file in catalogFileResults.Where((res) => res.Result == CatalogFileResult.Merged)
							.Select((res) => res.File))
						{
							Program.log(file);
							File.Delete(file);
						}
					}
				}
			}
			catch (Exception exception)
			{
				Program.log("===Fatal error===");
				Program.log(exception.GetType().Name);
				Program.log(exception.Message);
				Program.log(exception.StackTrace);
				Program.log("===Fatal error===");
			}
			finally
			{
				Program.log();
			}
#if DEBUG
			Console.ReadKey(true);
#endif
		}

		#region Helpers

		private static void runTests()
		{
			int failedTestCount = 0;
			int problemCount = 0;
			foreach (Type testSuiteType in Assembly.GetExecutingAssembly().GetTypes()
				.Where((type) => type.Namespace == "KFileBackup.Tests" && type.Name.EndsWith("Test")))
			{
				Program.log("Testing {0}", testSuiteType.Name);
				foreach (MethodInfo testMethod in testSuiteType.GetMethods(BindingFlags.Static | BindingFlags.Public))
				{
					try
					{
						testMethod.Invoke(null, null);
						Program.log("   {0}", testMethod.Name);
					}
					catch (ApplicationException applicationException)
					{
						Program.log(" ! {0} failed: {1}", testMethod.Name, applicationException.InnerException.Message);
						failedTestCount++;
					}
					catch (Exception exception)
					{
						Program.log(" # {0} PROBLEM: {1} - {2}", testMethod.Name, exception.GetType().Name, exception.Message);
						problemCount++;
					}
				}
			}
			Program.log($"Done. {failedTestCount} failures, {problemCount} problems.");
		}

		private static void log()
		{
			Program.log(string.Empty);
		}

		private static void log(string message)
		{
			Console.WriteLine(message);
			File.AppendAllText(Program.logFileName, string.Format("{0:MM/dd/yyyy hh:mm:ss tt}: {1}{2}", DateTime.Now, message, Environment.NewLine));
		}

		private static void log(string message, params object[] args)
		{
			Program.log(string.Format(message, args));
		}

		#endregion Helpers

		#endregion Methods
	}
}
