using System;
using System.Collections;
using System.IO;
using System.Text;
using CrayonScript.Code;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable once CheckNamespace
namespace Bettr.Code
{
    [Serializable]
    public class BettrOutcomeController
    {
        [NonSerialized]  public bool UseFileSystemOutcomes = true;
        
        [NonSerialized] public string FileSystemOutcomesBaseURL = "Assets/Bettr/LocalStore/Outcomes";
        
        // ReSharper disable once UnassignedField.Global
        [NonSerialized]  public string WebOutcomesBaseURL;
        
        public int OutcomeNumber { get; set; }
        
        public BettrAssetScriptsController BettrAssetScriptsController { get; private set; }

        public BettrOutcomeController(BettrAssetScriptsController bettrAssetScriptsController)
        {
            TileController.RegisterType<BettrOutcomeController>("BettrOutcomeController");
            TileController.AddToGlobals("BettrOutcomeController", this);

            this.BettrAssetScriptsController = bettrAssetScriptsController;
        }
        
        public IEnumerator LoadServerOutcome(string gameId)
        {
            // Load the Outcomes
            if (UseFileSystemOutcomes)
            {
                yield return LoadFileSystemOutcome(gameId, OutcomeNumber);
            }
            else
            {
                yield return LoadWebOutcome(gameId, OutcomeNumber);
            }
        }
        
        IEnumerator LoadWebOutcome(string gameId, int outcomeNumber)
        {
            var className = $"{gameId}Outcome{outcomeNumber:09}";

            var assetBundleURL = $"{WebOutcomesBaseURL}/{className}.cscript.txt";
            using UnityWebRequest www = UnityWebRequest.Get(assetBundleURL);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                yield break;
            }

            var assetBundleManifestBytes = www.downloadHandler.data;

            var script = Encoding.ASCII.GetString(assetBundleManifestBytes);
            
            BettrAssetScriptsController.AddScriptToTable(className, script);
        }

        IEnumerator LoadFileSystemOutcome(string gameId, int outcomeNumber)
        {
            var className = $"{gameId}Outcome{outcomeNumber:D9}";
            var assetBundleManifestURL = $"{FileSystemOutcomesBaseURL}/{className}.cscript.txt";
            var assetBundleManifestBytes = File.ReadAllBytes(assetBundleManifestURL);

            var script = Encoding.ASCII.GetString(assetBundleManifestBytes);

            BettrAssetScriptsController.AddScriptToTable(className, script);
            
            yield break;
        }
    }
}