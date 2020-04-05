using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KCatalog.Tests
{
	public static class TestRunner
	{
		#region Methods

		public static void RunTests()
		{
			int testsRunCount = 0;
			int failedTestCount = 0;
			int problemCount = 0;
			foreach (Type testSuiteType in Assembly.GetExecutingAssembly().GetTypes()
				.Where((type) => type.Namespace == "KCatalog.Tests" && type.Name.EndsWith("Tests")))
			{
				Console.WriteLine("Running {0}...", testSuiteType.Name);
				foreach (MethodInfo testMethod in testSuiteType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					try
					{
						testsRunCount++;
						object testClassInstance = Activator.CreateInstance(testSuiteType);
						try
						{
							testMethod.Invoke(testClassInstance, null);
						}
						catch (TargetInvocationException targetInvocationException)
						{
							throw targetInvocationException.InnerException;
						}
						Console.WriteLine("   {0}", testMethod.Name);
					}
					catch (AssertTestException assertTestException)
					{
						Console.WriteLine(" ! {0} failed: {1}", testMethod.Name, assertTestException.Message);
						failedTestCount++;
					}
					catch (Exception exception)
					{
						Console.WriteLine(" # {0} PROBLEM: {1} - {2}", testMethod.Name, exception.GetType().Name, exception.Message);
						problemCount++;
					}
				}
			}
			Console.WriteLine($"Done. {testsRunCount} tests, {failedTestCount} failures, {problemCount} problems.");
		}

		#endregion Methods
	}
}
