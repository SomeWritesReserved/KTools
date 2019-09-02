using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KPhotoOrganizer
{
	public class Program
	{
		#region Fields

		private static readonly Regex jpegPropertySplitRegex = new Regex(":");

		#endregion Fields

		#region Methods

		public static void Main(string[] args)
		{
			try
			{
				Program.log($"---");
				Program.log($"{DateTime.Now}");
				Program.log($"Getting all files");
				List<PhotoInfo> allPhotos = Program.getAllPhotos(new DirectoryInfo(args[0]), new ProgressReporter());
				Program.log($"Got {allPhotos.Count} photos ({allPhotos.Where((p) => p.DateTaken.HasValue).Count()} with dates, {allPhotos.Where((p) => !p.DateTaken.HasValue).Count()} with guesses).");

				if (args.Contains("-y"))
				{
					Program.log($"Copying and organizing {allPhotos.Count} photos");
					Program.copyAndOrganizePhotos(allPhotos, new DirectoryInfo(args[1]), new ProgressReporter());
				}
			}
			catch (Exception exception)
			{
				Program.log("-----------");
				Program.log($"Exception ({exception.GetType().Name}): {exception.Message}");
			}
		}

		private static List<PhotoInfo> getAllPhotos(DirectoryInfo sourceDirectory, IProgress<int> progress)
		{
			FileInfo[] allJpegFiles = sourceDirectory.GetFiles("*.jpg", SearchOption.AllDirectories)
				.Concat(sourceDirectory.GetFiles("*.jpeg", SearchOption.AllDirectories))
				.OrderBy((file) => file.FullName)
				.ToArray();

			List<PhotoInfo> photos = new List<PhotoInfo>();
			int progressCount = 0;
			foreach (FileInfo jpegFile in allJpegFiles)
			{
				photos.Add(new PhotoInfo(jpegFile, Program.getDateTaken(jpegFile)));
				progress?.Report(100 * progressCount / allJpegFiles.Length);
				progressCount++;
			}
			return photos;
		}

		private static void copyAndOrganizePhotos(List<PhotoInfo> allPhotos, DirectoryInfo destinationDirectory, IProgress<int> progress)
		{
			int progressCount = 0;

			// First handle all the pictures with a known taken date
			foreach (PhotoInfo photo in allPhotos.Where((p) => p.DateTaken.HasValue))
			{
				string photoDestinationPath = Path.Combine(destinationDirectory.FullName,
					photo.DateTaken.Value.Year.ToString(),
					Program.getMonthString(photo.DateTaken.Value),
					Program.getDayString(photo.DateTaken.Value),
					photo.File.Directory.Name,
					photo.File.Name);

				Directory.CreateDirectory(Path.GetDirectoryName(photoDestinationPath));
				if (!File.Exists(photoDestinationPath))
				{
					photo.File.CopyTo(photoDestinationPath);
					photo.CopiedTo = photoDestinationPath;
					Program.save($"y|{photo.File.FullName}|{photoDestinationPath}");
				}
				else
				{
					if (!File.ReadAllBytes(photo.File.FullName).SequenceEqual(File.ReadAllBytes(photoDestinationPath)))
					{
						Program.log($"  '{photoDestinationPath}' already exists and is different!");
						Program.save($"d|{photo.File.FullName}|{photoDestinationPath}");
					}
				}
				progress?.Report(100 * progressCount / allPhotos.Count);
				progressCount++;
			}

			// Now do the pictures without a taken date
			foreach (PhotoInfo photo in allPhotos.Where((p) => !p.DateTaken.HasValue))
			{
				DateTime dateTaken = photo.File.LastWriteTime;


				string photoDestinationPath = Path.Combine(destinationDirectory.FullName,
					"Guesses",
					dateTaken.Year.ToString(),
					Program.getMonthString(dateTaken),
					Program.getDayString(dateTaken),
					photo.File.Directory.Name,
					photo.File.Name);

				Directory.CreateDirectory(Path.GetDirectoryName(photoDestinationPath));
				if (!File.Exists(photoDestinationPath))
				{
					photo.File.CopyTo(photoDestinationPath);
					photo.CopiedTo = photoDestinationPath;
					Program.save($"y|{photo.File.FullName}|{photoDestinationPath}");
				}
				else
				{
					if (!File.ReadAllBytes(photo.File.FullName).SequenceEqual(File.ReadAllBytes(photoDestinationPath)))
					{
						Program.log($"  '{photoDestinationPath}' already exists and is different!");
						Program.save($"d|{photo.File.FullName}|{photoDestinationPath}");
					}
				}
				progress?.Report(100 * progressCount / allPhotos.Count);
				progressCount++;
			}
		}

		private static DateTime? getDateTaken(FileInfo jpegFile)
		{
			using (FileStream fileStream = jpegFile.OpenRead())
			{
				using (Image image = Image.FromStream(fileStream, false, false))
				{
					PropertyItem propertyItem;
					try
					{
						propertyItem = image.GetPropertyItem(36867);
					}
					catch (ArgumentException)
					{
						return null;
					}
					string dateTaken = Program.jpegPropertySplitRegex.Replace(Encoding.UTF8.GetString(propertyItem.Value), "-", 2);
					return DateTime.Parse(dateTaken);
				}
			}
		}

		private static string getMonthString(DateTime dateTime)
		{
			switch (dateTime.Month)
			{
				case 1:
					return "01-January";
				case 2:
					return "02-February";
				case 3:
					return "03-March";
				case 4:
					return "04-April";
				case 5:
					return "05-May";
				case 6:
					return "06-June";
				case 7:
					return "07-July";
				case 8:
					return "08-August";
				case 9:
					return "09-September";
				case 10:
					return "10-October";
				case 11:
					return "11-November";
				case 12:
					return "12-December";
				default:
					throw new ArgumentException($"{0} is not a month.");
			}
		}

		private static string getDayString(DateTime dateTime)
		{
			return dateTime.ToString("MM-dd-yyyy");
		}

		private static void log(string message)
		{
			Console.WriteLine(message);
			File.AppendAllText("KPhotoOrganizer.log", message + Environment.NewLine);
		}

		private static void save(string message)
		{
			File.AppendAllText("Work.log", message + Environment.NewLine);
		}

		#endregion Methods

		#region Nested Types

		private class PhotoInfo
		{
			#region Constructors

			public PhotoInfo(FileInfo file, DateTime? dateTaken)
			{
				this.File = file;
				this.DateTaken = dateTaken;
			}

			#endregion Constructors

			#region Properties

			public FileInfo File { get; }

			public DateTime? DateTaken { get; }

			public string CopiedTo { get; set; }

			#endregion Properties
		}

		private class ProgressReporter : IProgress<int>
		{
			#region Fields

			private int previousReportedProgress = 0;

			#endregion Fields

			#region Methods

			void IProgress<int>.Report(int progress)
			{
				if (progress != this.previousReportedProgress)
				{
					Console.WriteLine($"{progress}%");
					this.previousReportedProgress = progress;
				}
			}

			#endregion Methods
		}

		#endregion Nested Types
	}
}
