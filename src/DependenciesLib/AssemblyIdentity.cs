using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Dependencies;

// <assemblyIdentity name="NetFx4-legacy_web_minimaltrust_config_default" version="4.0.15840.3" processorArchitecture="x86" language="neutral" buildType="release" publicKeyToken="b03f5f7f11d50a3a" />
public class AssemblyIdentity
{
    public AssemblyIdentity(string processorArchitecture, string name, string publicKeyToken, Version version, string language)
    {
        ProcessorArchitecture = processorArchitecture.Trim().ToLowerInvariant();
        Name = name;
        PublicKeyToken = publicKeyToken.Trim().ToLowerInvariant();
        Version = version;
        Language = language.Trim().ToLowerInvariant();
    }

    public AssemblyIdentity(XElement xElem)
    {
        ProcessorArchitecture = xElem.Attribute("processorArchitecture") != null ? xElem.Attribute("processorArchitecture").Value.ToLowerInvariant() : "*";
        Name = xElem.Attribute("name").Value.ToLowerInvariant();
        PublicKeyToken = xElem.Attribute("publicKeyToken").Value.ToLowerInvariant();
        Version = Version.Parse(xElem.Attribute("version").Value);
        Language = xElem.Attribute("language") != null ? xElem.Attribute("language").Value.ToLowerInvariant() : "*";
    }

    /// <summary>
    /// msil, x86, ia64, amd64, arm in <see cref="System.Reflection.ProcessorArchitecture"/>,
    /// wow64, arm64
    /// </summary>
    public string ProcessorArchitecture { get; }
    public string Name { get; }
    public string PublicKeyToken { get; }
    public Version Version { get; }
    /// <summary>
    /// fr, zh-cn, neutral, or *
    /// will be 'none' or 'x-ww'(XP) in path
    /// </summary>
    public string Language { get; }

    public override bool Equals(object obj)
    {
        return obj is AssemblyIdentity identity &&
               ProcessorArchitecture == identity.ProcessorArchitecture &&
               Name == identity.Name &&
               PublicKeyToken == identity.PublicKeyToken &&
               EqualityComparer<Version>.Default.Equals(Version, identity.Version) &&
               Language == identity.Language;
    }

    public override int GetHashCode()
    {
        int hashCode = 1084222224;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProcessorArchitecture);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PublicKeyToken);
        hashCode = hashCode * -1521134295 + EqualityComparer<Version>.Default.GetHashCode(Version);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Language);
        return hashCode;
    }
}
