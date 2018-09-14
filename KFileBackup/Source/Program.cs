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

		private const string catalogFileName = "files.bkc";

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

					Program.log("Cataloging {0}...", directory);
					Program.catalogFilesInDirectory(fileItemCatalog, directory, "*", isFromReadOnlyLocation);
					fileItemCatalog.SaveCatalogToFile(Program.catalogFileName);
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
				Program.log("Done");
				Program.log();
			}
			Console.ReadKey(true);
		}

		private static void catalogFilesInDirectory(FileItemCatalog fileItemCatalog, string directory, string searchPattern, bool isFromReadOnlyLocation)
		{
			Program.log("Finding files...", directory);
			string[] allFiles = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
			Program.log("Found {0} files.", allFiles.Length);

			int processedFileCount = 0;
			int existingFiles = 0;
			int addedFiles = 0;
			int mergedFiles = 0;
			Program.log("Getting file hashes...");
			foreach (string file in allFiles)
			{
				FileItem fileItem;
				try
				{
					fileItem = FileItem.CreateFromPath(file, isFromReadOnlyLocation);
				}
				catch (IOException)
				{
					Program.log("Couldn't access file, ignoring. {0}", file);
					continue;
				}
				AddOrMergeResult addOrMergeResult = fileItemCatalog.AddOrMerge(fileItem);
				if (addOrMergeResult == AddOrMergeResult.None) { existingFiles++; }
				if (addOrMergeResult == AddOrMergeResult.Added) { addedFiles++; }
				if (addOrMergeResult == AddOrMergeResult.Merged) { mergedFiles++; }

				processedFileCount++;
				if ((processedFileCount % 20) == 0) { Program.log("{0:0.0%} - {1} of {2} files...", (double)processedFileCount / (double)allFiles.Length, processedFileCount, allFiles.Length); }
			}
			Program.log("Cataloged files (found {0}: {1} existing, {2} new, {3} merged)", allFiles.Length, existingFiles, addedFiles, mergedFiles);
		}

		#region Helpers

		private static void runTests()
		{
			foreach (Type testSuiteType in Assembly.GetExecutingAssembly().GetTypes()
				.Where((type) => type.Namespace == "KFileBackup.Tests" && type.Name.EndsWith("Test")))
			{
				Program.log("Testing {0}", testSuiteType.Name);
				foreach (MethodInfo testMethod in testSuiteType.GetMethods(BindingFlags.Static | BindingFlags.Public))
				{
					try
					{
						testMethod.Invoke(null, null);
						Program.log("  {0}", testMethod.Name);
					}
					catch (ApplicationException applicationException)
					{
						Program.log("  {0} FAILED: {1}", testMethod.Name, applicationException.Message);
					}
					catch (Exception exception)
					{
						Program.log("  {0} ERROR: {1} - {2}", testMethod.Name, exception.GetType().Name, exception.Message);
					}
				}
			}
		}

		private static void log()
		{
			Program.log(string.Empty);
		}

		private static void log(string message)
		{
			Console.WriteLine(message);
			File.AppendAllText("log.log", string.Format("{0}: {1}{2}", DateTime.Now, message, Environment.NewLine));
		}

		private static void log(string message, params object[] args)
		{
			Program.log(string.Format(message, args));
		}

		#endregion Helpers

		#endregion Methods
	}
}
