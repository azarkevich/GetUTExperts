using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DetectTestExperts
{
	class TestsParser
	{
		readonly List<string> _errors;
		public TestsParser(List<string> errors)
		{
			_errors = errors;
		}

		public void Parse(string sourcesDir, List<TestDefinition> tests)
		{
			Console.WriteLine("Finding sources for {0} tests", tests.Count);

			var testClasses = GetAllTestClassesDeclarations(sourcesDir);
			var testMethods = GetTestMethods(testClasses).ToList();

			var testMethodsByFqn = testMethods
				.ToDictionary(m => GetFQN((ClassDeclarationSyntax)m.Parent) + "." + m.Identifier, m => m)
			;
			var testMethodsByN = testMethods
				.GroupBy(m => m.Identifier.ToString())
				.Where(g => g.Count() == 1)
				.ToDictionary(g => g.Key, g => g.First())
			;

			foreach (var tcTest in tests)
			{
				// try find method in sources
				MethodDeclarationSyntax m;
				if (!testMethodsByFqn.TryGetValue(tcTest.TeamCityName, out m) && !testMethodsByN.TryGetValue(tcTest.TeamCityName, out m))
				{
					var err = string.Format("Test not found in C# sources: {0}", tcTest.TeamCityName);
					Console.WriteLine("ERROR: " + err);
					_errors.Add(err);
					continue;
				}

				var lineSpan = m.GetLocation().GetLineSpan();

				tcTest.FullPath = lineSpan.Path;
				tcTest.RelativePath = tcTest.FullPath.Substring(sourcesDir.Length).Trim('/', '\\');
				tcTest.LineNoStart = lineSpan.StartLinePosition.Line;
				tcTest.LineNoEnd = lineSpan.EndLinePosition.Line;
			}
		}

		List<ClassDeclarationSyntax> GetAllTestClassesDeclarations(string dir)
		{
			var ret = new List<ClassDeclarationSyntax>();
			foreach (var csFile in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
			{
				try
				{
					var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(csFile), path: csFile);

					var root = (CompilationUnitSyntax)tree.GetRoot();

					foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
					{
						ret.Add(classDecl);
					}
				}
				catch (Exception ex)
				{
					_errors.Add(string.Format("ERROR: {0}", ex));
				}
			}

			// group classes by names
			ret = ret
				.GroupBy(GetFQN)
				// check that at least one class declaration contains attribute (other can be partial)
				.Where(g => {
					return g.Any(
						cd =>
						{
							return cd.AttributeLists
								.SelectMany(a => a.Attributes)
								.Select(a => a.Name.GetText().ToString())
								.Any(an => an == "TestClass" || an == "TestFixture" || an == "NUnit.Framework.TestFixtureAttribute")
							;
						}
					);
				})
				.SelectMany(g => g)
				.ToList()
			;

			return ret;
		}


		IEnumerable<MethodDeclarationSyntax> GetTestMethods(IEnumerable<ClassDeclarationSyntax> allClasses)
		{
			foreach (var classDecl in allClasses)
			{
				foreach (var methodDecl in classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>())
				{
					var isTestMethod = methodDecl.AttributeLists
						.SelectMany(a => a.Attributes)
						.Select(a => a.Name.GetText().ToString())
						.Any(an => an == "TestMethod" || an == "Test" || an == "Theory" || an == "TestAttribute" || an == "NUnit.Framework.TestAttribute")
					;

					if (!isTestMethod)
						continue;

					yield return methodDecl;
				}
			}
		}

		string GetFQN(ClassDeclarationSyntax cd)
		{
			var sb = new StringBuilder(cd.Identifier.ToString());
			var parent = cd.Parent;
			while (parent != null)
			{
				var namespaceDeclarationSyntax = parent as NamespaceDeclarationSyntax;
				if (namespaceDeclarationSyntax != null)
					sb.Insert(0, namespaceDeclarationSyntax.Name + ".");

				var classDeclarationSyntax = parent as ClassDeclarationSyntax;
				if (classDeclarationSyntax != null)
					sb.Insert(0, classDeclarationSyntax.Identifier + ".");

				parent = parent.Parent;
			}
			return sb.ToString();
		}

	}
}
