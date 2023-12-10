using System;
using System.Collections;
using System.Text;
using CrayonScript.Code;
using Newtonsoft.Json;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Bettr.Code
{
    [Serializable]
    public class BettrUserController
    {
        public BettrServer BettrServer { get; private set; }
        
        public BettrUserConfig BettrUserConfig { get; private set; }
        
        public bool UserIsLoggedIn { get; private set; }

        public BettrUserController(BettrServer bettrServer)
        {
            TileController.RegisterType<BettrUserController>("BettrUserController");
            TileController.AddToGlobals("BettrUserController", this);
            
            TileController.RegisterType<BettrUserController>("BettrUserConfig");

            BettrServer = bettrServer;
        }

        public IEnumerator Login()
        {
            Debug.Log($"Starting User Login");
            
            // TODO: replace this with a real device ID
            // get the device ID
            var deviceId = "EE0DE516-5053-5142-80AC-2D878E91215C"; //SystemInfo.deviceUniqueIdentifier;
            var uniqueId = $"{deviceId}";

            BettrUserConfig = null;
            UserIsLoggedIn = false;

            byte[] downloadedPayload = null;
            
            yield return BettrServer.Get($"/users/{uniqueId}.json", (url, payload, success, error) =>
            {
                if (!success)
                {
                    Debug.LogError($"User JSON retrieved Success: url={url} error={error}");
                    return;
                }
                
                if (payload.Length == 0)
                {
                    Debug.LogError("empty payload retrieved from url={url}");
                }
                
                downloadedPayload = payload;

                
            });

            if (downloadedPayload != null)
            {
                BettrUserConfig = JsonConvert.DeserializeObject<BettrUserConfig>(Encoding.UTF8.GetString(downloadedPayload));
                TileController.AddToGlobals("BettrUser", BettrUserConfig);
                UserIsLoggedIn = true;
            }
        }
    }
}