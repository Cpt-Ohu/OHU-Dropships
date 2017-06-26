using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld.Planet;
using RimWorld;
using System.Reflection;

namespace OHUShips
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.ohu.ships.main");

            harmony.Patch(AccessTools.Method(typeof(RimWorld.FactionGenerator), "GenerateFactionsIntoWorld"), null, new HarmonyMethod(typeof(HarmonyPatches), "GenerateFactionsIntoWorldPostFix"));

            harmony.Patch(AccessTools.Property(typeof(MapPawns), "AnyPawnBlockingMapRemoval").GetGetMethod(false), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(AnyColonistTameAnimalOrPrisonerOfColonyPostFix)), null);

            harmony.Patch(AccessTools.Property(typeof(TransferableOneWay), "MaxCount").GetGetMethod(false), new HarmonyMethod(typeof(HarmonyPatches), nameof(MaxCountTransferablePostFix)), null);

            harmony.Patch(AccessTools.Method(typeof(RimWorld.GameEnder), "CheckGameOver"), null, new HarmonyMethod(typeof(HarmonyPatches), "CheckGameOverPostfix"));

            harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.WorldSelector), "AutoOrderToTileNow", new Type[] { typeof(Caravan), typeof(int) }), new HarmonyMethod(typeof(HarmonyPatches), "AutoOrderToTileNowPrefix"), null);

            harmony.Patch(AccessTools.Method(typeof(RimWorld.Scenario), "GenerateIntoMap", new Type[] { typeof(Map) }), new HarmonyMethod(typeof(HarmonyPatches), "GenerateIntoMapPreFix"), null);

            harmony.Patch(AccessTools.Method(typeof(RimWorld.TransferableOneWayWidget), "AddSection"), new HarmonyMethod(typeof(HarmonyPatches), "AddSectionPrefix"), null);

            harmony.Patch(AccessTools.Method(typeof(CaravanInventoryUtility), "AllInventoryItems"), new HarmonyMethod(typeof(HarmonyPatches), "AllInventoryItemsPrefix"), null);

            //harmony.Patch(AccessTools.Method(typeof(RimWorld.Transferable), "AdjustTo"),new HarmonyMethod(typeof(HarmonyPatches), "AdjustToPrefix"), null);

        }

        public static void AnyColonistTameAnimalOrPrisonerOfColonyPostFix(ref bool __result, MapPawns __instance)
        {
            if (!__result)
            {
                Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
                if (map != null)
                {
                    List<Thing> list = map.listerThings.AllThings.FindAll(x => x is ShipBase_Traveling || x is ShipBase);
                    if (list.Count > 0)
                    {
                        __result = true;
                    }
                }
            }
        }

        public static bool AllInventoryItemsPrefix(ref Caravan caravan, ref List<Thing> __result)
        {
            __result = new List<Thing>();
                List<Pawn> pawnsListForReading = caravan.PawnsListForReading;
                for (int i = 0; i < pawnsListForReading.Count; i++)
                {
                    Pawn pawn = pawnsListForReading[i];
                    for (int j = 0; j < pawn.inventory.innerContainer.Count; j++)
                    {
                        Thing item = pawn.inventory.innerContainer[j];
                        __result.Add(item);
                    }
                }
                LandedShip landedShip = caravan as LandedShip;

            Predicate<Thing> cargoValidator = delegate (Thing t)
            {
                Pawn pawn = t as Pawn;
                if (pawn != null)
                {
                    if (pawn.IsColonist || pawn.records.GetAsInt(RecordDefOf.TimeAsColonistOrColonyAnimal) > 0)
                    {
                        return false;
                    }
                }
                return true;
            };

            if (landedShip != null)
                {
                __result.AddRange(landedShip.AllLandedShipCargo.Where(x => cargoValidator(x)));
                }            
            return false;
        }

        public static void MaxCountTransferablePostFix(TransferableOneWay __instance)
        {
            Map map = Find.VisibleMap;
            List<ShipBase> ships = DropShipUtility.ShipsOnMap(map);
            for (int i=0; i < ships.Count; i++)
            {
                for (int j=0; j < ships[i].GetDirectlyHeldThings().Count; j++)
                {
                    __instance.things.RemoveAll(x => ships[i].GetDirectlyHeldThings().Contains(x));
                }
            }            
        }

        public static void AddSectionPrefix(TransferableOneWayWidget __instance, string title, IEnumerable<TransferableOneWay> transferables)
        {
            List<TransferableOneWay> tmp = transferables.ToList();
            for (int i = 0; i < tmp.Count; i++)
            {
                Dialog_LoadShipCargo.RemoveExistingTransferable(tmp[i], Find.VisibleMap);
                //tmp[i].AdjustTo(tmp[i].GetMinimum());
            }
        }

        public static void CheckGameOverPostfix()
        {
            List<TravelingShips> travelingShips = Find.WorldObjects.AllWorldObjects.FindAll(x => x is TravelingShips).Cast<TravelingShips>().ToList();
            for (int i=0; i < travelingShips.Count; i++)
            {
                TravelingShips ship = travelingShips[i];
                if (ship.containsColonists)
                {
                    Find.GameEnder.gameEnding = false;
                }
            }
        }

        //public static bool AdjustToPrefix(ref Transferable __instance, int destination)
        //{
        //    if (!Find.WindowStack.Windows.Any(x => x.GetType() == typeof(Dialog_LoadShipCargo)))
        //    {
        //        if (!__instance.CanAdjustTo(destination).Accepted)
        //        {
        //            Log.Error("Failed to adjust transferable counts");
        //            return false;
        //        }
        //    }

        //    int countTotransferInt = Traverse.Create(__instance).Field("countToTransfer").GetValue<int>();
        //    Log.Message(countTotransferInt.ToString());

        //    Log.Message(destination.ToString() + " vs. " + countTotransferInt.ToString() + " vs " + __instance.CountToTransfer.ToString());
        //    Log.Message(__instance.GetMaximum().ToString() + "  " + __instance.GetMinimum());
        //    countTotransferInt = __instance.ClampAmount(destination);
        //    return false;
        //}

        //public static bool CanAdjustToPostFix(ref TransferableOneWay instance, ref AcceptanceReport _result, int destination)
        //{
        //    Log.Message(destination.ToString() + " vs. " + instance.CountToTransfer.ToString());
              
        //    if (destination == instance.CountToTransfer)
        //    {
        //        return true;
        //    }
        //    int num = instance.ClampAmount(destination);
        //    if (num != instance.CountToTransfer)
        //    {
        //        return true;
        //    }
        //    if (destination < instance.CountToTransfer)
        //    {
        //        Log.Message("Underflow");
        //        return instance.UnderflowReport().Accepted;
        //    }

        //    Log.Message("Overrflow");
        //    return instance.OverflowReport().Accepted;
        //}

        public static void GenerateFactionsIntoWorldPostFix()
        {
            Log.Message("GeneratingShipTracker");
            ShipTracker shipTracker = (ShipTracker)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.ShipTracker);
            int tile = 0;
            while (!(Find.WorldObjects.AnyWorldObjectAt(tile) || Find.WorldGrid[tile].biome == BiomeDefOf.Ocean))
            {
                tile = Rand.Range(0, Find.WorldGrid.TilesCount);
            }
            shipTracker.Tile = tile;
            Find.WorldObjects.Add(shipTracker);
        }

        public static bool AutoOrderToTileNowPrefix(Caravan c, int tile)
        {
            LandedShip ship = c as LandedShip;
            if (ship != null)
            {
                return false;
            }
            return true;
        }

        public static void GenerateIntoMapPreFix(Map map)
        {
            if (Find.GameInitData == null)
            {
                return;
            }
            else
            {
                ScenPart_StartWithShip scenPart = Find.Scenario.AllParts.FirstOrDefault(x => x is ScenPart_StartWithShip) as ScenPart_StartWithShip;
                if (scenPart != null)
                {
                    List<List<Thing>> list = new List<List<Thing>>();
                    foreach (Pawn current in Find.GameInitData.startingPawns)
                    {
                        list.Add(new List<Thing>
                {
                    current
                });
                    }
                    List<Thing> list2 = new List<Thing>();
                    foreach (ScenPart current2 in Find.Scenario.AllParts)
                    {
                        list2.AddRange(current2.PlayerStartingThings());
                    }
                    int num = 0;
                    foreach (Thing current3 in list2)
                    {
                        if (current3.def.CanHaveFaction)
                        {
                            current3.SetFactionDirect(Faction.OfPlayer);
                        }
                        list[num].Add(current3);
                        num++;
                        if (num >= list.Count)
                        {
                            num = 0;
                        }
                    }
                    foreach (List<Thing> current in list)
                    {
                        scenPart.AddToStartingCargo(current);
                    }
                    ScenPart_PlayerPawnsArriveMethod arrivalPart = Find.Scenario.AllParts.FirstOrDefault(x => x is ScenPart_PlayerPawnsArriveMethod) as ScenPart_PlayerPawnsArriveMethod;
                    if (arrivalPart != null)
                    {
                        Find.Scenario.RemovePart(arrivalPart);
                    }
                }
            }
        }
        
        
    }
}
