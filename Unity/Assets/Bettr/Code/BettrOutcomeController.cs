using System;
using System.Collections;
using System.Linq;
using CrayonScript.Code;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace Bettr.Code
{
    [Serializable]
    public class BettrOutcomeController
    {
        public string OutcomeId { get; set; }
        
        public BettrAssetScriptsController BettrAssetScriptsController { get; private set; }

        public BettrOutcomeController(BettrAssetScriptsController bettrAssetScriptsController)
        {
            TileController.RegisterType<BettrOutcomeController>("BettrOutcomeController");
            TileController.AddToGlobals("BettrOutcomeController", this);

            this.BettrAssetScriptsController = bettrAssetScriptsController;

            this.OutcomeId = "game001outcome1";
        }
        
        public IEnumerator LoadServerOutcome(string gameId)
        {
            // Load the Test Outcomes
            yield return BettrAssetScriptsController.LoadScripts(OutcomeId, "");
        }
    }
}