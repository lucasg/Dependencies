using System;
using System.IO;
using NUnit.Framework;

namespace Dependencies.Test
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class BinaryCacheTests
    {
        private string cacheDirectory;
        private BinaryCacheImpl cache;

        [SetUp]
        public void SetUp()
        {
            cacheDirectory = Path.Combine(Path.GetTempPath(), "Dependencies.Tests", Guid.NewGuid().ToString("N"));
            cache = new BinaryCacheImpl(cacheDirectory, 0);
        }

        [TearDown]
        public void TearDown()
        {
            cache.Unload();
        }

        [TestCase("apisetschema.v2.dll")]
        [TestCase("apisetschema.v4.dll")]
        [TestCase("apisetschema.v6.dll")]
        [TestCase("changepk.exe")]
        [TestCase("SystemSettings.exe")]
        [TestCase("DevicePairingFolder.dll")]
        public void LoadsAndReusesBinaryWithoutLockingOriginal(string fileName)
        {
            string originalPath = Path.Combine(cacheDirectory, fileName);
            File.Copy(TestEnvironment.TestDataPath(fileName), originalPath);

            var binary = cache.GetBinary(originalPath);

            Assert.That(binary, Is.Not.Null);
            Assert.That(binary.LoadSuccessful, Is.True);
            Assert.That(binary.Filepath, Is.EqualTo(originalPath));
            Assert.That(cache.GetBinary(originalPath), Is.SameAs(binary));

            // A mapped original would prevent exclusive access
            using var _ = File.Open(originalPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }

        [Test]
        public void MissingBinaryReturnsNull()
        {
            Assert.That(cache.GetBinary(Path.Combine(cacheDirectory, "missing.dll")), Is.Null);
        }
    }
}
