using YamlDotNet.Serialization;

// ReSharper disable once CheckNamespace
namespace Bettr.Code
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class BettrAssetBundleManifest
    {
        public long ManifestFileVersion { get; set; }

        public uint CRC { get; set; }

        public Hashes Hashes { get; set; }

        public long HashAppended { get; set; }

        public ClassType[] ClassTypes { get; set; }

        public object[] SerializeReferenceClassIdentifiers { get; set; }

        public string[] Assets { get; set; }

        public object[] Dependencies { get; set; }
        
        public string AssetBundleName { get; set; }
        
        public string AssetBundleVersion { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ClassType
    {
        public long Class { get; set; }

        public Script Script { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Script
    {
        [YamlMember(ApplyNamingConventions = false, Alias = "instanceID")]
        public long InstanceId { get; set; }
        
        [YamlMember(ApplyNamingConventions = false, Alias = "fileID")]
        public long FileId { get; set; }
        
        [YamlMember(ApplyNamingConventions = false, Alias = "guid")]
        // ReSharper disable once InconsistentNaming
        public string GUID { get; set; }
        
        [YamlMember(ApplyNamingConventions = false, Alias = "type")]
        public long Type { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class Hashes
    {
        public EHash AssetFileHash { get; set; }

        public EHash TypeTreeHash { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class EHash
    {
        [YamlMember(ApplyNamingConventions = false, Alias = "serializedVersion")]
        public long SerializedVersion { get; set; }

        public string Hash { get; set; }
    }
}
