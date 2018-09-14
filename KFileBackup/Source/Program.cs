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
		#region Methods

		public static void Main(string[] args)
		{
			try
			{
				Program.log("Starting new process");
				Program.runTests();
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

		private static FileItemCatalog catalogFilesInDirectory(string directory, string searchPattern, bool isFromReadOnlyLocation)
		{
			Program.log("Finding files...");
			string[] allFiles = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
			Program.log("Found {0} files.", allFiles.Length);

			Program.log("Getting file hashes...");
			FileItemCatalog fileItemCatalog = new FileItemCatalog();
			int count = 0;
			foreach (string file in allFiles)
			{
				FileItem newFileItem;
				try
				{
					newFileItem = FileItem.CreateFromPath(file, isFromReadOnlyLocation);
				}
				catch (IOException)
				{
					Program.log("Couldn't access file, ignoring. {0}", file);
					continue;
				}
				if (fileItemCatalog.TryGetValue(newFileItem.Hash, out FileItem existingFileItem))
				{
					existingFileItem.FileLocations.Add(newFileItem.FileLocations.Single());
				}
				else
				{
					fileItemCatalog.Add(newFileItem);
				}
				count++;
				if ((count % 20) == 0) { Program.log("{0:0.0%} - {1} of {2} files...", (double)count / (double)allFiles.Length, count, allFiles.Length); }
			}
			return fileItemCatalog;
		}

		#region Helpers

		private static void runTests()
		{
			foreach (Type testSuiteType in Assembly.GetExecutingAssembly().GetTypes()
				.Where((type) => type.Namespace == "KFileBackup.Tests" && type.Name != "Assert"))
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
