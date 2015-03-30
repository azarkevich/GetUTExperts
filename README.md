# GetUTExperts
Return .NET Unit Tests authorship with help of SVN Blame
Intended for integration with TeamCity

Algorithm:
* Read list of tests from TeamCity
* Scan directory for MSTest, NUnit in *.cs files. Parse it with help of Roslyn and detect begin/end of test methods
* Run SVN blame for each test and gather authors statistic
* Generate html report

![TeamCity Report](https://github.com/azarkevich/GetUTExperts/blob/master/doc/GetUTExprts.png)
