using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Dependencies.ClrPh;
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Children)]
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]

namespace Dependencies.Test
{
    [SetUpFixture]
    public class TestEnvironment
    {
        [OneTimeSetUp]
        public static void InitializeNativeLibrary()
        {
            // Native initialization changes process-wide state; finish it before any tests run.
            Assert.That(Phlib.InitializePhLib(), Is.True, "Could not initialize phlib.");
        }

        public static string TestDataPath(string fileName)
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", fileName);
        }

        public static (int ExitCode, string StandardOutput, string StandardError) RunCli(params string[] arguments)
        {
            var result = Cli.Wrap(Path.Combine(TestContext.CurrentContext.TestDirectory, "Dependencies.exe"))
                .WithArguments(arguments)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync()
                .GetAwaiter()
                .GetResult();
            return (result.ExitCode, result.StandardOutput, result.StandardError);
        }
    }
}
