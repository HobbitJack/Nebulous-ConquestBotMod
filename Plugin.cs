using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace ConquestBotMod
{
    public class ConquestBotMod : Modding.IModEntryPoint {
        public struct ExtraShipData {
            public string HullName { get; set; }
            public string HullType { get; set; }
            public List<ShipComponent> Components { get; set; }
        }
        public struct ShipComponent {
            public string Name { get; set; }
            public string Key { get; set; }
            public List<string> Special { get; set; }
        }

        public static List<ExtraShipData> ShipList = new List<ExtraShipData>();
        
        public static object getPrivate(object obj, string privatefield)
		{
			return Traverse.Create(obj).Field(privatefield).GetValue();
		}

        void Modding.IModEntryPoint.PreLoad() {
            Debug.Log("Loading ConquestBotMod Version 0.0.0.5");
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
                            ExtraShipData ESD = ShipList.Find(CESD => CESD.HullName == shipReport.ShipName);
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
                            streamWriter.WriteLine(string.Format(string.Format("                        \"undamaged\": {0},", num3)));
                            streamWriter.WriteLine("                        \"components\": {");
                            foreach (ShipComponent component in ESD.Components) {
                                streamWriter.WriteLine($"                            \"{component.Name}\": {{");
                                streamWriter.WriteLine($"                                \"key\": \"{component.Key}\",");
                                if (shipReport.PartStatus.ContainsKey(component.Key)) {
                                streamWriter.WriteLine($"                                \"health\": {shipReport.PartStatus[component.Key].HealthPercent * 100},");
                                streamWriter.WriteLine($"                                \"destroyed\": {shipReport.PartStatus[component.Key].IsDestroyed.ToString().ToLower()}" + (component.Special.Count == 0 ? "" : ","));
                                }
                                else {
                                    streamWriter.WriteLine("ERROR: SOMETHING WENT WRONG PULLING THIS COMPONENT'S DATA");
                                }
                                if (component.Special != null) {
                                    foreach (string specialString in component.Special) {
                                        streamWriter.WriteLine($"                                {specialString}");
                                    }
                                }
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
                Game.AfterActionReport aar = getPrivate(__instance, "_aarStarted") as Game.AfterActionReport;
                Game.Team<Game.SkirmishPlayer>[] teams = getPrivate(__instance, "_teams") as Game.Team<Game.SkirmishPlayer>[];
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
                                    ESD.HullName = ship.ShipName;
                                    ESD.HullType = ship.Hull.ToString();
                                    ESD.Components = new List<ShipComponent>();
                                    foreach(Ships.HullPart hullPart in ship.Hull.GetAllParts().Values) {
                                        ShipComponent component = new ShipComponent();
                                        component.Name = hullPart.UIName;
                                        component.Key = hullPart.RpcKey;
                                        component.Special = new List<string>();
                                        //Start looking for special things, specifically ammo for now and later restores
                                        foreach(Ships.HullComponent magcomponent in ship.Hull.GetAllComponents()) {
                                            if (magcomponent.RpcKey == component.Key) {
                                                if ((magcomponent.GetType() == typeof(Ships.BulkMagazineComponent) || magcomponent.GetType() == typeof(Ships.CellLauncherComponent))) {
                                                    List<Munitions.Magazine> magazine = getPrivate(magcomponent, "_magazines") as List<Munitions.Magazine>;
                                                    if (magazine == null) {
                                                        magazine = getPrivate(magcomponent, "_missiles") as List<Munitions.Magazine>;
                                                    }
                                                    if(magazine != null) {
                                                        component.Special.Add("\"ammotypes\": {");
                                                        foreach(Munitions.Magazine ammoType in magazine) {
                                                            component.Special.Add($"    \"{ammoType.AmmoType.ToString().Remove(ammoType.AmmoType.ToString().IndexOf('(') - 1)}\": {ammoType.QuantityAvailable}" + (magazine.IndexOf(ammoType) == magazine.Count - 1 ? "" : ","));
                                                        }
                                                        component.Special.Add("}");
                                                    }
                                                }
                                                else if (magcomponent.GetType() == typeof(Ships.DCLockerComponent)) {
													component.Special.Add(string.Format($"\"restores\": {(int)ConquestBotMod.getPrivate(magcomponent, "_restoresRemaining")},"));
													component.Special.Add(string.Format($"\"dcteams\": {(int)ConquestBotMod.getPrivate(magcomponent, "_teamsProduced")}"));
                                                }
                                            }
                                        }
                                        ESD.Components.Add(component);
                                    }
                                    ShipList.Add(ESD);
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
