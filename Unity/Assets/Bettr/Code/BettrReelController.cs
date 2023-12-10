using System;
using CrayonScript.Code;

// ReSharper disable once CheckNamespace
namespace Bettr.Code
{
    [Serializable]
    public class BettrReelController
    {
        public BettrReelController()
        {
            TileController.RegisterType<BettrReelController>("BettrReelController");
            TileController.AddToGlobals("BettrReelController", this);
        }
    }
}