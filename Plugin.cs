using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace ConquestBotMod
{
    public class ConquestBotMod : Modding.IModEntryPoint
    {
        void Modding.IModEntryPoint.PreLoad() {
            Debug.Log("Loading ConquestBotMod Version 0.0.0.1");
         }

        void Modding.IModEntryPoint.PostLoad()
        {
            Harmony harmony = new Harmony("org.conquestbot.plugin");
            harmony.PatchAll();
        }

        [HarmonyPatch( typeof(Game.SkirmishGameManager), "OnClientStopped")]
        public class SkirmishGameManger_OnClientStopped_Patch {
            public static void WriteJSON(Game.AfterActionReport AAR)
            {
                StreamWriter streamWriter = new StreamWriter(Application.streamingAssetsPath + string.Format("/{0}_{1}{2}_match.json", System.DateTime.Now.ToShortDateString().Replace("/", "-"), System.DateTime.Now.Hour, (System.DateTime.Now.Minute)));
                streamWriter.WriteLine("{");
                streamWriter.WriteLine(string.Format("    \"winner\": \"{0}\",", AAR.WinningTeam.ToString()));
                streamWriter.WriteLine("    \"teams\": [");
                foreach (Game.AfterActionReport.TeamReport teamReport in AAR.Teams)
                {
                    streamWriter.WriteLine("        \"" + teamReport.TeamID.ToString().ToLower() + "\": {");
                    foreach (Game.AfterActionReport.PlayerReport playerReport in teamReport.Players)
                    {
                        streamWriter.WriteLine("            \"" + playerReport.PlayerName.ToLower() + "\": {");
                        foreach (Game.AfterActionReport.ShipReport shipReport in playerReport.Ships)
                        {
                            streamWriter.WriteLine("                \"" + shipReport.ShipName + "\": {");
                            streamWriter.WriteLine("                    \"hullnumber\": \"" + shipReport.HullString + "\",");
                            //streamWriter.WriteLine("                    \"hulltype\": \"" + shipReport.HullType.Remove(shipReport.HullType.IndexOf('(')) + "\",");
                            streamWriter.WriteLine(string.Format(string.Format("                    \"offensivlyCapable\": {0},", (!shipReport.WasDefanged).ToString().ToLower())));
                            streamWriter.WriteLine(string.Format("                    \"eliminated\": \"{0}\",", shipReport.Eliminated));
                            streamWriter.WriteLine("                    \"partstatus\": [");
                            int num = 0;
                            int num2 = 0;
                            int num3 = 0;
                            foreach (string key in shipReport.PartStatus.Keys)
                            {
                                if (shipReport.PartStatus[key].IsDestroyed)
                                {
                                    num++;
                                }
                                else if (shipReport.PartStatus[key].HealthPercent > 0f && shipReport.PartStatus[key].HealthPercent < 1f)
                                {
                                    num2++;
                                }
                                else
                                {
                                    num3++;
                                }
                            }
                            streamWriter.WriteLine(string.Format(string.Format("                        \"destroyed\": {0},", num)));
                            streamWriter.WriteLine(string.Format(string.Format("                        \"damaged\": {0},", num2)));
                            streamWriter.WriteLine(string.Format(string.Format("                        \"undamaged\": {0}", num3)));
                            streamWriter.WriteLine("                    ]");
                            streamWriter.WriteLine("                }" + ((playerReport.Ships.IndexOf(shipReport) == playerReport.Ships.Count - 1) ? "" : ","));
                        }
                        streamWriter.WriteLine("            }" + ((teamReport.Players.IndexOf(playerReport) == teamReport.Players.Count - 1) ? "" : ","));
                    }
                    streamWriter.WriteLine("        }" + ((AAR.Teams.IndexOf(teamReport) == AAR.Teams.Count - 1) ? "" : ","));
                }
                streamWriter.WriteLine("    ]");
                streamWriter.WriteLine("}");
                streamWriter.Close();
            }
            
            [HarmonyPostfix]
            public static void Postfix(Game.SkirmishGameManager __instance) {
                Game.AfterActionReport aar = Traverse.Create(__instance).Field("_aarStarted").GetValue() as Game.AfterActionReport;
                WriteJSON(aar);
            }
        }
    }
}
