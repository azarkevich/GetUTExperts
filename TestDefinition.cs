using System.Collections.Generic;
using SharpSvn;

namespace GetUTExperts
{
	class TestDefinition
	{
		public string TeamCityId;
		public string TeamCityFullName;
		public string TeamCityName;
		public string TeamCityHref;
		public bool IsIgnored;
		public bool IsAssigned;

		public string RelativePath;
		public string FullPath;
		public int LineNoStart;
		public int LineNoEnd;

		public SvnBlameEventArgs[] Blame;

		public List<Expert> Experts = new List<Expert>();
	}
}
