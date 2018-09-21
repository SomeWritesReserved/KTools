﻿using System;
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

		private const string catalogFileName = "files.bkc";
		private static string logFileName = "log.log";

		#endregion Fields

		#region Methods

		public static void Main(string[] args)
		{
			try
			{
				if (args.FirstOrDefault() == "test")
				{
					Program.log("Running tests...");
					Program.runTests();
				}
				else if (args.FirstOrDefault() == "catalog")
				{
					Program.logFileName = "log-Catalog.log";

					bool isFromReadOnlyLocation = args.Contains("--readonly");
					string directory = args.Last();
					if (!Path.IsPathRooted(directory)) { throw new ArgumentException("Directory must be a full, rooted path."); }
					if (!Directory.Exists(directory)) { throw new ArgumentException("Directory does not exist."); }

					FileItemCatalog fileItemCatalog = new FileItemCatalog();
					if (File.Exists(Program.catalogFileName))
					{
						Program.log("Reading catalog from saved file...");
						fileItemCatalog.ReadCatalogFromFile(Program.catalogFileName);
					}

					DriveInfo drive = DriveInfo.GetDrives().Single((driveInfo) => driveInfo.Name.Equals(Path.GetPathRoot(directory), StringComparison.OrdinalIgnoreCase));
					Program.log($"Cataloging {directory} (on '{drive.VolumeLabel}')...");
					if (isFromReadOnlyLocation) { Program.log("Treating as readonly volume"); }
					fileItemCatalog.CatalogFilesInDirectory(directory, "*", isFromReadOnlyLocation, Program.log);
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
					Dictionary<string, CheckFileResult> checkFileResults = fileItemCatalog.CheckFilesInDirectory(directory, "*", showAllFiles, Program.log);
					if (shouldDeleteDuplicates)
					{
						Program.log("Deleting files already in catalog...");
						foreach (string file in checkFileResults.Where((kvp) => kvp.Value == CheckFileResult.Exists)
							.Select((kvp) => kvp.Key))
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
					Program.log($"Checking base directory {baseDirectory}...");
					fileItemCatalog.CatalogFilesInDirectory(baseDirectory, "*", false, (str) => { });

					Program.log($"Comparing compare directory {compareDirectory}...");
					Dictionary<string, CheckFileResult> checkFileResults = fileItemCatalog.CheckFilesInDirectory(compareDirectory, "*", showAllFiles, Program.log);
					if (shouldDeleteDuplicates)
					{
						Program.log("Deleting files already in catalog...");
						foreach (string file in checkFileResults.Where((kvp) => kvp.Value == CheckFileResult.Exists)
							.Select((kvp) => kvp.Key))
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
							Program.log(" {0}{1}", fileLocation.IsFromReadOnlyLocation ? "*" : " ", fileLocation.FullPath);
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

					foreach (FileItem fileItem in fileItemCatalog.OrderBy((fi) => fi.FileLocations.First()))
					{
						Program.log($"{fileItem.Hash}:");
						foreach (FileLocation fileLocation in fileItem.FileLocations.OrderBy((fl) => fl))
						{
							Program.log(" {0}{1}", fileLocation.IsFromReadOnlyLocation ? "*" : " ", fileLocation.FullPath);
							if (!showAllFileLocations) { break; }
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
