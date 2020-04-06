using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog
{
	public class Program
	{
		#region Fields

		public static string SoftwareVersion { get; } = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

		#endregion Fields

		#region Methods

		public static void Main(string[] args)
		{
			try
			{
				if (args.Length == 1 && args[0].Equals("--test", StringComparison.OrdinalIgnoreCase))
				{
					Tests.TestRunner.RunTests();
				}
				else
				{
					new CommandRunner(new FileSystem(), Console.Out, Console.In).Run(args);
				}
			}
			catch (OperationCanceledException operationCanceledException)
			{
				Console.WriteLine(operationCanceledException.Message);
			}
			catch (CommandLineArgumentException) { }
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

		#endregion Methods
	}
}
