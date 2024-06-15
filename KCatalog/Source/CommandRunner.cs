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
	public partial class CommandRunner
	{
		#region Fields

		private readonly IFileSystem fileSystem;
		private readonly System.IO.TextWriter outputWriter;
		private readonly System.IO.TextReader inputReader;
		private string logFileName;

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

				{ "catalog-create-detached", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogCreateDetached, "<DirectoryToCatalog> <CatalogFile>",
					"Catalogs all files in <DirectoryToCatalog> and its subdirectories to create a catalog file. The catalog will be saved as a '.kcatalog' file at <CatalogFile>. This is intended for cataloging read-only media like CD/DVDs when a .kcatalog file cannot be created at the root directory.") },

				{ "catalog-update", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogUpdate, "[--dryrun] <CatalogFile>",
					"Catalogs all new files in the directory of <CatalogFile> and its subdirectories that have been added or removed since when the catalog was taken. This command only checks for new or deleted files, it does not check file contents or hashes of existing already cataloged files. If --dryrun is specified the catalog won't actually be updated and instead the new and deleted files will only be listed.") },

				{ "catalog-check-file-integrity", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogCheckFileIntegrity, "<CatalogFile>",
					"Checks all files in the directory of <CatalogFile> and its subdirectories that are already in the catalog, and confirms that they haven't changed. This command checks the contents and hashes of existing already cataloged files, listing those that changed, it will not check for new files. The catalog will not be updated, only modified files will be listed. Useful to check for file corruption or modified files.") },

				{ "catalog-compare-duplicates", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogCompareDuplicates, "[--delete] <BaseCatalogFile> <OtherCatalogFile>",
					"Finds all files in <OtherCatalogFile> that are already cataloged by <BaseCatalogFile> (i.e. files in <OtherCatalogFile> that duplicate files in <BaseCatalogFile>). This ignores file paths and file names, it only compares file contents/hashes (so files are considered duplicate if they have the same content but different file names). This command will list the duplicated files that are found. If --delete is specified the duplicated files will also be deleted from the directory of <OtherCatalogFile>. This is the same as 'dir-compare-duplicates' but faster since the cataloging has already been done. Be sure both catalogs are up-to-date (using 'catalog-create' or 'catalog-update').") },

				{ "catalog-compare-duplicates-self", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogCompareDuplicatesSelf, "<CatalogFile>",
					"Finds all files in <CatalogFile> that are cataloged in more than one place (i.e. files in <CatalogFile> that duplicate other files also in <CatalogFile>). This ignores file paths and file names, it only compares file contents/hashes (so files are considered duplicate if they have the same content but different file names). This command will list the duplicated files that are found.") },

				{ "catalog-compare-unique", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogCompareUnique, "<BaseCatalogFile> <OtherCatalogFile>",
					"Finds all files in <OtherCatalogFile> that are _not_ cataloged by <BaseCatalogFile> (i.e. files in <OtherCatalogFile> that are unique and not in <BaseCatalogFile>). This ignores file paths and file names, it only compares file contents/hashes (so files are considered duplicate if they have the same content but different file names). This command will list the unique files that are found. Be sure both catalogs are up-to-date (using 'catalog-create' or 'catalog-update').") },

				{ "catalog-find-unbackedup", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandCatalogFindUnbackedup, "[--makebatfiles] <BaseCatalogFile> <ReadOnlyCatalogDirectory>",
					"Finds all files in <BaseCatalogFile> that are _not_ cataloged by any of the catalogs in <ReadOnlyCatalogDirectory> (i.e. files in <BaseCatalogFile> that haven't been backed up to read-only media yet). Use this to determine what files need to be backed up and how much space it would take. If --makebatfiles is specified the command will write out Windows bat files to copy files to the Windows burning staging folder.") },

				{ "dir-compare-duplicates", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandDirectoryCompareDuplicate, "[--delete] <BaseDirectory> <OtherDirectory>",
					"Finds all files in <OtherDirectory> that exist in <BaseDirectory> (i.e. files in <OtherDirectory> that duplicate files in <BaseCatalogFile>). This ignores file paths and file names, it only compares file contents/hashes (so files are still considered duplicate if they have the same content but different file nameS). This command will list the duplicated files that are found. If --delete is specified the duplicated files will also be deleted from <OtherDirectory>. This is the same as 'catalog-compare-duplicates' but slower since both directories need to be cataloged first (these catalogs will not be saved).") },

				{ "dir-search", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandDirectorySearch, "[--norecursive] [--delete] <DirectoryToSearch> <FileNamePattern>",
					"Finds all files in <DirectoryToSearch> and its subdirectories (unless --norecursive is specified) matching <FileNamePattern>. This command will list the files that are found. The pattern is Windows file system pattern matching (not regex). If --delete is specified they will also be deleted. This is useful for purging files like thumbs.db.") },

				{ "dir-findempty", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandDirectoryFindEmpty, "[--delete] <DirectoryToSearch>",
					"Searches all directories in <DirectoryToSearch> and its subdirectories for empty directories. If --delete is specified they will also be deleted. A directory is empty if it contains no files in itself or any of its subdirectories. This will only print the upper-most directory if all of its subdirectories are also empty.") },

				{ "photo-archive", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandPhotoArchive, "<SourceDirectory> <CatalogFile>",
					"Moves all photo and video files from <SourceDirectory> and its subdirectories to the directory archived by <CatalogFile>, following the folder structure of YYYY/mm-xxxxx/dd-mm-YYYY. Files will be _moved_, not copied. Photos and videos must have an Android file name format (YYYYmmDD_HHMMSS). Prefixes of IMG_ and VID_ are removed so photos and videos are ordered chronologically side-by-side.") },

				{ "photo-archive-validate", new Tuple<Action<Dictionary<string, object>>, string, string>(this.commandPhotoArchiveValidate, "<ArchiveDirectory>",
					"Validates all folders in <ArchiveDirectory> to make sure they match the expected structure of YYYY/mm-xxxxx/dd-mm-YYYY. This command will list any folders that are incorrect (if nothing is listed then all folders are correct).") },
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
							this.log($"Version {Program.SoftwareVersion}: KCatalog.exe " + string.Join(" ", args.Select((arg) => $"\"{arg}\"")));
						}
						if (Program.SoftwareVersion.Contains("-dirty")) { this.outputWriter.WriteLine($"WARNING: This is a dev/dirty build ({Program.SoftwareVersion})."); }
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
					if (optionName.StartsWith("Directory") || optionName.EndsWith("Directory"))
					{
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
			this.outputWriter.WriteLine($"Version {Program.SoftwareVersion}");
			this.outputWriter.WriteLine($"Available commands are:");
			this.outputWriter.Write("  ");
			this.outputWriter.WriteLine(string.Join(", ", this.commands.Select((command) => command.Key)));
			this.outputWriter.WriteLine();
			this.outputWriter.WriteLine("How to use: Typically you create a catalog of a directory using 'catalog-create', update it periodically with 'catalog-update', then recreate the catalog as-needed by doing 'catalog-create' again if file contents change. " +
				"You can detect duplicate files between two folders by using 'dir-compare'. " +
				"Use 'photo-archive' to automatically put photos into the archive with the correct folder structure.");
			this.outputWriter.WriteLine();
			this.outputWriter.WriteLine("All commands accept the --log switch to write their important output to a time stamped log file in addition to standard output.");
		}

		private void commandHelp(Dictionary<string, object> arguments)
		{
			string commandName = (string)arguments["command"];
			if (commandName.Equals("all", StringComparison.OrdinalIgnoreCase))
			{
				foreach (var command in this.commands)
				{
					this.outputWriter.WriteLine($"{command.Key}");
					this.outputWriter.WriteLine(new string('-', command.Key.Length));
					this.outputWriter.WriteLine($"{command.Value.Item3}");
					this.outputWriter.WriteLine();
				}
			}
			else if (this.commands.ContainsKey(commandName))
			{
				this.outputWriter.WriteLine($"{commandName} [--log] {this.commands[commandName].Item2}");
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
			IFileInfo catalogFile = this.fileSystem.FileInfo.FromFileName(this.fileSystem.Path.Combine(directoryToCatalog.FullName, ".kcatalog"));
			this.catalogCreateCore(directoryToCatalog, catalogFile);
		}

		private void commandCatalogCreateDetached(Dictionary<string, object> arguments)
		{
			IDirectoryInfo directoryToCatalog = (IDirectoryInfo)arguments["DirectoryToCatalog"];
			IFileInfo catalogFile = (IFileInfo)arguments["CatalogFile"];
			this.catalogCreateCore(directoryToCatalog, catalogFile);
		}

		private Catalog catalogCreateCore(IDirectoryInfo directoryToCatalog, IFileInfo catalogFile)
		{
			if (!directoryToCatalog.Exists) { throw new CommandLineArgumentException("<DirectoryToCatalog>", "Directory does not exist."); }

			this.outputWriter.Write("Getting files in directory to catalog... ");
			IFileInfo[] foundFiles = directoryToCatalog.GetFiles("*", System.IO.SearchOption.AllDirectories)
				.Where((file) => file.Name != ".kcatalog")
				.Where((file) => !file.FullName.Contains("\\.git\\"))
				.ToArray();
			this.outputWriter.WriteLine($"Found {foundFiles.Length} files.");

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
			return catalog;
		}

		private void commandCatalogUpdate(Dictionary<string, object> arguments)
		{
			bool isDryRun = arguments.ContainsKey("--dryrun");

			IFileInfo catalogFile = (IFileInfo)arguments["CatalogFile"];
			if (!catalogFile.Exists) { throw new CommandLineArgumentException("<CatalogFile>", "Catalog file does not exist."); }

			Catalog originalCatalog = Catalog.Read(catalogFile);
			IDirectoryInfo catalogedDirectory = catalogFile.Directory;
			if (!catalogedDirectory.FullName.Equals(this.fileSystem.DirectoryInfo.FromDirectoryName(originalCatalog.BaseDirectoryPath).FullName, StringComparison.OrdinalIgnoreCase))
			{
				throw new CommandLineArgumentException("<CatalogFile>", "Catalog file was moved and does not represent a catalog of the directory it is currently in. Cannot update.");
			}

			this.outputWriter.Write("Getting files in cataloged directory... ");
			Dictionary<string, IFileInfo> foundFiles = catalogedDirectory.GetFiles("*", System.IO.SearchOption.AllDirectories)
				.Where((file) => file.Name != ".kcatalog")
				.Where((file) => !file.FullName.Contains("\\.git\\"))
				.ToDictionary((file) => file.GetRelativePath(catalogedDirectory), (file) => file, StringComparer.OrdinalIgnoreCase);
			this.outputWriter.WriteLine($"Found {foundFiles.Count} files.");

			bool hasChanges = false;
			Dictionary<Hash256, FileInstance> removedFileHashes = new Dictionary<Hash256, FileInstance>();
			HashSet<FileInstance> newFileInstances = new HashSet<FileInstance>(originalCatalog.FileInstances);

			foreach (FileInstance fileInstance in originalCatalog.FileInstances)
			{
				if (!foundFiles.Remove(fileInstance.RelativePath))
				{
					newFileInstances.Remove(fileInstance);
					if (!removedFileHashes.ContainsKey(fileInstance.FileContentsHash))
					{
						// Only add the first removed file instance which is probably the oldest one
						removedFileHashes.Add(fileInstance.FileContentsHash, fileInstance);
					}
					hasChanges = true;
				}
			}

			foreach (KeyValuePair<string, IFileInfo> leftOverFile in foundFiles.OrderBy((kvp) => kvp.Key))
			{
				FileInstance fileInstance = this.createFileInstance(catalogedDirectory, leftOverFile.Value);
				if (removedFileHashes.TryGetValue(fileInstance.FileContentsHash, out FileInstance removedFileInstance))
				{
					this.log($"File moved       : {leftOverFile.Key} (from {removedFileInstance.RelativePath})");
					removedFileHashes.Remove(removedFileInstance.FileContentsHash);
				}
				else
				{
					this.log($"New file added   : {leftOverFile.Key}");
				}
				newFileInstances.Add(fileInstance);
				hasChanges = true;
			}

			if (hasChanges)
			{
				Catalog updatedCatalog = new Catalog(originalCatalog.BaseDirectoryPath, originalCatalog.CatalogedOn, DateTime.Now, newFileInstances);

				foreach (FileInstance removedFileInstance in removedFileHashes.Values)
				{
					// These are all the files left over that haven't been detected as moved
					if (updatedCatalog.FileInstancesByHash.TryGetValue(removedFileInstance.FileContentsHash, out IReadOnlyList<FileInstance> otherFileInstances))
					{
						// In this case there are files that still exist with the same hash, so all we've done is remove a duplicate
						this.log($"Duplicate removed: {removedFileInstance.RelativePath} (from {otherFileInstances.First().RelativePath})");
					}
					else
					{
						// No duplicates, no newly files to be a move, it is truly removed
						this.log($"File deleted     : {removedFileInstance.RelativePath}");
					}
				}

				if (!isDryRun)
				{
					updatedCatalog.Write(catalogFile);
				}
			}
		}

		private void commandCatalogCheckFileIntegrity(Dictionary<string, object> arguments)
		{
			IFileInfo catalogFile = (IFileInfo)arguments["CatalogFile"];
			if (!catalogFile.Exists) { throw new CommandLineArgumentException("<CatalogFile>", "Catalog file does not exist."); }

			Catalog originalCatalog = Catalog.Read(catalogFile);
			IDirectoryInfo catalogedDirectory = catalogFile.Directory;
			if (!catalogedDirectory.FullName.Equals(this.fileSystem.DirectoryInfo.FromDirectoryName(originalCatalog.BaseDirectoryPath).FullName, StringComparison.OrdinalIgnoreCase))
			{
				throw new CommandLineArgumentException("<CatalogFile>", "Catalog file was moved and does not represent a catalog of the directory it is currently in. Cannot check.");
			}

			IFileInfo tempCatalogFile = this.fileSystem.FileInfo.FromFileName(this.fileSystem.Path.Combine(this.fileSystem.Path.GetTempPath(), "kcatalog_" + this.fileSystem.Path.GetRandomFileName() + ".kcatalog"));
			Catalog newCatalog = this.catalogCreateCore(catalogedDirectory, tempCatalogFile);
			this.log($"Temporary catalog file: {tempCatalogFile}");
			
			// Compare original and new catalog for same file paths with different hashes, ignore any files that do not exist in the other.
			int modifiedFileCount = 0;
			foreach (FileInstance originalFileInstance in originalCatalog.FileInstances)
			{
				if (!newCatalog.FileInstancesByPath.TryGetValue(originalFileInstance.RelativePath, out FileInstance newFileInstance)) { continue; }

				if (!originalFileInstance.FileContentsHash.Equals(newFileInstance.FileContentsHash))
				{
					this.log(originalFileInstance.RelativePath);
					modifiedFileCount++;
				}
			}

			this.log($"Found {modifiedFileCount} modified files.");
		}

		private void commandCatalogCompareDuplicates(Dictionary<string, object> arguments)
		{
			bool shouldDelete = arguments.ContainsKey("--delete");

			IFileInfo baseCatalogFile = (IFileInfo)arguments["BaseCatalogFile"];
			if (!baseCatalogFile.Exists) { throw new CommandLineArgumentException("<BaseCatalogFile>", "Catalog file does not exist."); }
			IFileInfo otherCatalogFile = (IFileInfo)arguments["OtherCatalogFile"];
			if (!otherCatalogFile.Exists) { throw new CommandLineArgumentException("<OtherCatalogFile>", "Catalog file does not exist."); }

			Catalog baseCatalog = Catalog.Read(baseCatalogFile);
			Catalog otherCatalog = Catalog.Read(otherCatalogFile);
			this.compareCatalogsForDuplicates(baseCatalogFile.Directory, baseCatalog, otherCatalogFile.Directory, otherCatalog, shouldDelete);
		}

		private void commandCatalogCompareDuplicatesSelf(Dictionary<string, object> arguments)
		{
			IFileInfo catalogFile = (IFileInfo)arguments["CatalogFile"];
			if (!catalogFile.Exists) { throw new CommandLineArgumentException("<CatalogFile>", "Catalog file does not exist."); }

			Catalog catalog = Catalog.Read(catalogFile);
			foreach (KeyValuePair<Hash256, IReadOnlyList<FileInstance>> duplicatedFile in catalog.FileInstancesByHash.Where((kvp) => kvp.Value.Count() > 1))
			{
				this.log($"Duplicate hash: {duplicatedFile.Key}");
				foreach (FileInstance duplicateFileInstance in duplicatedFile.Value)
				{
					this.log($"\t{duplicateFileInstance.RelativePath}");
				}
			}
		}

		private void commandCatalogCompareUnique(Dictionary<string, object> arguments)
		{
			IFileInfo baseCatalogFile = (IFileInfo)arguments["BaseCatalogFile"];
			if (!baseCatalogFile.Exists) { throw new CommandLineArgumentException("<BaseCatalogFile>", "Catalog file does not exist."); }
			IFileInfo otherCatalogFile = (IFileInfo)arguments["OtherCatalogFile"];
			if (!otherCatalogFile.Exists) { throw new CommandLineArgumentException("<OtherCatalogFile>", "Catalog file does not exist."); }

			Catalog baseCatalog = Catalog.Read(baseCatalogFile);
			Catalog otherCatalog = Catalog.Read(otherCatalogFile);
			this.compareCatalogsForUnique(baseCatalogFile.Directory, baseCatalog, otherCatalogFile.Directory, otherCatalog);
		}

		private void commandCatalogFindUnbackedup(Dictionary<string, object> arguments)
		{
			IFileInfo baseCatalogFile = (IFileInfo)arguments["BaseCatalogFile"];
			if (!baseCatalogFile.Exists) { throw new CommandLineArgumentException("<BaseCatalogFile>", "Catalog file does not exist."); }
			IDirectoryInfo readOnlyCatalogDirectory = (IDirectoryInfo)arguments["ReadOnlyCatalogDirectory"];
			if (!readOnlyCatalogDirectory.Exists) { throw new CommandLineArgumentException("<ReadOnlyCatalogDirectory>", "Directory does not exist."); }
			bool shouldMakeBatFiles = arguments.ContainsKey("--makebatfiles");

			Catalog baseCatalog = Catalog.Read(baseCatalogFile);
			List<Catalog> readOnlyCatalogs = readOnlyCatalogDirectory.GetFiles("*.kcatalog", System.IO.SearchOption.TopDirectoryOnly)
				.Select((catalogFile) => Catalog.Read(catalogFile))
				.ToList();

			// First simply get all of the files that need backed up
			List<FileInstance> allFilesThatNeedBackedUp = new List<FileInstance>();
			foreach (KeyValuePair<Hash256, IReadOnlyList<FileInstance>> fileInstanceByHash in baseCatalog.FileInstancesByHash
				.OrderBy((instance) => instance.Value.First().RelativePath))
			{
				if (readOnlyCatalogs.Any((readOnlyCatalog) => readOnlyCatalog.FileInstancesByHash.ContainsKey(fileInstanceByHash.Key)))
				{
					continue;
				}
				allFilesThatNeedBackedUp.Add(fileInstanceByHash.Value.First());
			}

			// Then group the files together onto discs that can be burned
			// This logic will try to collect full directories together onto one disc, so files in a directory are not split across two discs
			const long maxDiscSize = 24_000_000_000 /*blu-ray disc size that Windows will accept*/;
			long currentDiscTotalSize = 0;
			int currentDiscNumber = 1;
			Dictionary<int, List<string>> allDiscsBatFileLines = new Dictionary<int, List<string>>() { { currentDiscNumber, new List<string>() } };
			IEnumerable<FileInstance> remainingFilesThatNeedBackedUp = allFilesThatNeedBackedUp;
			while (remainingFilesThatNeedBackedUp.Any())
			{
				FileInstance firstFileInFolder = remainingFilesThatNeedBackedUp.First();
				List<FileInstance> filesInSameFolder = remainingFilesThatNeedBackedUp
					.TakeWhile((instance) => System.IO.Path.GetDirectoryName(instance.RelativePath) == System.IO.Path.GetDirectoryName(firstFileInFolder.RelativePath))
					.ToList();
				remainingFilesThatNeedBackedUp = remainingFilesThatNeedBackedUp.Skip(filesInSameFolder.Count);

				long sizeOfFolder = filesInSameFolder.Sum((instance) => instance.FileSize);
				if (currentDiscTotalSize + sizeOfFolder > maxDiscSize)
				{
					this.log($"# Filled disc {currentDiscNumber}, with {currentDiscTotalSize} bytes ({currentDiscTotalSize / 1024.0 / 1024.0 / 1024.0:0.000} GB).");
					currentDiscTotalSize = 0;
					currentDiscNumber++;
					allDiscsBatFileLines.Add(currentDiscNumber, new List<string>());
				}
				currentDiscTotalSize += sizeOfFolder;

				foreach (FileInstance fileInstanceInFolder in filesInSameFolder)
				{
					allDiscsBatFileLines[currentDiscNumber].Add($@"echo F|xcopy ""{baseCatalog.BaseDirectoryPath}\{fileInstanceInFolder.RelativePath}"" ""C:\Users\dexter\AppData\Local\Microsoft\Windows\Burn\Burn\{fileInstanceInFolder.RelativePath}""");
					this.log($"{fileInstanceInFolder.RelativePath}\t{fileInstanceInFolder.FileContentsHash}\t{fileInstanceInFolder.FileSize}");
				}
			}
			this.log($"# Unfilled disc {currentDiscNumber}, with {currentDiscTotalSize} bytes ({currentDiscTotalSize / 1024.0 / 1024.0 / 1024.0:0.000} GB).");

			// Finally write out the bat files if requested
			if (shouldMakeBatFiles)
			{
				foreach (KeyValuePair<int, List<string>> discBatFileLines in allDiscsBatFileLines)
				{
					string batFileName = $"WriteDisc{discBatFileLines.Key}.bat";
					if (this.fileSystem.File.Exists(batFileName)) { throw new InvalidOperationException($@"Bat file ""{batFileName}"" already exists, canceling."); }
					this.fileSystem.File.WriteAllLines(batFileName, discBatFileLines.Value);
				}
			}

			long totalFileSizeUnbackedup = allFilesThatNeedBackedUp.Sum((instance) => instance.FileSize);
			this.log($"{allFilesThatNeedBackedUp.Count} files not backed up, with {totalFileSizeUnbackedup} bytes ({totalFileSizeUnbackedup / 1024.0 / 1024.0 / 1024.0:0.000} GB) across {currentDiscNumber} Blu-ray discs.");
		}

		private void commandDirectoryCompareDuplicate(Dictionary<string, object> arguments)
		{
			bool shouldDelete = arguments.ContainsKey("--delete");

			IDirectoryInfo baseDirectory = (IDirectoryInfo)arguments["BaseDirectory"];
			if (!baseDirectory.Exists) { throw new CommandLineArgumentException("<BaseDirectory>", "Directory does not exist."); }
			IDirectoryInfo otherDirectory = (IDirectoryInfo)arguments["OtherDirectory"];
			if (!otherDirectory.Exists) { throw new CommandLineArgumentException("<OtherDirectory>", "Directory does not exist."); }

			this.outputWriter.Write("Getting files in base directory... ");
			IFileInfo[] baseDirectoryFiles = baseDirectory.GetFiles("*", System.IO.SearchOption.AllDirectories);
			this.outputWriter.WriteLine($"Found {baseDirectoryFiles.Length} files.");

			this.outputWriter.Write("Getting files in other directory... ");
			IFileInfo[] otherDirectoryFiles = otherDirectory.GetFiles("*", System.IO.SearchOption.AllDirectories);
			this.outputWriter.WriteLine($"Found {otherDirectoryFiles.Length} files.");

			this.outputWriter.WriteLine("Cataloging the base directory files...");
			Catalog baseCatalog = this.createCatalogForDirectory(baseDirectory, baseDirectoryFiles, out List<string> baseDirectoryErrors);

			this.outputWriter.WriteLine("Cataloging the other directory files...");
			Catalog otherCatalog = this.createCatalogForDirectory(otherDirectory, otherDirectoryFiles, out List<string> otherDirectoryErrors);

			List<string> allErrors = baseDirectoryErrors.Concat(otherDirectoryErrors).ToList();
			if (allErrors.Any())
			{
				this.log($"{allErrors.Count} errors:");
				allErrors.ForEach((error) => this.log($"  {error}"));
			}

			this.compareCatalogsForDuplicates(baseDirectory, baseCatalog, otherDirectory, otherCatalog, shouldDelete);
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
				try
				{
					FileInstance fileInstance = this.createFileInstance(baseDirectory, file);
					fileInstances.Add(fileInstance);
				}
				catch (System.IO.IOException ioException)
				{
					errors.Add($"Couldn't read ({ioException.Message}): {file}");
				}
				fileCount++;
				if ((fileCount % 20) == 0) { this.outputWriter.WriteLine($"{(double)fileCount / allFiles.Length:P}% ({fileCount} / {allFiles.Length})"); }
			}
			return new Catalog(baseDirectory.FullName, DateTime.Now, fileInstances);
		}

		/// <summary>
		/// Creates a new <see cref="FileInstance"/> for the specified file.
		/// </summary>
		private FileInstance createFileInstance(IDirectoryInfo baseDirectory, IFileInfo file)
		{
			string relativePath = file.GetRelativePath(baseDirectory);
			using (System.IO.Stream stream = file.OpenRead())
			{
				long fileSize = stream.Length;
				Hash256 fileContentsHash = Hash256.GetContentsHash(stream);
				return new FileInstance(relativePath, fileSize, fileContentsHash);
			}
		}

		/// <summary>
		/// Compares two cataloged directories and lists all the files in the 'other' directory that duplicate files in the 'base' directory, with the option of deleting those files.
		/// </summary>
		private void compareCatalogsForDuplicates(IDirectoryInfo baseDirectory, Catalog baseCatalog, IDirectoryInfo otherDirectory, Catalog otherCatalog, bool shouldDelete)
		{
			List<FileInstance> fileInstancesToDelete = new List<FileInstance>();
			foreach (FileInstance otherFileInstance in otherCatalog.FileInstances)
			{
				IReadOnlyList<FileInstance> matchingBaseFileInstances = baseCatalog.FindFiles(otherFileInstance.FileContentsHash);
				if (!matchingBaseFileInstances.Any()) { continue; }

				// [Todo]: If we found duplicates and we are going to delete the files, make sure their hashes are still up-to-date and identical in case the files were edited
				// though this doesn't make sense if being called from dir-compare since it is always up-to-date because the hashes were just taken.

				fileInstancesToDelete.Add(otherFileInstance);
				this.log(otherFileInstance.RelativePath);
			}
			this.log($"Found {fileInstancesToDelete.Count} files in '{otherDirectory}' duplicating those in '{baseDirectory}'.");

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
						string fullPath = this.fileSystem.Path.Combine(otherDirectory.FullName, fileInstance.RelativePath);
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

		/// <summary>
		/// Compares two cataloged directories and lists all the files in the 'other' directory that are unique (not in 'base' directory).
		/// </summary>
		private void compareCatalogsForUnique(IDirectoryInfo baseDirectory, Catalog baseCatalog, IDirectoryInfo otherDirectory, Catalog otherCatalog)
		{
			List<FileInstance> uniqueFileInstances = new List<FileInstance>();
			foreach (FileInstance otherFileInstance in otherCatalog.FileInstances)
			{
				IReadOnlyList<FileInstance> matchingBaseFileInstances = baseCatalog.FindFiles(otherFileInstance.FileContentsHash);
				if (!matchingBaseFileInstances.Any())
				{
					uniqueFileInstances.Add(otherFileInstance);
					this.log(otherFileInstance.RelativePath);
				}
			}
			this.log($"Found {uniqueFileInstances.Count} unique files in '{otherDirectory}' not in '{baseDirectory}'.");
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
