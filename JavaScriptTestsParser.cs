using System;
using System.Collections.Generic;

namespace GetUTExperts
{
	class JavaScriptTestsParser
	{
		readonly List<string> _errors;
		public JavaScriptTestsParser(List<string> errors)
		{
			_errors = errors;
		}

		public void Parse(string sourcesDir, List<TestDefinition> tests)
		{
			Console.WriteLine("Finding sources for {0} tests", tests.Count);


		}
	}
}
