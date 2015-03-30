using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DetectTestExperts
{
	class Program
	{
		static void Main(string[] args)
		{
			var teamCityUrl = "";
			var teamCityUser = "";
			var teamCityPassword = "";
			var sourcesBaseDir = "";
			var svnBaseUrl = "";
			var outPath = "report-tests-authorhip.html";
			var svnUser = "";
			var svnPassword = "";
			var buildId = 0;
			var timeout = 30;
			var analyzeAssigned = false;
			var javaScript = false;

			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "--tc-url":
						teamCityUrl = args[++i];
						break;
					case "--tc-user":
						teamCityUser = args[++i];
						break;
					case "--tc-pass":
						teamCityPassword = args[++i];
						break;
					case "--tc-build-id":
						buildId = Int32.Parse(args[++i]);
						break;
					case "--tc-retry-timeout":
						timeout = Int32.Parse(args[++i]);
						break;
					case "--svn-user":
						svnUser = args[++i];
						break;
					case "--svn-pass":
						svnPassword = args[++i];
						break;
					case "--sources-dir":
						sourcesBaseDir = args[++i];
						break;
					case "--svn-base-url":
						svnBaseUrl = args[++i];
						break;
					case "--out":
						outPath = args[++i];
						break;
					case "--analyze-assigned":
						analyzeAssigned = true;
						break;
					case "--javascript-ut":
						javaScript = true;
						break;
				}
			}

			Console.WriteLine("Settings:");
			Console.WriteLine("	TC URL: {0}", teamCityUrl);
			Console.WriteLine("	TC User: {0}", teamCityUser);
			Console.WriteLine("	TC BuildId: {0}", buildId);
			Console.WriteLine("	Svn URL: {0}", svnBaseUrl);
			Console.WriteLine("	Svn User: {0}", svnUser);
			Console.WriteLine("	Sources dir: {0}", sourcesBaseDir);
			Console.WriteLine("	Output: {0}", outPath);
			Console.WriteLine("	Analyze Assigned: {0}", analyzeAssigned);
			Console.WriteLine("	JavaScript UT: {0}", javaScript);
			Console.WriteLine();

			var errors = new List<string>();

			var tests = new TeamCityConnector(teamCityUrl, teamCityUser, teamCityPassword).GetFailedIgnoredTests(buildId);
			while (true)
			{
				Thread.Sleep(TimeSpan.FromSeconds(timeout));

				var oldTests = tests;
				tests = new TeamCityConnector(teamCityUrl, teamCityUser, teamCityPassword).GetFailedIgnoredTests(buildId);

				if (oldTests.Count == tests.Count)
					break;

				Console.WriteLine("Tests count increased: {0} -> {1}. Retry tests list after several seconds", oldTests.Count, tests.Count);
			}

			Console.WriteLine("Found {0} tests (Ignored: {1}, Assigned: {2}, Failed: {3})",
				tests.Count, tests.Count(t => t.IsIgnored), tests.Count(t => t.IsAssigned), tests.Count(t => !t.IsAssigned && !t.IsIgnored)
			);

			var forFindInSources = tests.Where(t => analyzeAssigned || !t.IsAssigned).ToList();
			if(javaScript)
			{
				// find sources for UTs
				new TestsParser(errors).Parse(sourcesBaseDir, forFindInSources);
			}
			else
			{
				// find sources for UTs
				new TestsParser(errors).Parse(sourcesBaseDir, forFindInSources);
			}

			// detect experts
			new ExpertsDetector(svnBaseUrl, svnUser, svnPassword, errors).Detect(tests);

			using (var tw = File.CreateText(outPath))
			{
				new ReportGenerator(tw).Generate(tests, 0.8, errors);
			}
		}
	}
}
