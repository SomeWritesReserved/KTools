﻿using System;
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
			{ "help", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandHelp, "<command>", "Gets more detailed help for a specific command and its usage (help <command>).") },
			{ "catalog", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandCatalog, "<DirectoryToCatalog>", "Catalogs all files in <DirectoryToCatalog> and its subdirectories. The catalog will be saved as a .kcatalog file in <DirectoryToCatalog>.") },
			{ "search", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandFileSearch, "[--norecursive] [--delete] <FileNamePattern> <DirectoryToSearch>", "Finds all files in <DirectoryToSearch> and its subdirectories (unless --norecursive is specified) matching <FileNamePattern>. Will list the files that are found. If --delete is specified they will also be deleted (useful for purging files like thumbs.db).") },
			{ "dedup", new Tuple<Action<Dictionary<string, object>>, string, string>(Program.commandDeduplicate, "[--delete] <DirectoryBase> <DirectoryToCheck>", "Finds all files in <DirectoryToCheck> and its subdirectories that already exist in <DirectoryBase> (i.e. files in <DirectoryToCheck> that duplicate files in <DirectoryBase>). Will list the files that are found. If --delete is specified they will also be deleted.") },
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
					if (optionName.StartsWith("Directory", StringComparison.OrdinalIgnoreCase))
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
				Console.WriteLine($"    {command.Key}:\t{command.Value.Item3}");
			}
			Console.WriteLine();
			Console.WriteLine("All commands accept the --log switch to write their output to a time stamped log file in addition to console output.");
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

		private static void commandCatalog(Dictionary<string, object> arguments)
		{
			DirectoryInfo directoryToCatalog = (DirectoryInfo)arguments["DirectoryToCatalog"];
			if (!directoryToCatalog.Exists) { throw new CommandLineArgumentException("<DirectoryToCatalog>", "Directory does not exist."); }

			FileInfo catalogFile = new FileInfo(Path.Combine(directoryToCatalog.FullName, ".kcatalog"));
			if (catalogFile.Exists)
			{
				Console.WriteLine($"Catalog file already exists: {catalogFile.FullName}");
				Console.WriteLine($"Really overwrite? <yes|no>");
				if (!string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Aborting, nothing overwritten.");
					return;
				}
			}

			FileInfo[] foundFiles = directoryToCatalog.GetFiles("*", SearchOption.AllDirectories);

			List<FileHash> fileHashes = new List<FileHash>();
			int fileCount = 0;
			foreach (FileInfo file in foundFiles)
			{
				string relativePath = file.FullName.Substring(directoryToCatalog.FullName.Length);
				try
				{
					using (FileStream fileStream = File.OpenRead(file.FullName))
					{
						long fileSize = fileStream.Length;
						Hash hash = Hash.GetFileHash(fileStream);
						fileHashes.Add(new FileHash(relativePath, fileSize, hash));
					}
				}
				catch (IOException ioException)
				{
					Program.log($"Couldn't read ({ioException.Message}): {relativePath}");
				}
				fileCount++;
				if ((fileCount % 20) == 0) { Console.WriteLine($"{(double)fileCount / foundFiles.Length:P}% ({fileCount} / {foundFiles.Length})"); }
			}

			Program.saveFileHashes(catalogFile.FullName, fileHashes);

			Console.WriteLine($"Cataloged {foundFiles.Length} files in '{directoryToCatalog.FullName}'.");
		}

		private static void commandFileSearch(Dictionary<string, object> arguments)
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
				string relativePath = file.FullName.Substring(directoryToSearch.FullName.Length);
				Program.log(relativePath);
			}
			Console.WriteLine($"Found {foundFiles.Length} files matching '{fileNamePattern}' in '{directoryToSearch.FullName}'.");

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
					Console.WriteLine($"Deleted {deletedCount} files.");
				}
			}
		}

		private static void commandDeduplicate(Dictionary<string, object> arguments)
		{
			bool shouldDelete = arguments.ContainsKey("--delete");

			DirectoryInfo directoryBase = (DirectoryInfo)arguments["DirectoryBase"];
			if (!directoryBase.Exists) { throw new CommandLineArgumentException("<DirectoryBase>", "Directory does not exist."); }
			DirectoryInfo directoryToCheck = (DirectoryInfo)arguments["DirectoryToCheck"];
			if (!directoryToCheck.Exists) { throw new CommandLineArgumentException("<DirectoryToCheck>", "Directory does not exist."); }

			FileInfo baseCatalogFile = new FileInfo(Path.Combine(directoryBase.FullName, ".kcatalog"));
			if (!baseCatalogFile.Exists) { throw new CommandLineArgumentException("<DirectoryBase>", "Directory does not contain a catalog file."); }
			FileInfo toCheckCatalogFile = new FileInfo(Path.Combine(directoryToCheck.FullName, ".kcatalog"));
			if (!toCheckCatalogFile.Exists) { throw new CommandLineArgumentException("<DirectoryToCheck>", "Directory does not contain a catalog file."); }

			FileCatalog baseFileCatalog = new FileCatalog(Program.loadFileHashes(baseCatalogFile.FullName));
			List<FileHash> toCheckFileHashes = Program.loadFileHashes(toCheckCatalogFile.FullName);

			List<FileHash> fileHashesToDelete = new List<FileHash>();
			foreach (FileHash toCheckFileHash in toCheckFileHashes)
			{
				IList<FileHash> baseFileHashes = baseFileCatalog.Find(toCheckFileHash.Hash);
				if (!baseFileHashes.Any()) { continue; }

				string fullPath = Path.Combine(directoryToCheck.FullName, toCheckFileHash.RelativePath);
				if (!File.Exists(fullPath)) { continue; }

				// Todo: If we found duplicates, make sure their hashes are still identical and not just stale
				fileHashesToDelete.Add(toCheckFileHash);
				Program.log(toCheckFileHash.RelativePath);
			}
			Console.WriteLine($"Found {fileHashesToDelete.Count} files in '{directoryToCheck}' duplicating those in '{directoryBase}'.");

			if (shouldDelete && fileHashesToDelete.Any())
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
					foreach (FileHash fileHash in fileHashesToDelete)
					{
						string fullPath = Path.Combine(directoryToCheck.FullName, fileHash.RelativePath);
						File.SetAttributes(fullPath, FileAttributes.Normal);
						File.Delete(fullPath);
						deletedCount++;
					}
				}
				finally
				{
					Console.WriteLine($"Deleted {deletedCount} files.");
				}
			}

		}

		#region Helpers

		private static void saveFileHashes(string path, IEnumerable<FileHash> fileHashes)
		{
			new XDocument(
				new XElement("Catalog",
					new XElement("Date", DateTime.Now),
					new XElement("Files",
						fileHashes.Select((fileHash) => new XElement("f", new XAttribute("p", fileHash.RelativePath), new XAttribute("h", fileHash.Hash), new XAttribute("l", fileHash.FileSize)))
					)
				)
			).Save(path);
		}

		private static List<FileHash> loadFileHashes(string path)
		{
			XDocument xDocument = XDocument.Load(path);
			return xDocument.Element("Catalog").Element("Files").Elements("f")
				.Select((element) => new FileHash(element.Attribute("p").Value, long.Parse(element.Attribute("l").Value), Hash.Parse(element.Attribute("h").Value)))
				.ToList();
		}

		#endregion Helpers

		#endregion Commands

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