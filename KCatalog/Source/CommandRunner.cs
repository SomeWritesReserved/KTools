using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KCatalog
{
	/// <summary>
	/// This is the class that actually runs KCatalog commands and does the logic.
	/// </summary>
	/// <remarks>
	/// This class must never use System.Console (instead use <see cref="outputWriter"/> and <see cref="inputReader"/>)
	/// and it must never use System.IO types (instead use <see cref="fileSystem"/>) so that it can be unit testable.
	/// </remarks>
	public class CommandRunner
	{
		#region Fields

		private readonly IFileSystem fileSystem;
		private readonly System.IO.TextWriter outputWriter;
		private readonly System.IO.TextReader inputReader;
		private string logFileName;

		private readonly IReadOnlyList<string> helpSwitches = new List<string>() { "help", "?", "-h", "--help" };
		private readonly Dictionary<string, Tuple<Action<Dictionary<string, object>>, string, string>> commands;

		#endregion Fields

		#region Constructors

		public CommandRunner(IFileSystem fileSystem, System.IO.TextWriter outputWriter, System.IO.TextReader inputReader)
		{
			this.fileSystem = fileSystem;
			this.outputWriter = outputWriter;
			this.inputReader = inputReader;

			this.commands = new Dictionary<string, Tuple<Action<Dictionary<string, object>>, string, string>>(StringComparer.OrdinalIgnoreCase)
			{
				{ "help", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandHelp, "<command>",
					"Gets more detailed help for a specific command and its usage (help <command>).") },

				{ "catalog-create", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogCreate, "<DirectoryToCatalog>",
					"Catalogs all files in <DirectoryToCatalog> and its subdirectories to create a catalog file. The catalog will be saved as '.kcatalog' in <DirectoryToCatalog>. You will need to recatalog the directory if files are changed. When you recatalog all files are rescanned and the existing .kcatalog is overwritten.") },

				{ "catalog-check", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogCheck, "<CatalogFile>",
					"Checks all files in the directory of <CatalogFile> and its subdirectories, listing any files that have been added or removed since the catalog was taken. This command does not check file contents or hashes of existing files, it just checks for new/removed files. This only informs you if you need to recatalog (using 'catalog-create').") },

				{ "catalog-compare", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogCompare, "[--delete] <BaseCatalogFile> <OtherCatalogFile>",
					"Finds all files in <OtherCatalogFile> that are already cataloged by <BaseCatalogFile> (i.e. files in <OtherCatalogFile> that duplicate files in <BaseCatalogFile>). This ignores file paths and file names, it only compares file contents/hashes. This command will list the duplicated files that are found. If --delete is specified the duplicated files will also be deleted from the directory of <OtherCatalogFile>. Be sure both catalogs are up-to-date (using 'catalog-check' and 'catalog-create' as necessary).") },

				{ "dir-search", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandDirectorySearch, "[--norecursive] [--delete] <DirectoryToSearch> <FileNamePattern>",
					"Finds all files in <DirectoryToSearch> and its subdirectories (unless --norecursive is specified) matching <FileNamePattern>. This command will list the files that are found. The pattern is Windows file system pattern matching (not regex). If --delete is specified they will also be deleted. This is useful for purging files like thumbs.db.") },

				{ "dir-findempty", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandDirectoryFindEmpty, "[--delete] <DirectoryToSearch>",
					"Searches all directories in <DirectoryToSearch> and its subdirectories for empty directories. If --delete is specified they will also be deleted. A directory is empty if it contains no files in itself or any of its subdirectories. This will only print the upper-most directory if all of its subdirectories are also empty.") },
			};
		}

		#endregion Constructors

		#region Methods

		#region Running

		public void Run(string[] args)
		{
			if (args.Length > 0)
			{
				string commandName = args[0];
				if (this.commands.ContainsKey(commandName))
				{
					try
					{
						string[] commandArgs = args.Skip(1).ToArray();
						string[] commandArgsNoLog = commandArgs.Where((c) => !c.Equals("--log", StringComparison.OrdinalIgnoreCase)).ToArray();
						if (!commandArgs.SequenceEqual(commandArgsNoLog))
						{
							this.logFileName = DateTime.Now.ToString("yyyy-MM-dd_hh-mmtt") + $"_{commandName}.log";
							this.log(string.Join(" ", args.Select((arg) => $"\"{arg}\"")));
						}
						Dictionary<string, object> parsedArguments = this.parseArguments(fileSystem, commandArgsNoLog, this.commands[commandName].Item2);
						this.commands[commandName].Item1.Invoke(parsedArguments);
					}
					catch (CommandLineArgumentException commandLineArgumentException)
					{
						this.outputWriter.WriteLine("Invalid command line arguments:");
						this.outputWriter.WriteLine($"  {commandLineArgumentException.Message}");
						this.outputWriter.WriteLine();
						this.outputWriter.Write("Usage: ");
						this.commandHelp(new Dictionary<string, object>() { { "command", commandName } });
						throw;
					}
				}
				else
				{
					this.outputWriter.Write($"Unknown command '{commandName}'. ");
					this.showHelp();
				}
			}
			else
			{
				this.showHelp();
			}
		}

		private Dictionary<string, object> parseArguments(IFileSystem fileSystem, string[] args, string format)
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
							arguments.Add(optionName, fileSystem.DirectoryInfo.FromDirectoryName(arg));
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
							arguments.Add(optionName, fileSystem.FileInfo.FromFileName(arg));
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

		private void log(string message)
		{
			this.outputWriter.WriteLine(message);
			if (this.logFileName != null)
			{
				this.fileSystem.File.AppendAllLines(this.logFileName, new string[] { message });
			}
		}

		private void showHelp()
		{
			this.outputWriter.WriteLine($"Available commands are:");
			this.outputWriter.WriteLine();
			foreach (var command in this.commands)
			{
				this.outputWriter.WriteLine($"{command.Key}");
				this.outputWriter.WriteLine(new string('-', command.Key.Length));
				this.outputWriter.WriteLine($"{command.Value.Item3}");
				this.outputWriter.WriteLine();
			}
			this.outputWriter.WriteLine("All commands accept the --log switch to write their important output to a time stamped log file in addition to standard output.");
			this.outputWriter.WriteLine($"Type '<command> help' or '<command> ?' for more info.");
		}

		private void commandHelp(Dictionary<string, object> arguments)
		{
			string commandName = (string)arguments["command"];
			if (this.commands.ContainsKey(commandName))
			{
				this.outputWriter.WriteLine($"  {commandName} [--log] {this.commands[commandName].Item2}");
				this.outputWriter.WriteLine();
				this.outputWriter.Write(this.commands[commandName].Item3);
				this.outputWriter.WriteLine(" If --log is specified the command will write output to a time stamped log file in addition to standard output.");
			}
			else
			{
				this.outputWriter.Write($"Unknown command '{commandName}'. ");
				this.showHelp();
			}
		}

		#endregion Running

		#region Commands

		private void commandCatalogCreate(Dictionary<string, object> arguments)
		{
			IDirectoryInfo directoryToCatalog = (IDirectoryInfo)arguments["DirectoryToCatalog"];
			if (!directoryToCatalog.Exists) { throw new CommandLineArgumentException("<DirectoryToCatalog>", "Directory does not exist."); }

			this.outputWriter.Write("Getting files in directory to catalog... ");
			IFileInfo[] foundFiles = directoryToCatalog.GetFiles("*", System.IO.SearchOption.AllDirectories);
			this.outputWriter.WriteLine($"Found {foundFiles.Length} files.");

			IFileInfo catalogFile = this.fileSystem.FileInfo.FromFileName(this.fileSystem.Path.Combine(directoryToCatalog.FullName, ".kcatalog"));
			if (catalogFile.Exists)
			{
				this.outputWriter.WriteLine($"Catalog file already exists: {catalogFile.FullName}");
				Catalog oldCatalog = Catalog.Read(catalogFile);
				this.outputWriter.WriteLine($"It was taken {(DateTime.Now - oldCatalog.CatalogedOn).Days} days ago and contains {oldCatalog.FileInstances.Count} files ({foundFiles.Length - oldCatalog.FileInstances.Count} difference).");
				this.outputWriter.WriteLine($"Overwrite existing catalog? <yes|no>");
				if (!string.Equals(this.inputReader.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
				{
					throw new OperationCanceledException("Aborting, nothing overwritten, nothing cataloged.");
				}
			}

			this.outputWriter.WriteLine("Cataloging the files...");
			Catalog catalog = this.createCatalogForDirectory(directoryToCatalog, foundFiles, out List<string> errors);
			catalog.Write(catalogFile);

			this.log($"Cataloged {catalog.FileInstances.Count} files in '{directoryToCatalog.FullName}'.");
			if (errors.Any())
			{
				this.log($"{errors.Count} errors:");
				errors.ForEach((error) => this.log($"  {error}"));
			}
		}

		private void commandCatalogCheck(Dictionary<string, object> arguments)
		{
			IFileInfo catalogFile = (IFileInfo)arguments["CatalogFile"];
			if (!catalogFile.Exists) { throw new CommandLineArgumentException("<CatalogFile>", "Catalog file does not exist."); }

			Catalog catalog = Catalog.Read(catalogFile);

			this.outputWriter.Write("Getting files in cataloged directory... ");
			IDirectoryInfo catalogedDirectory = catalogFile.Directory;
			Dictionary<string, IFileInfo> foundFiles = catalogedDirectory.GetFiles("*", System.IO.SearchOption.AllDirectories).ToDictionary((file) => file.GetRelativePath(catalogedDirectory), (file) => file, StringComparer.OrdinalIgnoreCase);
			this.outputWriter.WriteLine($"Found {foundFiles.Count} files.");

			foreach (FileInstance fileInstance in catalog.FileInstances)
			{
				if (!foundFiles.Remove(fileInstance.RelativePath)) { this.log($"Removed: {fileInstance.RelativePath}"); }
			}

			foreach (string leftOverFile in foundFiles.Keys.OrderBy((s) => s))
			{
				this.log($"Added  : {leftOverFile}");
			}
		}

		private void commandCatalogCompare(Dictionary<string, object> arguments)
		{
			bool shouldDelete = arguments.ContainsKey("--delete");

			IFileInfo baseCatalogFile = (IFileInfo)arguments["BaseCatalogFile"];
			if (!baseCatalogFile.Exists) { throw new CommandLineArgumentException("<BaseCatalogFile>", "Catalog file does not exist."); }
			IFileInfo otherCatalogFile = (IFileInfo)arguments["OtherCatalogFile"];
			if (!otherCatalogFile.Exists) { throw new CommandLineArgumentException("<OtherCatalogFile>", "Catalog file does not exist."); }

			Catalog baseCatalog = Catalog.Read(baseCatalogFile);
			Catalog otherCatalog = Catalog.Read(otherCatalogFile);

			List<FileInstance> fileInstancesToDelete = new List<FileInstance>();
			foreach (FileInstance otherFileInstance in otherCatalog.FileInstances)
			{
				IReadOnlyList<FileInstance> matchingBaseFileInstances = baseCatalog.FindFiles(otherFileInstance.FileContentsHash);
				if (!matchingBaseFileInstances.Any()) { continue; }

				// [Todo]: If we found duplicates and we are to delete the files, make sure their hashes are still up-to-date and identical in case the files were edited

				fileInstancesToDelete.Add(otherFileInstance);
				this.log(otherFileInstance.RelativePath);
			}
			this.log($"Found {fileInstancesToDelete.Count} files in '{otherCatalogFile}' duplicating those in '{baseCatalogFile}'.");

			if (shouldDelete && fileInstancesToDelete.Any())
			{
				this.outputWriter.WriteLine($"Really delete? <yes|no>");
				if (!string.Equals(this.inputReader.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
				{
					throw new OperationCanceledException("Aborting, nothing deleted.");
				}

				int deletedCount = 0;
				try
				{
					foreach (FileInstance fileInstance in fileInstancesToDelete)
					{
						string fullPath = this.fileSystem.Path.Combine(otherCatalogFile.Directory.FullName, fileInstance.RelativePath);
						IFileInfo fileInfo = this.fileSystem.FileInfo.FromFileName(fullPath);
						fileInfo.Attributes = System.IO.FileAttributes.Normal;
						fileInfo.Delete();
						deletedCount++;
					}
				}
				finally
				{
					this.log($"Deleted {deletedCount} files.");
				}
			}
		}

		private void commandDirectorySearch(Dictionary<string, object> arguments)
		{
			bool shouldDelete = arguments.ContainsKey("--delete");
			bool noRecursive = arguments.ContainsKey("--norecursive");

			string fileNamePattern = (string)arguments["FileNamePattern"];
			IDirectoryInfo directoryToSearch = (IDirectoryInfo)arguments["DirectoryToSearch"];
			if (!directoryToSearch.Exists) { throw new CommandLineArgumentException("<DirectoryToSearch>", "Directory does not exist."); }

			System.IO.SearchOption searchOption = noRecursive ? System.IO.SearchOption.TopDirectoryOnly : System.IO.SearchOption.AllDirectories;
			IFileInfo[] foundFiles = directoryToSearch.GetFiles(fileNamePattern, searchOption);

			foreach (IFileInfo file in foundFiles)
			{
				string relativePath = file.GetRelativePath(directoryToSearch);
				this.log(relativePath);
			}
			this.log($"Found {foundFiles.Length} files matching '{fileNamePattern}' in '{directoryToSearch.FullName}'.");

			if (shouldDelete && foundFiles.Any())
			{
				this.outputWriter.WriteLine($"Really delete? <yes|no>");
				if (!string.Equals(this.inputReader.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
				{
					throw new OperationCanceledException("Aborting, nothing deleted.");
				}

				int deletedCount = 0;
				try
				{
					foreach (IFileInfo file in foundFiles)
					{
						file.Delete();
						deletedCount++;
					}
				}
				finally
				{
					this.log($"Deleted {deletedCount} files.");
				}
			}
		}

		private void commandDirectoryFindEmpty(Dictionary<string, object> arguments)
		{
			bool shouldDelete = arguments.ContainsKey("--delete");

			IDirectoryInfo directoryToSearch = (IDirectoryInfo)arguments["DirectoryToSearch"];
			if (!directoryToSearch.Exists) { throw new CommandLineArgumentException("<DirectoryToSearch>", "Directory does not exist."); }

			List<IDirectoryInfo> emptyDirectories = new List<IDirectoryInfo>();
			void deleteRecursively(IDirectoryInfo directoryInfo)
			{
				if (!directoryInfo.GetFiles("*", System.IO.SearchOption.AllDirectories).Any())
				{
					emptyDirectories.Add(directoryInfo);
					this.log(directoryInfo.GetRelativePath(directoryToSearch));
				}
				else
				{
					foreach (IDirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories("*", System.IO.SearchOption.TopDirectoryOnly))
					{
						deleteRecursively(subDirectoryInfo);
					}
				}
			}

			deleteRecursively(directoryToSearch);
			this.log($"Found {emptyDirectories.Count} empty directories in '{directoryToSearch}'.");

			if (shouldDelete && emptyDirectories.Any())
			{
				this.outputWriter.WriteLine($"Really delete? <yes|no>");
				if (!string.Equals(this.inputReader.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
				{
					throw new OperationCanceledException("Aborting, nothing deleted.");
				}

				int deletedCount = 0;
				try
				{
					foreach (IDirectoryInfo directoryInfo in emptyDirectories)
					{
						directoryInfo.Delete(recursive: true);
						deletedCount++;
					}
				}
				finally
				{
					this.log($"Deleted {deletedCount} empty directories.");
				}
			}
		}

		#endregion Commands

		#region Helpers

		/// <summary>
		/// Goes through all files in the <paramref name="baseDirectory"/> and its subdirectories to create a <see cref="Catalog"/>.
		/// </summary>
		private Catalog createCatalogForDirectory(IDirectoryInfo baseDirectory, IFileInfo[] allFiles, out List<string> errors)
		{
			errors = new List<string>();
			List<FileInstance> fileInstances = new List<FileInstance>();
			int fileCount = 0;
			foreach (IFileInfo file in allFiles)
			{
				string relativePath = file.GetRelativePath(baseDirectory);
				try
				{
					using (System.IO.Stream stream = file.OpenRead())
					{
						long fileSize = stream.Length;
						Hash256 fileContentsHash = Hash256.GetContentsHash(stream);
						fileInstances.Add(new FileInstance(relativePath, fileSize, fileContentsHash));
					}
				}
				catch (System.IO.IOException ioException)
				{
					errors.Add($"Couldn't read ({ioException.Message}): {relativePath}");
				}
				fileCount++;
				if ((fileCount % 20) == 0) { this.outputWriter.WriteLine($"{(double)fileCount / allFiles.Length:P}% ({fileCount} / {allFiles.Length})"); }
			}
			return new Catalog(baseDirectory.FullName, DateTime.Now, fileInstances);
		}

		#endregion Helpers

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
