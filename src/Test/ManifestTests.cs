using NUnit.Framework;

namespace Dependencies.Test;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ManifestTests
{
    // Namespace redefinition, a resource index greater than one, and quoted
    // assembly identities previously covered by Test-ManifestRegress.ps1.
    [TestCase("changepk.exe", "<requestedExecutionLevel level=\"requireAdministrator\" />")]
    [TestCase("SystemSettings.exe", "<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" xmlns:asmv3=\"urn:schemas-microsoft-com:asm.v3\" manifestVersion=\"1.0\">")]
    [TestCase("DevicePairingFolder.dll", "<assemblyIdentity name=\"Microsoft.Windows.Shell.DevicePairingFolder\" processorArchitecture=\"amd64\"")]
    public void PrintsEmbeddedManifest(string fileName, string expectedManifest)
    {
        var (exitCode, standardOutput, standardError) = TestEnvironment.RunCli("-manifest", TestEnvironment.TestDataPath(fileName));

        Assert.That(standardOutput, Does.Contain(expectedManifest).IgnoreCase);
        Assert.That(standardOutput, Does.Not.Contain("System.Xml.XmlException"));
    }
}
