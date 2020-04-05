using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog
{
	public static class ExtensionMethods
	{
		#region Methods

		public static string GetRelativePath(this IFileSystemInfo fileOrDirectory, IDirectoryInfo baseDirectoryInfo)
		{
			string relativePath = fileOrDirectory.FullName.Substring(baseDirectoryInfo.FullName.Length);
			if (relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
			{
				relativePath = relativePath.Substring(1);
			}
			return relativePath;
		}

		#endregion Methods
	}
}
