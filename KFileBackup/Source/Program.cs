using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KFileBackup
{
	public class Program
	{
		#region Methods

		public static void Main(string[] args)
		{
			if (args.Length != 1 || !args[0].Contains(";")) { return; }

			string argument = args[0].Split(';')[0];
			string value = args[0].Split(';')[1];

			//Program.catalogLocalFiles(value, "*");
			FileDatabase fileDatabase = new FileDatabase();
			fileDatabase.AddFilesFromDatabaseFile("local.bkdb");

			var broken = fileDatabase.FileItems.Where((f) => f.FileLocations.Count <= 0).ToArray();
			var dups = fileDatabase.FileItems.Where((f) => f.FileLocations.Count > 1).ToArray();
		}

		private static void catalogLocalFiles(string path, string searchPattern)
		{
			Program.log("Finding files...");
			string[] allFiles = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
			Program.log("Found {0} files.", allFiles.Length);

			Program.log("Getting file hashes...");
			FileItemCollection fileItems = new FileItemCollection();
			int count = 0;
			foreach (string file in allFiles)
			{
				FileItem newFileItem;
				try
				{
					newFileItem = FileItem.CreateFromPath(file, false);
				}
				catch (IOException)
				{
					Program.log("Couldn't access file, ignoring. {0}", file);
					continue;
				}
				if (fileItems.TryGetValue(newFileItem.Hash, out FileItem existingFileItem))
				{
					existingFileItem.FileLocations.Add(newFileItem.FileLocations.Single());
				}
				else
				{
					fileItems.Add(newFileItem);
				}
				count++;
				if ((count % 20) == 0) { Program.log("{0:0.0%} - {1} of {2} files...", (double)count / (double)allFiles.Length, count, allFiles.Length); }
			}

			Program.log("Saving database...");
			using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream("local.bkdb", FileMode.Create, FileAccess.Write)))
			{
				binaryWriter.Write(fileItems.Count);
				foreach (FileItem fileItem in fileItems)
				{
					binaryWriter.Write(fileItem.Hash.Value);
					binaryWriter.Write(fileItem.FileLocations.Count);
					foreach (FileLocation fileLocation in fileItem.FileLocations)
					{
						binaryWriter.Write(fileLocation.FullPath);
						binaryWriter.Write(fileLocation.IsFromReadOnlyLocation);
					}
				}
			}
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

		#endregion Methods
	}
}
