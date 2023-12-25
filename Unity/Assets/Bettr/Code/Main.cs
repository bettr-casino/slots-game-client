using System;
using System.Collections;
using System.Linq;
using Bettr.Core;
using CrayonScript.Code;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bettr.Code
{
    [Serializable]
    public class Main : MonoBehaviour
    {
        [SerializeField] private TextAsset configFile;

        [NonSerialized] private ConfigData _configData;
        [NonSerialized] private BettrServer _bettrServer;
        [NonSerialized] private BettrAssetController _bettrAssetController;
        [NonSerialized] private BettrUserController _bettrUserController;
        [NonSerialized] private BettrReelController _bettrReelController;
        [NonSerialized] private BettrVisualsController _bettrVisualsController;
        [NonSerialized] private BettrAssetScriptsController _bettrAssetScriptsController;
        [NonSerialized] private BettrAssetScenesController _bettrAssetScenesController;
        [NonSerialized] private BettrAssetPackageController _bettrAssetPackageController;
        [NonSerialized] private BettrAssetPrefabsController _bettrAssetPrefabsController;
        [NonSerialized] private BettrOutcomeController _bettrOutcomeController;
        
        private bool _oneTimeSetUpComplete = false;

        // Start is called before the first frame update
        IEnumerator Start()
        {
            yield return OneTimeSetup();

            yield return LoginUser();

            yield return LoadMainLobby();
        }

        private IEnumerator OneTimeSetup()
        {
            if (_oneTimeSetUpComplete) yield break;
            
            Debug.Log("OneTimeSetup started");

            TileController.RegisterModule("Bettr.dll");
            TileController.RegisterModule("BettrCore.dll");
            
            // load the config file
            _configData = ConfigReader.Parse(configFile.text);
            
            _bettrServer = new BettrServer(_configData.AssetsBaseURL);
            
            _bettrUserController = new BettrUserController(_bettrServer);
            
            var userId = _bettrUserController.GetUserId();

            var assetVersion = "";
            
            yield return _bettrServer.Get($"/users/{userId}/commit_hash.txt", (url, payload, success, error) =>
            {
                if (!success)
                {
                    Debug.LogError($"User JSON retrieved Success: url={url} error={error}");
                    return;
                }
                
                if (payload.Length == 0)
                {
                    Debug.LogError("empty payload retrieved from url={url}");
                    return;
                }
                
                assetVersion = System.Text.Encoding.UTF8.GetString(payload);
                
            });

            if (String.IsNullOrWhiteSpace(assetVersion))
            {
                Debug.LogError($"Unable to retrieve commit_hash for user url={userId}");
                yield break;
            }
            
            _configData.AssetsVersion = assetVersion;
            
            BettrModel.Init();

            _bettrAssetController = new BettrAssetController
            {
                webAssetBaseURL = _configData.WebAssetsBaseURL,
                useFileSystemAssetBundles = false,
            };
            
            _bettrReelController = new BettrReelController();
            
            _bettrVisualsController = new BettrVisualsController();
            
            _bettrAssetScriptsController = new BettrAssetScriptsController(_bettrAssetController);
            _bettrAssetScenesController = new BettrAssetScenesController(_bettrAssetController);
            
            _bettrAssetPackageController = new BettrAssetPackageController(_bettrAssetController, _bettrAssetScriptsController);
            _bettrAssetPrefabsController = new BettrAssetPrefabsController(_bettrAssetController, _bettrUserController);

            _bettrOutcomeController = new BettrOutcomeController(_bettrAssetScriptsController, _bettrUserController, _configData.AssetsVersion)
                {
                    UseFileSystemOutcomes = false,
                    WebOutcomesBaseURL = _configData.WebOutcomesBaseURL,
                };

            BettrVisualsController.SwitchOrientationToLandscape();
            
            Debug.Log("UnitySetUp");
            if (_oneTimeSetUpComplete) yield break;
            yield return _bettrAssetPackageController.LoadPackage(_configData.MainBundleName, _configData.MainBundleVariant, false);
            
            var mainTable = _bettrAssetScriptsController.GetScriptTable("Main");
            var scriptRunner = ScriptRunner.Acquire(mainTable);
            yield return scriptRunner.CallAsyncAction("Init");
            ScriptRunner.Release(scriptRunner);
            
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("OneTimeSetup ended");
            
            _oneTimeSetUpComplete = true;
        }

        private IEnumerator LoginUser()
        {
            var mainTable = _bettrAssetScriptsController.GetScriptTable("Main");
            var scriptRunner = ScriptRunner.Acquire(mainTable);
            yield return scriptRunner.CallAsyncAction("Login");
            ScriptRunner.Release(scriptRunner);
        }

        private IEnumerator LoadMainLobby()
        {
            var mainTable = _bettrAssetScriptsController.GetScriptTable("Main");
            var scriptRunner = ScriptRunner.Acquire(mainTable);
            yield return scriptRunner.CallAsyncAction("LoadLobbyScene");
            ScriptRunner.Release(scriptRunner);

            yield return UpdateCommitHash();
        }
        
        private IEnumerator UpdateCommitHash()
        {
            var activeScene = SceneManager.GetActiveScene();
            while (activeScene.name != "MainLobbyScene")
            {
                yield return null;
                activeScene = SceneManager.GetActiveScene();
            }
            
            var allRootGameObjects = activeScene.GetRootGameObjects();
            var appGameObject = allRootGameObjects.First((o => o.name == "App"));
            var appTile = appGameObject.GetComponent<Tile>();
            appTile.Call("SetCommitHash", _configData.AssetsVersion);
        }
    }
}




