using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KCatalog
{
	public class Program
	{
		#region Fields

		private static string logFileName;

		private static readonly IReadOnlyList<string> helpSwitches = new List<string>() { "help", "?", "-h", "--help" };

		private static readonly Dictionary<string, Tuple<Action<Dictionary<string, object>>, string, string>> commands = new Dictionary<string, Tuple<Action<Dictionary<string, object>>, string, string>>(StringComparer.OrdinalIgnoreCase)
		{
			{ "help", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandHelp, "<command>",
				"Gets more detailed help for a specific command and its usage (help <command>).") },

			{ "catalog-create", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandCatalogCreate, "<DirectoryToCatalog>",
				"Catalogs all files in <DirectoryToCatalog> and its subdirectories to create a catalog file. The catalog will be saved as '.kcatalog' in <DirectoryToCatalog>. You will need to recatalog the directory if files are changed. When you recatalog all files are rescanned and the existing .kcatalog is overwritten.") },

			{ "catalog-check", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandCatalogCheck, "<CatalogFile>",
				"Checks all files in the directory of <CatalogFile> and its subdirectories, listing any files that have been added or removed since the catalog was taken. This command does not check file contents or hashes of existing files, it just checks for new/removed files. This only informs you if you need to recatalog (using 'catalog-create').") },

			{ "catalog-compare", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandCatalogCompare, "[--delete] <BaseCatalogFile> <OtherCatalogFile>",
				"Finds all files in <OtherCatalogFile> that are already cataloged by <BaseCatalogFile> (i.e. files in <OtherCatalogFile> that duplicate files in <BaseCatalogFile>). This ignores file paths and file names, it only compares file contents/hashes. This command will list the duplicated files that are found. If --delete is specified the duplicated files will also be deleted from the directory of <OtherCatalogFile>. Be sure both catalogs are up-to-date (using 'catalog-check' and 'catalog-create' as necessary).") },

			{ "dir-search", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandDirectorySearch, "[--norecursive] [--delete] <DirectoryToSearch> <FileNamePattern>",
				"Finds all files in <DirectoryToSearch> and its subdirectories (unless --norecursive is specified) matching <FileNamePattern>. This command will list the files that are found. The pattern is Windows file system pattern matching (not regex). If --delete is specified they will also be deleted. This is useful for purging files like thumbs.db.") },

			{ "dir-findempty", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandDirectoryFindEmpty, "[--delete] <DirectoryToSearch>",
				"Searches all directories in <DirectoryToSearch> and its subdirectories for empty directories. If --delete is specified they will also be deleted. A directory is empty if it contains no files in itself or any of its subdirectories. This will only print the upper-most directory if all of its subdirectories are also empty.") },
		};

		#endregion Fields

		#region Methods

		#region Main

		public static void Main(string[] args)
		{
			try
			{
				if (args.Length > 0)
				{
					string commandName = args[0];
					if (Program.commands.ContainsKey(commandName))
					{
						try
						{
							string[] commandArgs = args.Skip(1).ToArray();
							string[] commandArgsNoLog = commandArgs.Where((c) => !c.Equals("--log", StringComparison.OrdinalIgnoreCase)).ToArray();
							if (!commandArgs.SequenceEqual(commandArgsNoLog))
							{
								Program.logFileName = DateTime.Now.ToString("yyyy-MM-dd_hh-mmtt") + $"_{commandName}.log";
								Program.log(string.Join(" ", args.Select((arg) => $"\"{arg}\"")));
							}
							Dictionary<string, object> parsedArguments = Program.parseArguments(commandArgsNoLog, Program.commands[commandName].Item2);
							Program.commands[commandName].Item1.Invoke(parsedArguments);
						}
						catch (CommandLineArgumentException commandLineArgumentException)
						{
							Console.WriteLine("Invalid command line arguments:");
							Console.WriteLine($"  {commandLineArgumentException.Message}");
							Console.WriteLine();
							Console.Write("Usage: ");
							Program.commandHelp(new Dictionary<string, object>() { { "command", commandName } });
						}
					}
					else
					{
						Console.Write($"Unknown command '{commandName}'. ");
						Program.showHelp();
					}
				}
				else
				{
					Program.showHelp();
				}
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine($"-- Fatal Error --");
				Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");
				Console.Error.WriteLine(exception.StackTrace);
			}

			if (System.Diagnostics.Debugger.IsAttached)
			{
				Console.ReadKey(true);
			}
		}

		private static Dictionary<string, object> parseArguments(string[] args, string format)
		{
			Regex switchRegex = new Regex(@"\[(--[a-zA-Z0-9]+?)\]");
			Regex argumentRegex = new Regex(@"<([a-zA-Z0-9]+?)>");

			string[] allowedSwitches = switchRegex.Matches(format).OfType<Match>().Select((match) => match.Groups[1].Value).ToArray();
			string[] allowedOptions = argumentRegex.Matches(format).OfType<Match>().Select((match) => match.Groups[1].Value).ToArray();

			int nextOption = 0;
			Dictionary<string, object> arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			for (int argIndex = 0; argIndex < args.Length; argIndex++)
			{
				string arg = args[argIndex];
				if (arg.StartsWith("-", StringComparison.OrdinalIgnoreCase))
				{
					if (allowedSwitches.Contains(arg, StringComparer.OrdinalIgnoreCase))
					{
						try
						{
							arguments.Add(arg, true);
						}
						catch (ArgumentException)
						{
							throw new CommandLineArgumentException($"Switch specified more than once: {arg}.");
						}
					}
					else
					{
						throw new CommandLineArgumentException($"Unknown switch: {arg}.");
					}
				}
				else
				{
					if (nextOption >= allowedOptions.Length)
					{
						throw new CommandLineArgumentException($"Too many non-switch options: {arg}.");
					}

					string optionName = allowedOptions[nextOption];
					if (optionName.StartsWith("Directory"))
					{
						if (!arg.EndsWith("/", StringComparison.OrdinalIgnoreCase) && !arg.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
						{
							arg += "\\";
						}
						try
						{
							arguments.Add(optionName, new DirectoryInfo(arg));
						}
						catch (Exception exception)
						{
							throw new CommandLineArgumentException($"Directory is not valid ({exception.Message}): {arg}.");
						}
					}
					else if (optionName.EndsWith("File"))
					{
						try
						{
							arguments.Add(optionName, new FileInfo(arg));
						}
						catch (Exception exception)
						{
							throw new CommandLineArgumentException($"File is not valid ({exception.Message}): {arg}.");
						}
					}
					else
					{
						arguments.Add(optionName, arg);
					}
					nextOption++;
				}
			}

			if (nextOption != allowedOptions.Length)
			{
				throw new CommandLineArgumentException($"Not enough required options specified.");
			}

			return arguments;
		}

		private static void log(string message)
		{
			Console.WriteLine(message);
			if (Program.logFileName != null)
			{
				File.AppendAllLines(Program.logFileName, new string[] { message });
			}
		}

		private static void showHelp()
		{
			Console.WriteLine($"Available commands are:");
			Console.WriteLine();
			foreach (var command in Program.commands)
			{
				Console.WriteLine($"-{command.Key}-");
				Console.WriteLine($"{command.Value.Item3}");
				Console.WriteLine();
			}
			Console.WriteLine("All commands accept the --log switch to write their important output to a time stamped log file in addition to console output.");
			Console.WriteLine($"Type '<command> help' or '<command> ?' for more info.");
		}

		#endregion Main

		#region Commands

		private static void commandHelp(Dictionary<string, object> arguments)
		{
			string commandName = (string)arguments["command"];
			if (Program.commands.ContainsKey(commandName))
			{
				Console.WriteLine($"  {commandName} [--log] {Program.commands[commandName].Item2}");
				Console.WriteLine();
				Console.Write(Program.commands[commandName].Item3);
				Console.WriteLine(" If --log is specified the command will write output to a time stamped log file in addition to console output.");
			}
			else
			{
				Console.Write($"Unknown command '{commandName}'. ");
				Program.showHelp();
			}
		}

		private static void commandCatalogCreate(Dictionary<string, object> arguments)
		{
			DirectoryInfo directoryToCatalog = (DirectoryInfo)arguments["DirectoryToCatalog"];
			if (!directoryToCatalog.Exists) { throw new CommandLineArgumentException("<DirectoryToCatalog>", "Directory does not exist."); }

			Console.Write("Getting files in directory to catalog... ");
			FileInfo[] foundFiles = directoryToCatalog.GetFiles("*", SearchOption.AllDirectories);
			Console.WriteLine($"Found {foundFiles.Length} files.");

			FileInfo catalogFile = new FileInfo(Path.Combine(directoryToCatalog.FullName, ".kcatalog"));
			if (catalogFile.Exists)
			{
				Console.WriteLine($"Catalog file already exists: {catalogFile.FullName}");
				Catalog oldCatalog = Catalog.Read(catalogFile.FullName);
				Console.WriteLine($"It was taken {(DateTime.Now - oldCatalog.CatalogedOn).Days} days ago and contains {oldCatalog.FileInstances.Count} files ({foundFiles.Length - oldCatalog.FileInstances.Count} difference).");
				Console.WriteLine($"Overwrite existing catalog? <yes|no>");
				if (!string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Aborting, nothing overwritten, nothing cataloged.");
					return;
				}
			}

			Console.WriteLine("Cataloging the files...");
			Catalog catalog = Program.createCatalogForDirectory(directoryToCatalog, foundFiles, out List<string> errors);
			catalog.Write(catalogFile.FullName);

			Program.log($"Cataloged {catalog.FileInstances.Count} files in '{directoryToCatalog.FullName}'.");
			if (errors.Any())
			{
				Program.log($"{errors.Count} errors:");
				errors.ForEach((error) => Program.log($"  {error}"));
			}
		}

		private static void commandCatalogCheck(Dictionary<string, object> arguments)
		{
			FileInfo catalogFile = (FileInfo)arguments["CatalogFile"];
			if (!catalogFile.Exists) { throw new CommandLineArgumentException("<CatalogFile>", "Catalog file does not exist."); }

			Catalog catalog = Catalog.Read(catalogFile.FullName);

			Console.Write("Getting files in cataloged directory... ");
			DirectoryInfo catalogedDirectory = catalogFile.Directory;
			Dictionary<string, FileInfo> foundFiles = catalogedDirectory.GetFiles("*", SearchOption.AllDirectories).ToDictionary((file) => file.GetRelativePath(catalogedDirectory), (file) => file, StringComparer.OrdinalIgnoreCase);
			Console.WriteLine($"Found {foundFiles.Count} files.");

			foreach (FileInstance fileInstance in catalog.FileInstances)
			{
				if (!foundFiles.Remove(fileInstance.RelativePath)) { Program.log($"Removed: {fileInstance.RelativePath}"); }
			}

			foreach (string leftOverFile in foundFiles.Keys.OrderBy((s) => s))
			{
				Program.log($"Added  : {leftOverFile}");
			}
		}

		private static void commandCatalogCompare(Dictionary<string, object> arguments)
		{
			bool shouldDelete = arguments.ContainsKey("--delete");

			FileInfo baseCatalogFile = (FileInfo)arguments["BaseCatalogFile"];
			if (!baseCatalogFile.Exists) { throw new CommandLineArgumentException("<BaseCatalogFile>", "Catalog file does not exist."); }
			FileInfo otherCatalogFile = (FileInfo)arguments["OtherCatalogFile"];
			if (!otherCatalogFile.Exists) { throw new CommandLineArgumentException("<OtherCatalogFile>", "Catalog file does not exist."); }

			Catalog baseCatalog = Catalog.Read(baseCatalogFile.FullName);
			Catalog otherCatalog = Catalog.Read(otherCatalogFile.FullName);

			List<FileInstance> fileInstancesToDelete = new List<FileInstance>();
			foreach (FileInstance otherFileInstance in otherCatalog.FileInstances)
			{
				IReadOnlyList<FileInstance> matchingBaseFileInstances = baseCatalog.Find(otherFileInstance.FileContentsHash);
				if (!matchingBaseFileInstances.Any()) { continue; }

				// [Todo]: If we found duplicates and we are to delete the files, make sure their hashes are still up-to-date and identical in case the files were edited

				fileInstancesToDelete.Add(otherFileInstance);
				Program.log(otherFileInstance.RelativePath);
			}
			Program.log($"Found {fileInstancesToDelete.Count} files in '{otherCatalogFile}' duplicating those in '{baseCatalogFile}'.");

			if (shouldDelete && fileInstancesToDelete.Any())
			{
				Console.WriteLine($"Really delete? <yes|no>");
				if (!string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Aborting, nothing deleted.");
					return;
				}

				int deletedCount = 0;
				try
				{
					foreach (FileInstance fileInstance in fileInstancesToDelete)
					{
						string fullPath = Path.Combine(otherCatalogFile.Directory.FullName, fileInstance.RelativePath);
						File.SetAttributes(fullPath, FileAttributes.Normal);
						File.Delete(fullPath);
						deletedCount++;
					}
				}
				finally
				{
					Program.log($"Deleted {deletedCount} files.");
				}
			}
		}

		private static void commandDirectorySearch(Dictionary<string, object> arguments)
		{
			bool shouldDelete = arguments.ContainsKey("--delete");
			bool noRecursive = arguments.ContainsKey("--norecursive");

			string fileNamePattern = (string)arguments["FileNamePattern"];
			DirectoryInfo directoryToSearch = (DirectoryInfo)arguments["DirectoryToSearch"];
			if (!directoryToSearch.Exists) { throw new CommandLineArgumentException("<DirectoryToSearch>", "Directory does not exist."); }

			SearchOption searchOption = noRecursive ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
			FileInfo[] foundFiles = directoryToSearch.GetFiles(fileNamePattern, searchOption);

			foreach (FileInfo file in foundFiles)
			{
				string relativePath = file.GetRelativePath(directoryToSearch);
				Program.log(relativePath);
			}
			Program.log($"Found {foundFiles.Length} files matching '{fileNamePattern}' in '{directoryToSearch.FullName}'.");

			if (shouldDelete && foundFiles.Any())
			{
				Console.WriteLine($"Really delete? <yes|no>");
				if (!string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Aborting, nothing deleted.");
					return;
				}

				int deletedCount = 0;
				try
				{
					foreach (FileInfo file in foundFiles)
					{
						file.Delete();
						deletedCount++;
					}
				}
				finally
				{
					Program.log($"Deleted {deletedCount} files.");
				}
			}
		}

		private static void commandDirectoryFindEmpty(Dictionary<string, object> arguments)
		{
			bool shouldDelete = arguments.ContainsKey("--delete");

			DirectoryInfo directoryToSearch = (DirectoryInfo)arguments["DirectoryToSearch"];
			if (!directoryToSearch.Exists) { throw new CommandLineArgumentException("<DirectoryToSearch>", "Directory does not exist."); }

			List<DirectoryInfo> emptyDirectories = new List<DirectoryInfo>();
			void deleteRecursively(DirectoryInfo directoryInfo)
			{
				if (!directoryInfo.GetFiles("*", SearchOption.AllDirectories).Any())
				{
					emptyDirectories.Add(directoryInfo);
					Program.log(directoryInfo.GetRelativePath(directoryToSearch));
				}
				else
				{
					foreach (DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories("*", SearchOption.TopDirectoryOnly))
					{
						deleteRecursively(subDirectoryInfo);
					}
				}
			}

			deleteRecursively(directoryToSearch);
			Program.log($"Found {emptyDirectories.Count} empty directories in '{directoryToSearch}'.");

			if (shouldDelete && emptyDirectories.Any())
			{
				Console.WriteLine($"Really delete? <yes|no>");
				if (!string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Aborting, nothing deleted.");
					return;
				}

				int deletedCount = 0;
				try
				{
					foreach (DirectoryInfo directoryInfo in emptyDirectories)
					{
						directoryInfo.Delete(recursive: true);
						deletedCount++;
					}
				}
				finally
				{
					Program.log($"Deleted {deletedCount} empty directories.");
				}
			}
		}

		#endregion Commands

		#region Helpers

		/// <summary>
		/// Goes through all files in the <paramref name="baseDirectory"/> and its subdirectories to create a <see cref="Catalog"/>.
		/// </summary>
		private static Catalog createCatalogForDirectory(DirectoryInfo baseDirectory, FileInfo[] allFiles, out List<string> errors)
		{
			errors = new List<string>();
			List<FileInstance> fileInstances = new List<FileInstance>();
			int fileCount = 0;
			foreach (FileInfo file in allFiles)
			{
				string relativePath = file.GetRelativePath(baseDirectory);
				try
				{
					using (FileStream fileStream = File.OpenRead(file.FullName))
					{
						long fileSize = fileStream.Length;
						Hash256 fileContentsHash = Hash256.GetFileContentsHash(fileStream);
						fileInstances.Add(new FileInstance(relativePath, fileSize, fileContentsHash));
					}
				}
				catch (IOException ioException)
				{
					errors.Add($"Couldn't read ({ioException.Message}): {relativePath}");
				}
				fileCount++;
				if ((fileCount % 20) == 0) { Console.WriteLine($"{(double)fileCount / allFiles.Length:P}% ({fileCount} / {allFiles.Length})"); }
			}
			return new Catalog(fileInstances, DateTime.Now);
		}

		#endregion Helpers

		#endregion Methods
	}

	public static class ExtensionMethodHelpers
	{
		#region Methods

		public static string GetRelativePath(this FileSystemInfo fileOrDirectory, DirectoryInfo baseDirectoryInfo)
		{
			return fileOrDirectory.FullName.Substring(baseDirectoryInfo.FullName.Length);
		}

		#endregion Methods
	}

	public class CommandLineArgumentException : ArgumentException
	{
		#region Constructors

		public CommandLineArgumentException(string message) : base(message) { }

		public CommandLineArgumentException(string argumentName, string message) : base(message, argumentName) { }

		#endregion Constructors
	}
}
