using System;
using System.Collections;
using CrayonScript.Code;
using UnityEngine;
using UnityEngine.Assertions;

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
        [NonSerialized] private BettrController _bettrController;
        [NonSerialized] private BettrAssetScriptsController _bettrAssetScriptsController;
        [NonSerialized] private BettrAssetScenesController _bettrAssetScenesController;
        [NonSerialized] private BettrAssetPackageController _bettrAssetPackageController;
        [NonSerialized] private BettrAssetPrefabsController _bettrAssetPrefabsController;
        
        private bool _oneTimeSetUpComplete = false;

        // Start is called before the first frame update
        IEnumerator Start()
        {
            yield return OneTimeSetup();
            
            // setup the server
        }
        
        private IEnumerator OneTimeSetup()
        {
            if (_oneTimeSetUpComplete) yield break;
            
            Debug.Log("OneTimeSetup started");
            
            // load the config file
            _configData = ConfigReader.Parse(configFile.text);
            
            BettrModel.Init();

            _bettrServer = new BettrServer(_configData.AssetsBaseURL);
            
            _bettrAssetController = new BettrAssetController
            {
                webAssetBaseURL = _configData.WebAssetsBaseURL,
                useFileSystemAssetBundles = false,
            };

            _bettrUserController = new BettrUserController(_bettrServer);
            _bettrReelController = new BettrReelController();
            
            _bettrController = new BettrController();
            
            _bettrAssetScriptsController = new BettrAssetScriptsController(_bettrAssetController);
            _bettrAssetScenesController = new BettrAssetScenesController(_bettrAssetController);
            
            _bettrAssetPackageController = new BettrAssetPackageController(_bettrAssetController, _bettrAssetScriptsController);
            _bettrAssetPrefabsController = new BettrAssetPrefabsController(_bettrAssetController, _bettrUserController);

            BettrController.SwitchOrientationToLandscape();
            
            Debug.Log("UnitySetUp");
            if (_oneTimeSetUpComplete) yield break;
            yield return _bettrAssetPackageController.LoadPackage(_configData.MainBundleName, _configData.MainBundleVariant, false);
            
            var mainTable = _bettrAssetScriptsController.GetScriptTable("Main");
            var scriptRunner = ScriptRunner.Acquire(mainTable);
            yield return scriptRunner.CallAsyncAction("Init");
            ScriptRunner.Release(scriptRunner);
            
            Debug.Log("OneTimeSetup ended");
            
            _oneTimeSetUpComplete = true;
        }
    }
}




