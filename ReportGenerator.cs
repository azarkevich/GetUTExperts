using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace GetUTExperts
{
	class ReportGenerator
	{
		readonly TextWriter _tw;

		public ReportGenerator(TextWriter tw)
		{
			_tw = tw;
		}

		public void Generate(List<TestDefinition> tests, double trustThresold, List<string> errors)
		{
			tests = tests.Where(t => t.Experts.Count > 0).ToList();
			Console.WriteLine("Generate report for {0} tests", tests.Count);

			_tw.WriteLine(@"
<html>
<head>
</head>
<body>
<tt>
");

			if (errors.Count > 0)
			{
				_tw.WriteLine("<font color=red><h2>Errors</h2><br/>");
				foreach (var error in errors)
				{
					_tw.WriteLine("&nbsp;&nbsp;{0}<br>", WebUtility.HtmlEncode(error).Replace(" ", "&nbsp;").Replace("\n", "<br/>"));
				}
				_tw.WriteLine("<br/></font>");
			}

			var failedTests = tests.Where(t => !t.IsIgnored).ToList();
			if (failedTests.Count > 0)
				DumpTests("Failed tests", failedTests, trustThresold);

			var ignoredTests = tests.Where(t => t.IsIgnored).ToList();
			if (ignoredTests.Count > 0)
				DumpTests("Ignored tests", ignoredTests, trustThresold);

			_tw.WriteLine(@"
</tt>
</body>
</html>
");
		}

		void DumpTests(string sectionCaption, List<TestDefinition> tests, double trustThresold)
		{
			_tw.WriteLine("<h2>{0}</h2><br/>", sectionCaption);

			var trusted = tests.Where(t => t.Experts.First().Authorship >= trustThresold).ToList();
			var untrusted = tests.Where(t => t.Experts.First().Authorship < trustThresold).ToList();

			Console.WriteLine("{0}: Dump experts for {1} / {2} tests", sectionCaption, trusted.Count, untrusted.Count);

			foreach (var testGroup in trusted.GroupBy(t => t.Experts.First().Name))
			{
				_tw.WriteLine("{0}:", testGroup.Key);
				_tw.WriteLine("</br>");

				foreach (var test in testGroup.OrderByDescending(t => t.Experts.First().Authorship))
					WriteTestInfo(test);
				_tw.WriteLine("</br>");
				_tw.WriteLine("</br>");
			}

			foreach (var test in untrusted)
				WriteTestInfo2(test);
		}

		void WriteTestInfo(TestDefinition test)
		{
			var bestExper = test.Experts.First();
			var perc = string.Format("{0:0.0}%", 100 * bestExper.Authorship);
			perc = perc.PadLeft(7, ' ').Replace(" ", "&nbsp;");
			_tw.Write(test.IsAssigned ? "*&nbsp;" : "&nbsp;&nbsp;");
			_tw.Write(perc);
			_tw.WriteLine(" {0}", test.TeamCityName);
			_tw.WriteLine("</br>");
		}

		void WriteTestInfo2(TestDefinition test)
		{
			_tw.WriteLine("{0}", test.TeamCityName);
			_tw.WriteLine("</br>");
			foreach (var expert in test.Experts)
			{
				var perc = string.Format("{0:0.0}%", 100 * expert.Authorship);
				perc = perc.PadLeft(7, ' ').Replace(" ", "&nbsp;");

				_tw.Write(test.IsAssigned ? "*&nbsp;" : "&nbsp;&nbsp;");
				_tw.WriteLine("{0} {1}", perc, expert.Name);
				_tw.WriteLine("</br>");
			}
			_tw.WriteLine("</br>");
		}
	}
}
