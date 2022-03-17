using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace ConquestBotMod
{
    public class ConquestBotMod : Modding.IModEntryPoint {
        public struct ExtraShipData {
            public string HullID { get; set; }
            public string HullType { get; set; }
            public List<ShipComponent> Components { get; set; }
        }
        public struct ShipComponent {
            public string Name { get; set; }
            public string Key { get; set; }
        }
        public static List<ExtraShipData> Ships = new List<ExtraShipData>();
        void Modding.IModEntryPoint.PreLoad() {
            Debug.Log("Loading ConquestBotMod Version 0.0.0.2");
         }

        void Modding.IModEntryPoint.PostLoad()
        {
            Harmony harmony = new Harmony("org.conquestbot.plugin");
            harmony.PatchAll();
        }

        [HarmonyPatch( typeof(Game.SkirmishGameManager), "OnClientStopped")]
        public class SkirmishGameManger_OnClientStopped_Patch {

            
            private static void WriteJSON(Game.AfterActionReport AAR)
            {
                StreamWriter streamWriter = new StreamWriter(Application.streamingAssetsPath + string.Format("/{0}_{1}{2}_match.json", System.DateTime.Now.ToShortDateString().Replace("/", "-"), System.DateTime.Now.Hour.ToString().PadLeft(2, '0'), System.DateTime.Now.Minute.ToString().PadLeft(2, '0')));
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
                            Debug.Log("LoadingESD");
                            Debug.Log(Ships.Count);
                            foreach(ExtraShipData CESD in Ships) {
                                Debug.Log(CESD.HullID);
                            }
                            ExtraShipData ESD = Ships.Find(CESD => CESD.HullID == shipReport.HullString);
                            Debug.Log(ESD.HullID);
                            streamWriter.WriteLine("                \"" + shipReport.ShipName + "\": {");
                            streamWriter.WriteLine("                    \"hullnumber\": \"" + shipReport.HullString + "\",");
                            streamWriter.WriteLine("                    \"hulltype\": \"" + ESD.HullType.Remove(ESD.HullType.IndexOf('(')) + "\",");
                            streamWriter.WriteLine(string.Format(string.Format("                    \"offensivlyCapable\": {0},", (!shipReport.WasDefanged).ToString().ToLower())));
                            streamWriter.WriteLine(string.Format("                    \"eliminated\": \"{0}\",", shipReport.Eliminated));
                            streamWriter.WriteLine("                    \"partsummary\": {");
                            int num = 0;
                            int num2 = 0;
                            int num3 = 0;
                            foreach (string key in shipReport.PartStatus.Keys)
                            {
                                if (shipReport.PartStatus[key].IsDestroyed)
                                {
                                    num++;
                                }
                                else if (shipReport.PartStatus[key].HealthPercent < 1f)
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
                            streamWriter.WriteLine("                        \"components\": {");
                            foreach (ShipComponent component in ESD.Components) {
                                streamWriter.WriteLine($"                            \"{component.Name}\": {{");
                                streamWriter.WriteLine($"                                \"key\": \"{component.Key}\",");
                                streamWriter.WriteLine($"                                \"health\": {shipReport.PartStatus[component.Key].HealthPercent * 100},");
                                streamWriter.WriteLine($"                                \"destroyed\": {shipReport.PartStatus[component.Key].IsDestroyed.ToString().ToLower()}");
                                streamWriter.WriteLine($"                            }}" + ((ESD.Components.IndexOf(component) == ESD.Components.Count - 1) ? "" : ","));
                            }
                            streamWriter.WriteLine("                        }");
                            streamWriter.WriteLine("                    }");
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
                Game.Team<Game.SkirmishPlayer>[] teams = Traverse.Create(__instance).Field("_teams").GetValue() as Game.Team<Game.SkirmishPlayer>[];
                foreach (Game.Team<Game.SkirmishPlayer> team in teams)
			    {
                    if (team.TeamID != Utility.TeamIdentifier.None)
                    {
                        foreach (Game.SkirmishPlayer skirmishPlayer in team.Players)
                        {
                            if (skirmishPlayer.PlayerFleet != null)
                            {
                                foreach (Ships.Ship ship in skirmishPlayer.PlayerFleet.FleetShips)
                                {
                                    ExtraShipData ESD = new ExtraShipData();
                                    ESD.HullID = ship.FullHullNumber;
                                    ESD.HullType = ship.Hull.ToString();
                                    ESD.Components = new List<ShipComponent>();
                                    foreach(Ships.HullPart hullPart in ship.Hull.GetAllParts().Values) {
                                        ShipComponent component = new ShipComponent();
                                        component.Name = hullPart.UIName;
                                        component.Key = hullPart.RpcKey;
                                        ESD.Components.Add(component);
                                    }
                                    Ships.Add(ESD);
                                }
                            }
                        }
                    }
			    }
                WriteJSON(aar);
            }
        }
    }
}
