using System;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Bettr.Code
{
    [Serializable]
    public class ConfigData
    {
        public string AssetsVersion { get; set; }
        
        public string AssetsBaseURL { get; set; }
        public string MainBundleName { get; set; }
        public string MainBundleVariant { get; set; }
        
        public string OutcomesBaseURL { get; set; }
        
#if UNITY_iOS
        public string WebAssetsBaseURL => $"{AssetsBaseURL}/assets/{AssetsVersion}/iOS";
#endif
#if UNITY_Android
        public string WebAssetsBaseURL => $"{AssetsBaseURL}/assets/{AssetsVersion}/Android";
#endif
#if UNITY_WebGL
        public string WebAssetsBaseURL => $"{AssetsBaseURL}/assets/{AssetsVersion}/WebGL";
#endif
#if UNITY_StandaloneOSX
        public string WebAssetsBaseURL => $"{AssetsBaseURL}/assets/{AssetsVersion}/OSX";
#endif
#if UNITY_EDITOR
        public string WebAssetsBaseURL => $"{AssetsBaseURL}/assets/{AssetsVersion}/OSX";
#endif
        
        public string WebOutcomesBaseURL => $"{OutcomesBaseURL}";
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

