using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;

namespace GetUTExperts
{
	class TeamCityConnector
	{
		readonly string _restBasseUrl;
		readonly NetworkCredential _tcServerAccessCredentials;

		public TeamCityConnector(string teamCityServerUrl, string user, string password)
		{
			_tcServerAccessCredentials = new NetworkCredential(user, password);
			_restBasseUrl = new Uri(new Uri(teamCityServerUrl), "httpAuth/app/rest").ToString();
		}

		public List<TestDefinition> GetFailedIgnoredTests(long buildId)
		{
			var wc = new WebClient();
			wc.Credentials = _tcServerAccessCredentials;
			var allTestsUrl = string.Format("{0}/testOccurrences?locator=build:{1},count:999999", _restBasseUrl, buildId);
			Console.WriteLine("Request all tests: {0}", allTestsUrl);
			var tests = wc.DownloadString(allTestsUrl);

			var doc = new XmlDocument();
			doc.LoadXml(tests);

			var nodes = doc.SelectNodes(@"testOccurrences/testOccurrence[@status = 'FAILURE' or @ignored = 'true']");

			var badTests = new List<TestDefinition>();
			foreach (XmlElement testOccurence in nodes)
			{
				var isAssigned = testOccurence.Attributes["currentlyInvestigated"] != null && testOccurence.Attributes["currentlyInvestigated"].Value.ToLowerInvariant() == "true";
				var isIgnored = testOccurence.Attributes["ignored"] != null && testOccurence.Attributes["ignored"].Value.ToLowerInvariant() == "true";

				var name = testOccurence.Attributes["name"].Value;
				var fullName = name;

				var colon = name.IndexOf(':');
				if (colon != -1)
					name = name.Substring(colon + 1).Trim();

				var indBracket = name.IndexOf('(');
				if (indBracket != -1)
					name = name.Substring(0, indBracket);

				var test = new TestDefinition {
					TeamCityFullName = fullName,
					TeamCityName = name.Trim(),
					TeamCityId = testOccurence.Attributes["id"].Value,
					TeamCityHref = testOccurence.Attributes["href"].Value,
					IsAssigned = isAssigned,
					IsIgnored = isIgnored
				};

				if (badTests.All(t => !IsTestsEqual(t, test)))
					badTests.Add(test);
			}

			return badTests;
		}

		static bool IsTestsEqual(TestDefinition t1, TestDefinition t2)
		{
			return t1.TeamCityName == t2.TeamCityName && t1.IsAssigned == t2.IsAssigned && t1.IsIgnored == t2.IsIgnored;
		}
	}
}
