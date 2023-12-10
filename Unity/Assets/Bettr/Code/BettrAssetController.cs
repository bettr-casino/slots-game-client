using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CrayonScript.Code;
using CrayonScript.Interpreter;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace Bettr.Code
{
    public delegate void AssetLoadCompleteCallback(string assetBundleName, string assetBundleVersion,
        AssetBundle assetBundle, BettrAssetBundleManifest assetBundleManifest, bool success,
        bool previouslyLoaded, string error);

    public delegate void AssetBundleLoadCompleteCallback(string assetBundleName, AssetBundle assetBundle, bool success,
        bool previouslyLoaded, string error);

    public delegate void AssetBundleManifestLoadCompleteCallback(string assetName,
        BettrAssetBundleManifest assetBundleManifest, bool success,
        string error);


    [Serializable]
    public class BettrAssetPackageController
    {
        [NonSerialized] private BettrAssetController _bettrAssetController;
        [NonSerialized] private BettrAssetScriptsController _bettrAssetScriptsController;

        public BettrAssetPackageController(
            BettrAssetController bettrAssetController,
            BettrAssetScriptsController bettrAssetScriptsController)
        {
            TileController.RegisterType<BettrAssetPackageController>("BettrAssetPackageController");
            TileController.AddToGlobals("BettrAssetPackageController", this);

            _bettrAssetController = bettrAssetController;
            _bettrAssetScriptsController = bettrAssetScriptsController;
        }

        public IEnumerator LoadPackage(string packageName, string packageVersion, bool includeScenes)
        {
            var baseBundleName = $"{packageName}";
            var scenesBundleName = $"{packageName}_scenes";

            yield return _bettrAssetController.LoadAssetBundle(baseBundleName, packageVersion,
                (name, version, bundle, bundleManifest, success, loaded, error) =>
                {
                    if (success)
                    {
                        // preload and cache the scripts
                        _bettrAssetScriptsController.AddScriptsToTable(bundleManifest.Assets, bundle);
                    }
                    else
                    {
                        Debug.LogError(
                            $"Failed to load asset bundle={baseBundleName} version={packageVersion}: {error}");
                    }
                });

            if (includeScenes)
            {
                yield return _bettrAssetController.LoadAssetBundle(scenesBundleName, packageVersion,
                    (name, version, bundle, manifest, success, loaded, error) =>
                    {
                        if (!success)
                        {
                            Debug.LogError(
                                $"Failed to load asset bundle={scenesBundleName} version={packageVersion}: {error}");
                        }
                    });
            }
        }
    }

    [Serializable]
    public class BettrAssetPrefabsController
    {
        [NonSerialized] private BettrAssetController _bettrAssetController;
        [NonSerialized] private BettrUserController _bettrUserController;

        public BettrAssetPrefabsController(
            BettrAssetController bettrAssetController,
            BettrUserController bettrUserController)
        {
            TileController.RegisterType<BettrAssetPrefabsController>("BettrPrefabsController");
            TileController.AddToGlobals("BettrPrefabsController", this);

            _bettrAssetController = bettrAssetController;
            _bettrUserController = bettrUserController;
        }

        public IEnumerator LoadPrefab(string bettrAssetBundleName, string bettrAssetBundleVersion, string prefabName,
            GameObject parent = null)
        {
            yield return _bettrAssetController.LoadAssetBundle(bettrAssetBundleName, bettrAssetBundleVersion,
                (loadedAssetBundleName, loadedAssetBundleVersion, assetBundle, assetBundleManifest, success,
                    previouslyLoaded, error) =>
                {
                    if (success)
                    {
                        var prefab = assetBundle.LoadAsset<GameObject>(prefabName);
                        if (prefab == null)
                        {
                            Debug.LogError(
                                $"Failed to load prefab={prefabName} from asset bundle={loadedAssetBundleName} version={loadedAssetBundleVersion}");
                            return;
                        }

                        Object.Instantiate(prefab, parent == null ? null : parent.transform);
                    }
                    else
                    {
                        Debug.LogError(
                            $"Failed to load asset bundle={loadedAssetBundleVersion} version={loadedAssetBundleVersion}: {error}");
                    }
                });
        }
    }

    [Serializable]
    public class BettrAssetScenesController
    {
        [NonSerialized] private BettrAssetController _bettrAssetController;

        public BettrAssetScenesController(
            BettrAssetController bettrAssetController)
        {
            TileController.RegisterType<BettrAssetScenesController>("BettrAssetScenesController");
            TileController.AddToGlobals("BettrAssetScenesController", this);

            _bettrAssetController = bettrAssetController;
        }

        public bool LoadScene(string bettrAssetBundleName, string bettrAssetBundleVersion, string bettrSceneName)
        {
            var scenesBundleName = $"{bettrAssetBundleName}_scenes";
            var scenesBundleVersion = $"{bettrAssetBundleVersion}";

            var assetBundle = _bettrAssetController.GetCachedAssetBundle(scenesBundleName, scenesBundleVersion);

            if (assetBundle == null)
            {
                Debug.LogError(
                    $"Failed to load scene={bettrSceneName} from asset bundle={bettrAssetBundleName} version={bettrAssetBundleVersion}: asset bundle not loaded");
                return false;
            }

            var allScenePaths = assetBundle.GetAllScenePaths();
            var scenePath = string.IsNullOrWhiteSpace(bettrSceneName)
                ? allScenePaths[0]
                : allScenePaths.First(s => Path.GetFileNameWithoutExtension(s).Equals(bettrSceneName));

            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);

            return true;
        }
    }

    [Serializable]
    public class BettrAssetScriptsController
    {
        [NonSerialized] private BettrAssetController _bettrAssetController;
        public Dictionary<string, Table> ScriptsTables { get; private set; }

        public BettrAssetScriptsController(BettrAssetController bettrAssetController)
        {
            TileController.RegisterType<BettrAssetScriptsController>("BettrAssetScriptsController");
            TileController.AddToGlobals("BettrAssetScriptsController", this);

            _bettrAssetController = bettrAssetController;

            ScriptsTables = new Dictionary<string, Table>();
        }

        public Table GetScriptTable(string scriptName)
        {
            return ScriptsTables[scriptName];
        }

        public void ClearScripts()
        {
            ScriptsTables.Clear();
        }

        public IEnumerator LoadScripts(string bundleName, string bundleVersion)
        {
            var baseBundleName = $"{bundleName}";

            yield return _bettrAssetController.LoadAssetBundle(baseBundleName, bundleVersion,
                (name, version, bundle, bundleManifest, success, loaded, error) =>
                {
                    if (success)
                    {
                        // preload and cache the scripts
                        AddScriptsToTable(bundleManifest.Assets, bundle);
                    }
                    else
                    {
                        Debug.LogError(
                            $"Failed to load asset bundle={baseBundleName} version={bundleVersion}: {error}");
                    }
                });
        }

        public void AddScriptsToTable(string[] assetNames, AssetBundle assetBundle)
        {
            var scriptAssetNames = assetNames.Where(name => name.EndsWith(".cscript.txt")).ToArray();
            foreach (var scriptAssetName in scriptAssetNames)
            {
                var textAsset = assetBundle.LoadAsset<TextAsset>(scriptAssetName);
                var script = textAsset.text;
                var className = Path.GetFileNameWithoutExtension(scriptAssetName);
                try
                {
                    className = Path.GetFileNameWithoutExtension(className);
                    TileController.LoadScript(className, script);
                }
                catch (Exception e)
                {
                    Debug.LogError($"error loading script{scriptAssetName} class={className} error={e.Message}");
                    throw;
                }
            }

            foreach (var scriptAssetName in scriptAssetNames)
            {
                var className = Path.GetFileNameWithoutExtension(scriptAssetName);
                try
                {
                    className = Path.GetFileNameWithoutExtension(className);
                    var scriptTable = TileController.RunScript<Table>(scriptName: className);
                    ScriptsTables[className] = scriptTable;
                    // DEBUG SECTION
                    // var globals = TileController.LuaScript.Globals;
                    // var t = (Table) globals["Game001BaseGameFreeSpinsTriggerSummary"];
                    // if (t != null)
                    // {
                    //     Debug.Log($"DEBUG1 script={scriptAssetName} globals={t.TableToJson()}");
                    // }
                }
                catch (Exception e)
                {
                    Debug.LogError($"error running script={scriptAssetName} class={className} error={e.Message}");
                    throw;
                }
            }
        }
    }

    [Serializable]
    public class BettrAssetController
    {
        public bool useFileSystemAssetBundles = true;

        public string webAssetBaseURL;

        public string fileSystemAssetBaseURL = "Assets/Bettr/Tests/AssetBundles";

        private Dictionary<string, HashSet<string>> loadingHashes = new Dictionary<string, HashSet<string>>();

        private Dictionary<string, HashSet<BettrAssetBundleManifest>> loadingAssetBundleManifests =
            new Dictionary<string, HashSet<BettrAssetBundleManifest>>();

        private Dictionary<string, HashSet<string>> loadedHashes = new Dictionary<string, HashSet<string>>();

        private Dictionary<string, HashSet<BettrAssetBundleManifest>> loadedAssetBundleManifests =
            new Dictionary<string, HashSet<BettrAssetBundleManifest>>();

        public BettrAssetController()
        {
            TileController.RegisterType<BettrAssetController>("BettrAssetController");
            TileController.AddToGlobals("BettrAssetController", this);
        }

        public AssetBundle GetCachedAssetBundle(string bettrAssetBundleName, string bettrAssetBundleVersion)
        {
            var suffix = string.IsNullOrEmpty(bettrAssetBundleVersion) ? "" : $".{bettrAssetBundleVersion}";
            var assetBundleName = $"{bettrAssetBundleName}{suffix}";
            var cachedAssetBundleName = assetBundleName;

            var loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (var bundle in loadedBundles)
            {
                if (bundle.name == cachedAssetBundleName)
                {
                    return bundle;
                }
            }

            return null;
        }

        public BettrAssetBundleManifest GetCachedAssetBundleManifest(string bettrAssetBundleName,
            string bettrAssetBundleVersion)
        {
            if (!loadedAssetBundleManifests.ContainsKey(bettrAssetBundleName)) return null;
            var assetBundleManifests = loadedAssetBundleManifests[bettrAssetBundleName];
            foreach (var assetBundleManifest in assetBundleManifests)
            {
                if (assetBundleManifest.AssetBundleName == bettrAssetBundleName &&
                    assetBundleManifest.AssetBundleVersion == bettrAssetBundleVersion)
                {
                    return assetBundleManifest;
                }
            }

            return null;
        }

        public IEnumerator UnloadCachedAssetBundle(string bettrAssetBundleName, string bettrAssetBundleVersion)
        {
            var assetBundle = GetCachedAssetBundle(bettrAssetBundleName, bettrAssetBundleVersion);
            if (assetBundle == null)
            {
                yield break;
            }

            yield return assetBundle.UnloadAsync(true);

            ClearLoadedAssetBundleCache(bettrAssetBundleName);
        }

        public IEnumerator LoadAssetBundle(string bettrAssetBundleName, string bettrAssetBundleVersion,
            AssetLoadCompleteCallback callback)
        {
            var cachedAssetBundle = GetCachedAssetBundle(bettrAssetBundleName, bettrAssetBundleVersion);
            var cachedAssetBundleManifest = GetCachedAssetBundleManifest(bettrAssetBundleName, bettrAssetBundleVersion);
            if (cachedAssetBundle != null)
            {
                callback(bettrAssetBundleName, bettrAssetBundleVersion, cachedAssetBundle, cachedAssetBundleManifest,
                    true, true, null);
                yield break;
            }

            BettrAssetBundleManifest manifest = null;

            yield return LoadAssetBundleManifest(bettrAssetBundleName, bettrAssetBundleVersion,
                (assetBundleManifestName, assetBundleManifest, success, error) =>
                {
                    if (success)
                    {
                        manifest = assetBundleManifest;
                    }
                    else
                    {
                        Debug.LogError($"Failed to load asset bundle manifest {assetBundleManifestName}: {error}");
                    }
                });

            yield return LoadAssetBundle(manifest,
                ((name, bundle, success, loaded, error) =>
                {
                    callback(bettrAssetBundleName, bettrAssetBundleVersion, bundle, manifest, success, loaded,
                        error);
                }));
        }

        public IEnumerator LoadAssetBundle(BettrAssetBundleManifest assetBundleManifest,
            AssetBundleLoadCompleteCallback callback)
        {
            if (useFileSystemAssetBundles)
            {
                yield return LoadFileSystemAssetBundle(assetBundleManifest, callback);
            }
            else
            {
                yield return LoadWebAssetBundle(assetBundleManifest, callback);
            }
        }

        public IEnumerator LoadAssetBundleManifest(string bettrAssetBundleName, string bettrAssetBundleVersion,
            AssetBundleManifestLoadCompleteCallback callback)
        {
            if (useFileSystemAssetBundles)
            {
                yield return LoadFileSystemAssetBundleManifest(bettrAssetBundleName, bettrAssetBundleVersion, callback);
            }
            else
            {
                yield return LoadWebAssetBundleManifest(bettrAssetBundleName, bettrAssetBundleVersion, callback);
            }
        }

        IEnumerator LoadWebAssetBundle(BettrAssetBundleManifest assetBundleManifest,
            AssetBundleLoadCompleteCallback callback)
        {
            var suffix = string.IsNullOrEmpty(assetBundleManifest.AssetBundleVersion)
                ? ""
                : $".{assetBundleManifest.AssetBundleVersion}";
            var assetBundleName = $"{assetBundleManifest.AssetBundleName}{suffix}";
            var assetBundleHash = assetBundleManifest.Hashes.AssetFileHash.Hash;
            if (assetBundleManifest.HashAppended == 1)
            {
                assetBundleName = $"{assetBundleManifest.AssetBundleName}_{assetBundleHash}{suffix}";
            }

            var crc = assetBundleManifest.CRC;

            while (IsAssetBundleLoading(assetBundleManifest.AssetBundleName, assetBundleName))
            {
                yield return null;
            }

            if (IsAssetBundleLoaded(assetBundleManifest.AssetBundleName, assetBundleName))
            {
                var previouslyDownloadedAssetBundle = GetCachedAssetBundle(assetBundleManifest.AssetBundleName,
                    assetBundleManifest.AssetBundleVersion);
                if (previouslyDownloadedAssetBundle != null)
                {
                    callback(assetBundleName, previouslyDownloadedAssetBundle, true, true, null);
                    yield break;
                }
            }

            AddToLoadingAssetBundleCache(assetBundleManifest.AssetBundleName, assetBundleName, assetBundleManifest);

            var assetBundleURL = $"{webAssetBaseURL}/{assetBundleName}";
            using UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleURL, crc);
            float startTime = Time.realtimeSinceStartup;
            yield return www.SendWebRequest();
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            Debug.Log($"Network request bundle={assetBundleName} took {elapsedTime} seconds.");

            ClearLoadingAssetBundleCache(assetBundleManifest.AssetBundleName);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                callback(assetBundleName, null, false, false, www.error);
                yield break;
            }

            AssetBundle downloadedAssetBundle = DownloadHandlerAssetBundle.GetContent(www);
            if (downloadedAssetBundle == null)
            {
                var error = $"null bundle for webAssetName={assetBundleName}";
                Debug.LogError(error);
                callback(assetBundleName, null, false, false, error);
                yield break;
            }

            AddToLoadedAssetBundleCache(assetBundleManifest.AssetBundleName, assetBundleName, assetBundleManifest);

            callback(assetBundleName, downloadedAssetBundle, true, false, null);
        }

        IEnumerator LoadFileSystemAssetBundle(BettrAssetBundleManifest assetBundleManifest,
            AssetBundleLoadCompleteCallback callback)
        {
            var suffix = string.IsNullOrEmpty(assetBundleManifest.AssetBundleVersion)
                ? ""
                : $".{assetBundleManifest.AssetBundleVersion}";
            var assetBundleName = $"{assetBundleManifest.AssetBundleName}{suffix}";
            if (assetBundleManifest.HashAppended == 1)
            {
                assetBundleName =
                    $"{assetBundleManifest.AssetBundleName}_{assetBundleManifest.Hashes.AssetFileHash.Hash}{suffix}";
            }

            var crc = assetBundleManifest.CRC;

            if (IsAssetBundleLoaded(assetBundleManifest.AssetBundleName, assetBundleName))
            {
                var previouslyDownloadedAssetBundle = GetCachedAssetBundle(assetBundleManifest.AssetBundleName,
                    assetBundleManifest.AssetBundleVersion);
                if (previouslyDownloadedAssetBundle != null)
                {
                    callback(assetBundleName, previouslyDownloadedAssetBundle, true, true, null);
                    yield break;
                }
            }

            var assetBundleURL = $"{fileSystemAssetBaseURL}/{assetBundleName}";
            var downloadedAssetBundle = AssetBundle.LoadFromFile(assetBundleURL, crc);
            if (downloadedAssetBundle == null)
            {
                var error = $"null bundle for webAssetName={assetBundleName}";
                Debug.LogError(error);
                callback(assetBundleName, null, false, false, error);
                yield break;
            }

            AddToLoadedAssetBundleCache(assetBundleManifest.AssetBundleName, assetBundleName, assetBundleManifest);

            callback(assetBundleName, downloadedAssetBundle, true, false, null);
        }

        IEnumerator LoadWebAssetBundleManifest(string bettrAssetBundleName, string bettrAssetBundleVersion,
            AssetBundleManifestLoadCompleteCallback callback)
        {
            var suffix = string.IsNullOrEmpty(bettrAssetBundleVersion)
                ? ".manifest"
                : $".{bettrAssetBundleVersion}.manifest";
            var bettrBundleManifestName = $"{bettrAssetBundleName}{suffix}";

            var webAssetName = bettrBundleManifestName;

            var assetBundleURL = $"{webAssetBaseURL}/{webAssetName}";
            using UnityWebRequest www = UnityWebRequest.Get(assetBundleURL);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                callback(webAssetName, null, false, www.error);
                yield break;
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            var assetBundleManifestBytes = www.downloadHandler.data;

            var assetBundleText = Encoding.ASCII.GetString(assetBundleManifestBytes);

            var assetBundleManifest = deserializer.Deserialize<BettrAssetBundleManifest>(assetBundleText);
            assetBundleManifest.AssetBundleName = bettrAssetBundleName;
            assetBundleManifest.AssetBundleVersion = bettrAssetBundleVersion;

            callback(webAssetName, assetBundleManifest, true, null);
        }

        IEnumerator LoadFileSystemAssetBundleManifest(string bettrAssetBundleName, string bettrAssetBundleVersion,
            AssetBundleManifestLoadCompleteCallback callback)
        {
            var suffix = string.IsNullOrEmpty(bettrAssetBundleVersion)
                ? ".manifest"
                : $".{bettrAssetBundleVersion}.manifest";
            var bettrBundleManifestName = $"{bettrAssetBundleName}{suffix}";

            var fileSystemAssetName = bettrBundleManifestName;

            var assetBundleManifestURL = $"{fileSystemAssetBaseURL}/{fileSystemAssetName}";
            var assetBundleManifestBytes = File.ReadAllBytes(assetBundleManifestURL);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            var assetBundleText = Encoding.ASCII.GetString(assetBundleManifestBytes);

            var assetBundleManifest = deserializer.Deserialize<BettrAssetBundleManifest>(assetBundleText);
            assetBundleManifest.AssetBundleName = bettrAssetBundleName;
            assetBundleManifest.AssetBundleVersion = bettrAssetBundleVersion;

            callback(fileSystemAssetName, assetBundleManifest, true, null);
            yield break;
        }

        private void AddToLoadedAssetBundleCache(string bundleName, string assetBundleName,
            BettrAssetBundleManifest assetBundleManifest)
        {
            if (!loadedHashes.ContainsKey(bundleName))
            {
                loadedHashes[bundleName] = new HashSet<string>();
            }

            loadedHashes[bundleName].Add(assetBundleName);
            if (!loadedAssetBundleManifests.ContainsKey(bundleName))
            {
                loadedAssetBundleManifests[bundleName] = new HashSet<BettrAssetBundleManifest>();
            }

            loadedAssetBundleManifests[bundleName].Add(assetBundleManifest);
        }

        private void ClearLoadedAssetBundleCache(string bundleName)
        {
            loadedHashes.Remove(bundleName);
            loadedAssetBundleManifests.Remove(bundleName);
        }

        private bool IsAssetBundleLoaded(string bundleName, string bundleHash)
        {
            if (!loadedHashes.ContainsKey(bundleName)) return false;
            var hashes = loadedHashes[bundleName];
            foreach (var hash in hashes)
            {
                if (hash == bundleHash) return true;
            }

            return false;
        }

        private void AddToLoadingAssetBundleCache(string bundleName, string assetBundleName,
            BettrAssetBundleManifest assetBundleManifest)
        {
            if (!loadingHashes.ContainsKey(bundleName))
            {
                loadingHashes[bundleName] = new HashSet<string>();
            }

            loadingHashes[bundleName].Add(assetBundleName);
            if (!loadingAssetBundleManifests.ContainsKey(bundleName))
            {
                loadingAssetBundleManifests[bundleName] = new HashSet<BettrAssetBundleManifest>();
            }

            loadingAssetBundleManifests[bundleName].Add(assetBundleManifest);
        }

        private void ClearLoadingAssetBundleCache(string bundleName)
        {
            loadingHashes.Remove(bundleName);
            loadingAssetBundleManifests.Remove(bundleName);
        }

        private bool IsAssetBundleLoading(string bundleName, string bundleHash)
        {
            if (!loadingHashes.ContainsKey(bundleName)) return false;
            var hashes = loadingHashes[bundleName];
            foreach (var hash in hashes)
            {
                if (hash == bundleHash) return true;
            }

            return false;
        }
    }
}