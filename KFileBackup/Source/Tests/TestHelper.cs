using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KFileBackup.Tests
{
	public static class TestHelper
	{
		#region Methods

		public static Hash Hash(int shortHash)
		{
			return new Hash(shortHash, 0, 0, 0);
		}

		#endregion Methods
	}
}
