using System;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Bettr.Code
{
    [Serializable]
    public class ConfigData
    {
        public string AssetVersion { get; set; }
        
        public string AssetsBaseURL { get; set; }
        public string MainBundleName { get; set; }
        public string MainBundleVariant { get; set; }
        
        public string WebAssetsBaseURL => $"{AssetsBaseURL}/{AssetVersion}";
    }

    public static class ConfigReader
    {
        public static ConfigData Parse(string yamlText)
        {
            if (yamlText == null)
            {
                Debug.LogError("Config.yaml yamlText is not assigned.");
                return null;
            }

            DeserializerBuilder deserializerBuilder = new DeserializerBuilder();
            var deserializer = deserializerBuilder.Build();

            // Deserialize the YAML content from configFile.text into a C# data structure
            using var reader = new StringReader(yamlText);
            var configData = deserializer.Deserialize<ConfigData>(reader);

            return configData;
        }
    }
}

