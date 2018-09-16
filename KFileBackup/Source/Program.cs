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

					DriveInfo drive = DriveInfo.GetDrives().Single((driveInfo) => driveInfo.Name.Equals(Path.GetPathRoot(directory), StringComparison.OrdinalIgnoreCase));
					Program.log($"Cataloging {directory} (on '{drive.VolumeLabel}')...");
					if (isFromReadOnlyLocation) { Program.log("Treating as readonly volume"); }
					fileItemCatalog.CatalogFilesInDirectory(directory, "*", isFromReadOnlyLocation, Program.log);
					fileItemCatalog.SaveCatalogToFile(Program.catalogFileName);
				}
				else if (args.FirstOrDefault() == "check")
				{
					string directory = args.Last();
					if (!Path.IsPathRooted(directory)) { throw new ArgumentException("Directory must be a full, rooted path."); }
					if (!Directory.Exists(directory)) { throw new ArgumentException("Directory does not exist."); }

					if (!File.Exists(Program.catalogFileName)) { throw new ArgumentException("No saved catalog exists, nothing to check against. Run 'catalog' command."); }

					FileItemCatalog fileItemCatalog = new FileItemCatalog();
					Program.log("Reading catalog from saved file...");
					fileItemCatalog.ReadCatalogFromFile(Program.catalogFileName);

					Program.log($"Checking {directory}...");
					fileItemCatalog.CatalogFilesInDirectory(directory, "*", false, Program.log);
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
						Program.log("  {0} FAILED: {1}", testMethod.Name, applicationException.InnerException.Message);
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
