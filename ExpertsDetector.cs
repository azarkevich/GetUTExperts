using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SharpSvn;

namespace DetectTestExperts
{
	class ExpertsDetector
	{
		readonly Uri _svnBaseUrl;
		readonly SvnClient _svnClient;
		readonly List<string> _errors;

		public ExpertsDetector(string svnBaseUrl, string svnUser, string svnPassword, List<string> errors)
		{
			_svnBaseUrl = new Uri(svnBaseUrl.Trim('/', '\\') + '/');
			_svnClient = new SvnClient();
			_svnClient.Authentication.SslServerTrustHandlers += (sender, e) =>
			{
				e.AcceptedFailures = e.Failures;
				e.Save = true;
			};
			_svnClient.Authentication.ForceCredentials(svnUser, svnPassword);
			_errors = errors;
		}

		public void Detect(List<TestDefinition> tests)
		{
			tests = tests.Where(t => t.FullPath != null).ToList();

			Console.WriteLine("Finding experts for {0} tests", tests.Count);

			foreach (var testGroup in tests.GroupBy(t => t.RelativePath))
			{
				FindExpertsFor(testGroup.Key, testGroup.ToList());
			}
		}

		void FindExpertsFor(string file, IList<TestDefinition> tests)
		{
			Console.WriteLine("Finding experts for: {0} ({1} tests)", file, tests.Count());

			Collection<SvnBlameEventArgs> blameEvents;

			var blameArgs = new SvnBlameArgs {
				RetrieveMergedRevisions = true,
				IgnoreLineEndings = true,
				IgnoreMimeType = true,
				IgnoreSpacing = SvnIgnoreSpacing.IgnoreAll
			};

			var url = new Uri(_svnBaseUrl, file.Replace('\\', '/'));

			_svnClient.GetBlame(new SvnUriTarget(url), blameArgs, out blameEvents);

			foreach (var test in tests)
			{
				test.Blame = blameEvents
					.Skip(test.LineNoStart)
					.Take(test.LineNoEnd - test.LineNoStart)
					.ToArray()
				;

				CalculateAuthorship(test);
			}
		}

		void CalculateAuthorship(TestDefinition test)
		{
			test.Experts = test.Blame
				.GroupBy(b => b.MergedAuthor)
				.Select(g => new Expert { AuthoredLines = g.Count(), Name = g.Key })
				.OrderByDescending(e => e.AuthoredLines)
				.ToList()
			;

			// normalize
			var totalLines = test.Experts.Sum(e => e.AuthoredLines);
			test.Experts.ForEach(e => e.Authorship = (double)e.AuthoredLines / totalLines);
		}
	}
}
