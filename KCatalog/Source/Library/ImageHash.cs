using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog
{
	public class ImageHash
	{
		#region Constructors

		public ImageHash()
		{
		}

		#endregion Constructors

		#region Properties



		#endregion Properties

		#region Methods

		public static ImageHash CreateFrom(Pixel[,] imageData)
		{
			int width = imageData.GetLength(0);
			int height = imageData.GetLength(1);

			int leftSection = width / 2;
			int rightSection = width - leftSection;
			int topSection = height / 2;
			int bottomSection = height - topSection;

			// This works by splitting the image into quadrants, producing the hash for each quadrant and those hashes are
			// concatenated. The four quadrants are hashed clockwise. Within each quadrant we hash the bytes in a clockwise way:
			// Top-left quadrant hashes bytes left-to-right then top-to-bottom. Top-right quadrant hashes bytes top-to-bottom then right-to-left.
			// Bottom-right quadrant hashes bytes right-to-left then bottom-to-top. Bottom-left quadrant hashes bytes bottom-to-top then left-to-right.
			// This allows an image hash to be "circular" such that rotations don't affect the output except for where the hash begins.
			// This also largely ignore aspect ratio or dimensions as each quadrant is treated like a square as is the whole image.
			return null;
		}

		#endregion Methods
	}

	public struct Pixel
	{
		public byte R;
		public byte G;
		public byte B;
	}
}
