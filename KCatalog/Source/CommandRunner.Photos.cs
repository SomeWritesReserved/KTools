using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KCatalog
{
	public partial class CommandRunner
	{
		#region Fields

		/// <summary>A regex for the directory name format of a day directory which has month, day, and year; in that order and exact formatting (MM-dd-yyyy).</summary>
		private static readonly Regex dayDirectoryNameRegex = new Regex(@"(?<month>[01]\d)-(?<day>[0123]\d)-(?<year>[12][09]\d\d)(?<description>.*)");

		/// <summary>A regex for the file name format of a well-formed photo in a day directory.</summary>
		private static readonly Regex photoFileNameRegex = new Regex(@"(?<prefix>.*?)(?<year>[12][09]\d\d)(?<month>[01]\d)(?<day>[0123]\d)_(?<hour>[012]\d)(?<minute>\d\d)(?<second>\d\d)(?<millisecond>\d\d\d)(?<suffix>.*?)");

		/// <summary>The list of months that are formatted for the desired folder names.</summary>
		private static readonly string[] photoMonthFolderNames = new string[] { "01-January", "02-February", "03-March", "04-April", "05-May", "06-June", "07-July", "08-August", "09-September", "10-October", "11-November", "12-December" };

		#endregion Fields

		#region Methods

		#region Commands

		private void commandPhotoArchive(Dictionary<string, object> arguments)
		{
			IDirectoryInfo sourceDirectory = (IDirectoryInfo)arguments["SourceDirectory"];
			IDirectoryInfo archiveDirectory = (IDirectoryInfo)arguments["ArchiveDirectory"];

			this.outputWriter.Write("Getting files in source directory... ");
			IFileInfo[] sourceFiles = sourceDirectory.GetFiles("*", SearchOption.AllDirectories);
			this.outputWriter.WriteLine($"Found {sourceFiles.Length} files.");

			foreach (IFileInfo sourceFile in sourceFiles)
			{
				if (CommandRunner.photoFileNameRegex.IsMatch(sourceFile.Name))
				{
					Match match = CommandRunner.photoFileNameRegex.Match(sourceFile.Name);
					string prefix = match.Groups["prefix"].Value;
					int month = int.Parse(match.Groups["month"].Value);
					int day = int.Parse(match.Groups["day"].Value);
					int year = int.Parse(match.Groups["year"].Value);
					DateTime dateTime = new DateTime(year, month, day);

					string dayFolderPath = fileSystem.Path.Combine(archiveDirectory.FullName, this.getYearFormatted(dateTime), this.getMonthFormatted(dateTime), this.getDayFormatted(dateTime));
					fileSystem.Directory.CreateDirectory(dayFolderPath);

					// We strip the prefix so that photos and videos are all side-by-side, sorted by timestamp
					string archiveFileName = sourceFile.Name.Substring(prefix.Length);
					string archiveFilePath = fileSystem.Path.Combine(dayFolderPath, archiveFileName);

					if (fileSystem.File.Exists(archiveFilePath))
					{
						if (fileSystem.File.ReadAllBytes(archiveFilePath).SequenceEqual(fileSystem.File.ReadAllBytes(sourceFile.FullName)))
						{
							// Files are the same so just delete the source file since it already exists in the archive directory
							sourceFile.Delete();
						}
						else
						{
							this.log($"Cannot archive file, identical file name already exists with different file contents: {sourceFile} to {archiveFilePath}");
						}
					}
					else
					{
						sourceFile.MoveTo(archiveFilePath);
					}
				}
				else
				{
					this.log($"Cannot archive file, unknown date: {sourceFile}");
				}
			}
		}

		private void commandPhotoArchiveValidate(Dictionary<string, object> arguments)
		{
			IDirectoryInfo archiveDirectory = (IDirectoryInfo)arguments["ArchiveDirectory"];

			// Year
			List<IDirectoryInfo> yearFolders = new List<IDirectoryInfo>();
			foreach (IDirectoryInfo subDirectory in archiveDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly))
			{
				string folderName = subDirectory.Name;
				if (folderName.Length == 4 && int.TryParse(folderName, out int year)) { yearFolders.Add(subDirectory); }
				else { this.log($"Bad year folder: {subDirectory}"); }
			}

			foreach (IDirectoryInfo yearFolder in yearFolders)
			{
				// Month
				List<IDirectoryInfo> monthFolders = new List<IDirectoryInfo>();
				foreach (IDirectoryInfo subDirectory in yearFolder.GetDirectories("*", SearchOption.TopDirectoryOnly))
				{
					string folderName = subDirectory.Name;
					if (CommandRunner.photoMonthFolderNames.Contains(folderName)) { monthFolders.Add(subDirectory); }
					else { this.log($"Bad month folder: {subDirectory}"); }
				}

				foreach (IDirectoryInfo monthFolder in monthFolders)
				{
					// Day
					List<IDirectoryInfo> dayFolders = new List<IDirectoryInfo>();
					foreach (IDirectoryInfo subDirectory in monthFolder.GetDirectories("*", SearchOption.TopDirectoryOnly))
					{
						if (CommandRunner.dayDirectoryNameRegex.IsMatch(subDirectory.Name))
						{
							Match match = CommandRunner.dayDirectoryNameRegex.Match(subDirectory.Name);
							int month = int.Parse(match.Groups["month"].Value);
							int day = int.Parse(match.Groups["day"].Value);
							int year = int.Parse(match.Groups["year"].Value);
							string description = match.Groups["description"].Value;
							DateTime dateTime = new DateTime(year, month, day);

							if (this.getMonthFormatted(dateTime) != monthFolder.Name) { this.log($"Day folder in wrong month: {subDirectory}"); }
							if (this.getYearFormatted(dateTime) != yearFolder.Name) { this.log($"Day folder in wrong year: {subDirectory}"); }
							if (string.IsNullOrEmpty(description)) { this.log($"Day folder has no description: {subDirectory}"); }
							dayFolders.Add(subDirectory);
						}
						else { this.log($"Bad day folder: {subDirectory}"); }
					}

					foreach (IDirectoryInfo dayFolder in dayFolders)
					{
						// Photo
						foreach (IFileInfo file in dayFolder.GetFiles("*", SearchOption.TopDirectoryOnly))
						{
							if (CommandRunner.photoFileNameRegex.IsMatch(file.Name))
							{
								Match match = CommandRunner.photoFileNameRegex.Match(file.Name);
								int month = int.Parse(match.Groups["month"].Value);
								int day = int.Parse(match.Groups["day"].Value);
								int year = int.Parse(match.Groups["year"].Value);
								DateTime dateTime = new DateTime(year, month, day);

								if (this.getYearFormatted(dateTime) != yearFolder.Name) { this.log($"Photo in wrong year: {file}"); }
								if (this.getMonthFormatted(dateTime) != monthFolder.Name) { this.log($"Photo in wrong month: {file}"); }
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Returns a standardized path for a photo set on a given day with an optional description.
		/// </summary>
		private string getPhotoFolderPath(string archiveDirectory, DateTime dateTime, string description = null)
		{
			string path = Path.Combine(archiveDirectory, this.getYearFormatted(dateTime), this.getMonthFormatted(dateTime), this.getDayFormatted(dateTime));
			if (!string.IsNullOrEmpty(description))
			{
				path = $"{path} {description.Trim()}";
			}
			return path;
		}

		/// <summary>
		/// Gets the formatted string to use for the folder name for the year that the <see cref="DateTime"/> is in.
		/// </summary>
		private string getYearFormatted(DateTime dateTime)
		{
			return dateTime.ToString("yyyy");
		}

		/// <summary>
		/// Gets the formatted string to use for the folder name for the month that the <see cref="DateTime"/> is in.
		/// </summary>
		private string getMonthFormatted(DateTime dateTime)
		{
			return CommandRunner.photoMonthFolderNames[dateTime.Month - 1];
		}

		/// <summary>
		/// Gets a formatted string for how a day's date should be formatted.
		/// </summary>
		private string getDayFormatted(DateTime dateTime)
		{
			return dateTime.ToString("MM-dd-yyyy");
		}

		#endregion Commands

		#endregion Methods
	}
}
