using System;
using System.IO;
using NUnit.Framework;

namespace Dependencies.Test
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class CommandLineTests
    {
        [TestCase("-knowndll", "ntdll.dll")]
        [TestCase("-apisets", "api-ms-win-")]
        public void PrintsSystemInformation(string command, string expectedOutput)
        {
            var (exitCode, standardOutput, standardError) = TestEnvironment.RunCli(command);

            Assert.That(standardOutput, Does.Contain(expectedOutput).IgnoreCase);
        }

        [Test]
        public void PrintsSideBySideEntries()
        {
            string executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "ctfmon.exe");
            var (exitCode, standardOutput, standardError) = TestEnvironment.RunCli("-sxsentries", executable);

            Assert.That(standardOutput, Does.Contain("ctfmon.exe").IgnoreCase);
            Assert.That(standardOutput, Does.Contain("[+] comctl32.dll").IgnoreCase);
        }

        [TestCase("apisetschema.v2.dll")]
        [TestCase("apisetschema.v4.dll")]
        [TestCase("apisetschema.v6.dll")]
        public void PrintsApiSetSchema(string fileName)
        {
            var (exitCode, standardOutput, standardError) = TestEnvironment.RunCli("-apisetsdll", TestEnvironment.TestDataPath(fileName));

            // The v2/v4 readers omit the "api-" prefix used by the v6 reader.
            Assert.That(standardOutput, Does.Contain("ms-win-core-handle-l1-1-0 -> [ kernelbase.dll ]"));
        }
    }
}
