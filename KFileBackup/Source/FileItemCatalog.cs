using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace KFileBackup
{
	/// <summary>
	/// Represents a catalog of unique <see cref="FileItem"/> objects, addressable by each file's contents.
	/// </summary>
	public class FileItemCatalog : IEnumerable<FileItem>, IEnumerable
	{
		#region Fields

		public static readonly int FileFormatVersion = 3;
		private static readonly int fileFormatPreamble = BitConverter.ToInt32(Encoding.ASCII.GetBytes("KBKC"), 0);

		private readonly Dictionary<Hash, FileItem> fileItems = new Dictionary<Hash, FileItem>();

		#endregion Fields

		#region Properties

		public int Count { get { return this.fileItems.Values.Count; } }

		#endregion Properties

		#region Methods

		/// <summary>
		/// Adds a new <see cref="FileItem"/> to this catalog, throwing an exception if it already exists.
		/// </summary>
		public void Add(FileItem fileItem)
		{
			this.fileItems.Add(fileItem.Hash, fileItem);
		}

		/// <summary>
		/// Adds or merges the given <see cref="FileItem"/> into this catalog, adding if it doesn't exist or combining
		/// the paths of the given <see cref="FileItem"/> with the <see cref="FileItem"/> already in the catalog.
		/// </summary>
		public AddOrMergeResult AddOrMerge(FileItem fileItem)
		{
			if (this.TryGetValue(fileItem.Hash, out FileItem existingFileItem))
			{
				AddOrMergeResult addOrMergeResult = AddOrMergeResult.Same;
				foreach (FileLocation fileLocation in fileItem.FileLocations)
				{
					if (existingFileItem.FileLocations.Add(fileLocation)) { addOrMergeResult = AddOrMergeResult.Merged; }
				}
				return addOrMergeResult;
			}
			else
			{
				this.Add(fileItem);
				return AddOrMergeResult.Added;
			}
		}

		/// <summary>
		/// Finds all files in a directory and adds them to this catalog. Returns the results for each file found.
		/// </summary>
		public List<FileResult<CatalogFileResult>> CatalogFilesInDirectory(string directory, string searchPattern, string volumeName, bool isFromReadOnlyVolume, Action<string> log)
		{
			log.Invoke("Finding files...");
			string[] allFiles = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
			log.Invoke($"Found {allFiles.Length} files.");

			log.Invoke("Getting file hashes...");
			int processedFileCount = 0;
			List<FileResult<CatalogFileResult>> catalogFileResults = new List<FileResult<CatalogFileResult>>();
			foreach (string file in allFiles)
			{
				FileItem fileItem = null;
				CatalogFileResult catalogFileResult;
				try
				{
					fileItem = FileItem.CreateFromPath(file, volumeName, isFromReadOnlyVolume);
					AddOrMergeResult addOrMergeResult = this.AddOrMerge(fileItem);
					catalogFileResult = (CatalogFileResult)addOrMergeResult;
					log.Invoke($"{addOrMergeResult}\t{fileItem.Hash}\t{file}");
				}
				catch (IOException ioException)
				{
					catalogFileResult = CatalogFileResult.Skipped;
					log.Invoke($"Skipped ({ioException.Message}). {file}");
				}

				catalogFileResults.Add(this.CreateFileResult(file, catalogFileResult, fileItem));
				processedFileCount++;
				if ((processedFileCount % 20) == 0) { log.Invoke($"{processedFileCount / (double)allFiles.Length:0.0%} - {processedFileCount} of {allFiles.Length} files..."); }
			}
			log.Invoke($"Cataloged {allFiles.Length} files ({catalogFileResults.Count((res) => res.Result == CatalogFileResult.Same)} same, {catalogFileResults.Count((res) => res.Result == CatalogFileResult.Added)} added, {catalogFileResults.Count((res) => res.Result == CatalogFileResult.Merged)} merged, {catalogFileResults.Count((res) => res.Result == CatalogFileResult.Skipped)} skipped).");
			return catalogFileResults;
		}

		/// <summary>
		/// Checks all files in a directory to see if they are already in this catalog. Returns the results for each file found.
		/// </summary>
		public List<FileResult<CheckFileResult>> CheckFilesInDirectory(string directory, string searchPattern, bool shouldLogAll, Action<string> log)
		{
			log.Invoke("Finding files...");
			string[] allFiles = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
			log.Invoke($"Found {allFiles.Length} files.");

			log.Invoke("Getting file hashes...");
			int processedFileCount = 0;
			long newFilesTotalLengthInBytes = 0;
			List<FileResult<CheckFileResult>> checkFileResults = new List<FileResult<CheckFileResult>>();
			foreach (string file in allFiles)
			{
				FileItem fileItem = null;
				CheckFileResult checkFileResult;
				try
				{
					fileItem = FileItem.CreateFromPath(file, string.Empty, false);
					checkFileResult = this.TryGetValue(fileItem.Hash, out _) ? CheckFileResult.Exists : CheckFileResult.New;
					if (checkFileResult == CheckFileResult.New || shouldLogAll)
					{
						log.Invoke($"{checkFileResult}\t{fileItem.Hash}\t{file}\t({fileItem.FileSize.Value})");
					}
					if (checkFileResult == CheckFileResult.New) { newFilesTotalLengthInBytes += fileItem.FileSize.Value; }
				}
				catch (IOException ioException)
				{
					checkFileResult = CheckFileResult.Skipped;
					log.Invoke($"Skipped ({ioException.Message}). {file}");
				}

				checkFileResults.Add(this.CreateFileResult(file, checkFileResult, fileItem));
				processedFileCount++;
				if ((processedFileCount % 20) == 0) { log.Invoke($"{processedFileCount / (double)allFiles.Length:0.0%} - {processedFileCount} of {allFiles.Length} files..."); }
			}
			log.Invoke($"Checked {allFiles.Length} files ({checkFileResults.Count((res) => res.Result == CheckFileResult.Exists)} exist, {checkFileResults.Count((res) => res.Result == CheckFileResult.New)} new ({newFilesTotalLengthInBytes}), {checkFileResults.Count((res) => res.Result == CheckFileResult.Skipped)} skipped).");
			return checkFileResults;
		}

		/// <summary>
		/// Gets the <see cref="FileItem"/> associated with the specified <see cref="Hash"/>, returning whether or not
		/// the <see cref="FileItem"/> exists.
		/// </summary>
		public bool TryGetValue(Hash hash, out FileItem fileItem)
		{
			return this.fileItems.TryGetValue(hash, out fileItem);
		}

		public IEnumerator<FileItem> GetEnumerator()
		{
			return this.fileItems.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#region Helpers

		/// <summary>
		/// Writes the catalog to a file.
		/// </summary>
		public void WriteCatalogToFile(string catalogFile)
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(catalogFile, FileMode.Create, FileAccess.Write)))
			{
				binaryWriter.Write(FileItemCatalog.fileFormatPreamble);
				binaryWriter.Write(FileItemCatalog.FileFormatVersion);

				binaryWriter.Write(this.Count);
				foreach (FileItem fileItem in this)
				{
					Hash.Write(fileItem.Hash, binaryWriter);
					binaryWriter.Write(fileItem.FileLocations.Count);
					foreach (FileLocation fileLocation in fileItem.FileLocations)
					{
						binaryWriter.Write(fileLocation.FullPath);
						binaryWriter.Write(fileLocation.VolumeName);
						binaryWriter.Write(fileLocation.IsFromReadOnlyVolume);
					}
				}
			}
		}

		/// <summary>
		/// Reads a catalog from a file and merged it with this catalog.
		/// </summary>
		public void ReadCatalogFromFile(string catalogFile)
		{
			using (BinaryReader binaryReader = new BinaryReader(new FileStream(catalogFile, FileMode.Open, FileAccess.Read)))
			{
				int fileFormatType = binaryReader.ReadInt32();
				if (fileFormatType != FileItemCatalog.fileFormatPreamble) { throw new InvalidDataException("File is not a catalog file."); }
				int fileFormatVersion = binaryReader.ReadInt32();
				if (fileFormatVersion != FileItemCatalog.FileFormatVersion) { throw new InvalidDataException($"File format {fileFormatVersion} of the catalog file is not recognized."); }

				int fileItemCount = binaryReader.ReadInt32();
				foreach (int fileItemIndex in Enumerable.Range(0, fileItemCount))
				{
					FileItem fileItem = new FileItem(Hash.Read(binaryReader));
					int fileLocationCount = binaryReader.ReadInt32();
					foreach (int fileLocationIndex in Enumerable.Range(0, fileLocationCount))
					{
						string fullPath = binaryReader.ReadString();
						string volumeName = binaryReader.ReadString();
						bool isFromReadOnlyVolume = binaryReader.ReadBoolean();
						fileItem.FileLocations.Add(new FileLocation(fullPath, volumeName, isFromReadOnlyVolume));
					}
					this.AddOrMerge(fileItem);
				}
			}
		}

		private FileResult<T> CreateFileResult<T>(string file, T result, FileItem fileItem)
		{
			return new FileResult<T>(file, result, fileItem);
		}

		#endregion Helpers

		#endregion Methods
	}

	/// <summary>
	/// A value representing the result of <see cref="FileItemCatalog.AddOrMerge"/>, whether a
	/// <see cref="FileItem"/> already existed, was newly added, or was merged with an
	/// existing <see cref="FileItem"/>.
	/// </summary>
	public enum AddOrMergeResult
	{
		/// <summary>Everything was already identical, no action was taken.</summary>
		Same = 0,
		/// <summary>A new <see cref="FileItem"/> was added to the catalog.</summary>
		Added = 1,
		/// <summary>An existing <see cref="FileItem"/> was updated and merged.</summary>
		Merged = 2,
	}

	/// <summary>
	/// A value representing the result of <see cref="FileItemCatalog.CatalogFilesInDirectory"/>, whether a
	/// <see cref="FileItem"/> already existed, was newly added, was merged with an
	/// existing <see cref="FileItem"/>, or skipped.
	/// </summary>
	/// <remarks>The enum values with the same name in <see cref="AddOrMergeResult"/> but have the same numeric values.</remarks>
	public enum CatalogFileResult
	{
		/// <summary>Everything was identical, no action was taken (the exact same file in the same location is already cataloged).</summary>
		Same = 0,
		/// <summary>A new unique <see cref="FileItem"/> was added to the catalog (no other file matches the file).</summary>
		Added = 1,
		/// <summary>An existing <see cref="FileItem"/> was updated and merged (the file is identical to an existing cataloged file but in a different location).</summary>
		Merged = 2,
		/// <summary>Something went wrong (like couldn't read the file), and the file was skipped.</summary>
		Skipped = 3,
	}

	/// <summary>
	/// A value representing the result of <see cref="FileItemCatalog.CheckFilesInDirectory"/>,
	/// whether a file already exists, is new, or skipped.
	/// </summary>
	public enum CheckFileResult
	{
		/// <summary>The file exists in the catalog.</summary>
		Exists = 0,
		/// <summary>A new file that is not in the catalog.</summary>
		New = 1,
		/// <summary>Something went wrong (like couldn't read the file), and the file was skipped.</summary>
		Skipped = 2,
	}

	public class FileResult<T>
	{
		#region Constructors

		public FileResult(string file, T result, FileItem fileItem)
		{
			this.File = file;
			this.Result = result;
			this.FileItem = fileItem;
		}

		#endregion Constructors

		#region Properties

		public string File { get; }

		public T Result { get; }

		public FileItem FileItem { get; }

		#endregion Properties
	}
}
