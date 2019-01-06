using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld.Planet;
using RimWorld;
using System.Reflection;
using System.Runtime.Serialization;

namespace OHUShips
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            ////Log.Error("1");
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.ohu.ships.main");
            ////Log.Error("2");
            ////Log.Error("3");
            harmony.Patch(AccessTools.Property(typeof(MapPawns), "AnyPawnBlockingMapRemoval").GetGetMethod(false), null, new HarmonyMethod(typeof(HarmonyPatches), nameof(AnyColonistTameAnimalOrPrisonerOfColonyPostFix)), null);

            // harmony.Patch(AccessTools.Property(typeof(TransferableOneWay), "MaxCount").GetGetMethod(false), new HarmonyMethod(typeof(HarmonyPatches), nameof(MaxCountTransferablePostFix)), null);
            ////Log.Error("4");
            harmony.Patch(AccessTools.Method(typeof(RimWorld.GameEnder), "CheckOrUpdateGameOver"), null, new HarmonyMethod(typeof(HarmonyPatches), "CheckGameOverPostfix"));
            ////Log.Error("5");
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.WorldSelector), "AutoOrderToTileNow", new Type[] { typeof(Caravan), typeof(int) }), new HarmonyMethod(typeof(HarmonyPatches), "AutoOrderToTileNowPrefix"), null);
            ////Log.Error("6");
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Scenario), "GenerateIntoMap", new Type[] { typeof(Map) }), new HarmonyMethod(typeof(HarmonyPatches), "GenerateIntoMapPreFix"), null);
            ////Log.Error("7");
            harmony.Patch(AccessTools.Method(typeof(RimWorld.TransferableOneWayWidget), "AddSection"), new HarmonyMethod(typeof(HarmonyPatches), "AddSectionPrefix"), null);
            //Log.Error("8");
            harmony.Patch(AccessTools.Method(typeof(SettlementBase_TraderTracker), "ColonyThingsWillingToBuy"), new HarmonyMethod(typeof(HarmonyPatches), "AllInventoryItemsPrefix"), null);
            //Log.Error("9");
            harmony.Patch(AccessTools.Method(typeof(ThingOwner), "NotifyAddedAndMergedWith", new Type[] { typeof(Thing), typeof(int) }), new HarmonyMethod(typeof(HarmonyPatches), "NotifyAddedAndMergedWithPostfix"), null);
            //Log.Error("10");
            harmony.Patch(AccessTools.Method(typeof(ThingOwner), "NotifyAdded", new Type[] { typeof(Thing) }), new HarmonyMethod(typeof(HarmonyPatches), "NotifyAddedPostfix"), null);
            //Log.Error("11");
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Transferable), "AdjustTo"), new HarmonyMethod(typeof(HarmonyPatches), "AdjustToPrefix"), null);
            //Log.Error("12");
            harmony.Patch(AccessTools.Method(typeof(CaravanTicksPerMoveUtility), "GetTicksPerMove", new Type[] { typeof(Caravan), typeof(StringBuilder) }), new HarmonyMethod(typeof(HarmonyPatches), "GetTicksPerMovePrefix", null), null);
            //Log.Error("13");
            harmony.Patch(AccessTools.Method(typeof(WorldSelector), "HandleWorldClicks"), null, new HarmonyMethod(typeof(HarmonyPatches), "HandleWorldClicksPostfix", null), null);


            harmony.Patch(AccessTools.Method(typeof(TradeSession), "SetupWith"), new HarmonyMethod(typeof(HarmonyPatches), "SetupWithPrefix", null), null);
            
            harmony.Patch(AccessTools.Method(typeof(SettlementBase_TraderTracker), "GiveSoldThingToPlayer"), new HarmonyMethod(typeof(HarmonyPatches), "GiveSoldThingToPlayerPrefix", null), null);

            harmony.Patch(AccessTools.Method(typeof(Settlement_TraderTracker), "GiveSoldThingToTrader"), new HarmonyMethod(typeof(HarmonyPatches), "GiveSoldThingToTraderPrefix", null), null);

            harmony.Patch(AccessTools.Method(typeof(SettlementDefeatUtility), "IsDefeated"), null, new HarmonyMethod(typeof(HarmonyPatches), "IsDefeatedPostfix", null), null);

            

        }

        public static void AnyColonistTameAnimalOrPrisonerOfColonyPostFix(ref bool __result, MapPawns __instance)
        {
            //Log.Error("1");
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

        public static bool AllInventoryItemsPrefix(ref SettlementBase_TraderTracker __instance, ref IEnumerable<Thing> __result, Pawn playerNegotiator)
        {

            WorldShip worldShip = playerNegotiator.GetWorldShip();
            if (worldShip != null)
            {
                Predicate<Thing> cargoValidator = delegate (Thing t)
                {
                    Pawn pawn = t as Pawn;
                    if (pawn != null)
                    {
                        if (pawn.IsColonist || (pawn.records.GetAsInt(RecordDefOf.TimeAsColonistOrColonyAnimal) > 0 && pawn.records.GetAsInt(RecordDefOf.TimeAsPrisoner) == 0))
                        {
                            return false;
                        }
                    }
                    return true;
                };
                List<Thing> newResult = new List<Thing>();
                foreach (var data in worldShip.WorldShipData)
                {
                    foreach (Thing t in data.Ship.GetDirectlyHeldThings().Where(x => cargoValidator(x)))
                    {
                        newResult.Add(t);
                    }
                }
                __result = newResult;
                return false;
            }
            return true;

        }

        public static bool SetupWithPrefix(ITrader newTrader, Pawn newPlayerNegotiator, bool giftMode)
        {
            if (newPlayerNegotiator.GetWorldShip() != null)
            {
                if (!newTrader.CanTradeNow)
                {
                    Log.Warning("Called SetupWith with a trader not willing to trade now.", false);
                }
                TradeSession.trader = newTrader;
                TradeSession.playerNegotiator = newPlayerNegotiator;
                TradeSession.giftMode = giftMode;
                TradeSession.deal = CreateWorldshipTradeDeal();
                if (!giftMode && TradeSession.deal.cannotSellReasons.Count > 0)
                {
                    Messages.Message("MessageCannotSellItemsReason".Translate() + TradeSession.deal.cannotSellReasons.ToCommaList(true), MessageTypeDefOf.NegativeEvent, false);
                }
                return false;
            }
            return true;
        }

        private static TradeDeal_Worldship CreateWorldshipTradeDeal()
        {
            TradeDeal_Worldship tradeDeal = FormatterServices.GetUninitializedObject(typeof(TradeDeal_Worldship)) as TradeDeal_Worldship;
            FieldInfo fieldInfo = typeof(TradeDeal).GetField("tradeables", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(tradeDeal, new List<Tradeable>());
            tradeDeal.cannotSellReasons = new List<string>();
            return tradeDeal;
        }
        
        public static void MaxCountTransferablePostFix(TransferableOneWay __instance)
        {
            //Log.Error("3");
            Map map = Find.CurrentMap;
            List<ShipBase> ships = DropShipUtility.ShipsOnMap(map);
            for (int i = 0; i < ships.Count; i++)
            {
                for (int j = 0; j < ships[i].GetDirectlyHeldThings().Count; j++)
                {
                    __instance.things.RemoveAll(x => ships[i].GetDirectlyHeldThings().Contains(x));
                }
            }
        }

        public static void AddSectionPrefix(TransferableOneWayWidget __instance, string title, IEnumerable<TransferableOneWay> transferables)
        {
            //Log.Error("4");
            List<TransferableOneWay> tmp = transferables.ToList();
            for (int i = 0; i < tmp.Count; i++)
            {
                Dialog_LoadShip.RemoveExistingTransferable(tmp[i], Find.CurrentMap);
                //tmp[i].AdjustTo(tmp[i].GetMinimum());
            }
        }

        public static void CheckGameOverPostfix()
        {
            //Log.Error("5");
            List<WorldShip> travelingShips = Find.WorldObjects.AllWorldObjects.FindAll(x => x is WorldShip).Cast<WorldShip>().ToList();
            for (int i = 0; i < travelingShips.Count; i++)
            {
                WorldShip ship = travelingShips[i];
                if (ship.WorldShipData.Any(x => x.Passengers.Any(y => y.IsColonist)))
                {
                    Find.GameEnder.gameEnding = false;
                }
            }
        }

        public static bool AutoOrderToTileNowPrefix(Caravan c, int tile)
        {
            //Log.Error("7");
            LandedShip ship = c as LandedShip;
            if (ship != null)
            {
                return false;
            }
            return true;
        }

        public static void GenerateIntoMapPreFix(Map map)
        {
            //Log.Error("8");
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
                    foreach (Pawn current in Find.GameInitData.startingAndOptionalPawns)
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

        public static void NotifyAddedAndMergedWithPostfix(ref ThingOwner __instance, Thing item, int mergedCount)
        {
            //Log.Error("9");
            ShipBase ship = __instance.Owner as ShipBase;
            if (ship != null)
            {
                ship.compShip.NotifyItemAdded(item, mergedCount);
            }
        }

        public static void NotifyAddedPostfix(ref ThingOwner __instance, Thing item)
        {
            //Log.Error("10");
            ShipBase ship = __instance.Owner as ShipBase;
            if (ship != null)
            {
                ship.compShip.NotifyItemAdded(item, item.stackCount);
            }
        }

        static bool GetTicksPerMovePrefix(Caravan caravan, ref int __result, StringBuilder explanation = null)
        {
            if (caravan != null && caravan is LandedShip ship)
            {
                //__result = (int)(1 / ship.);
                explanation = new StringBuilder();

                explanation.Append("CaravanMovementSpeedFull".Translate() + ":");
                float num = __result;
                explanation.AppendLine();
                explanation.Append(string.Concat(new string[]
                {
        "  ",
        "Default".Translate(),
        ": ",
        num.ToString("0.#"),
        " ",
        "TilesPerDay".Translate()
                }));
                return false;
            }

            return true;
        }

        public static void HandleWorldClicksPostfix()
        {
            //object selectedObject = Find.WorldSelector.SingleSelectedObject;
            //if (selectedObject != null)
            //{
            //    WorldShip worldShip = selectedObject as WorldShip;
            //    if (worldShip != null)
            //    {
            //        worldShip.pather.SetDestination(GenWorld.MouseTile(false));
            //    }
            //}
        }

        public static bool GiveSoldThingToPlayerPrefix(ref SettlementBase_TraderTracker __instance, Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            WorldShip worldShip = playerNegotiator.GetWorldShip();
            if (worldShip != null)
            {
                Thing thing = toGive.SplitOff(countToGive);
                thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, __instance.settlement);
                worldShip.trader.GiveSoldThingToPlayer(toGive, countToGive, playerNegotiator);

                return false;
            }
            return true;
        }
        public static bool GiveSoldThingToTraderPrefix(ref Settlement_TraderTracker __instance, Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            WorldShip worldShip = playerNegotiator.GetWorldShip();
            if (worldShip != null)
            {
                Thing thing = toGive.SplitOff(countToGive);
                var init = __instance.StockListForReading.Count;
                thing.PreTraded(TradeAction.PlayerSells, playerNegotiator, __instance.settlement);
                if (!__instance.GetDirectlyHeldThings().TryAdd(thing, false))
                {
                    thing.Destroy(DestroyMode.Vanish);
                }

                return false;
            }

            return true;
        }

        public static void IsDefeatedPostfix(ref bool __result, Map map, Faction faction)
        {
            if (!faction.HostileTo(Faction.OfPlayer))
            {
                __result = false;
            }
        }

    }
}
