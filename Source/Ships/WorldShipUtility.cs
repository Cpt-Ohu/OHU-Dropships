using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Verse.Sound;

namespace OHUShips
{
    public static class WorldShipUtility
    {
        public static IEnumerable<WorldShip> AllWorldShips
        {
            get
            {
                return Find.WorldObjects.AllWorldObjects.Where(o => o is WorldShip).Cast<WorldShip>();
            }
        }

        public static void SortPawnsAndCargoIntoCaravan(WorldShip worldShip, ShipBase ship)
        {
            foreach (Thing t in ship.GetDirectlyHeldThings())
            {
                //worldShip.AddPawnOrItem(t, true);
            }
        }


        public static Command TradeCommand(WorldShip worldShip)
        {
            Pawn bestNegotiator = BestNegotiator(worldShip);
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "CommandTrade".Translate();
            command_Action.defaultDesc = "CommandTradeDesc".Translate();
            command_Action.icon = DropShipUtility.TradeCommandTex;
            command_Action.action = delegate
            {
                SettlementBase Settlement = (SettlementBase)Find.WorldObjects.SettlementBaseAt(worldShip.Tile);
                if (Settlement != null && Settlement.CanTradeNow)
                {
                    //caravan.UnloadCargoForTrading();
                    //Find.WindowStack.Add(new Dialog_TradeFromShips(caravan, bestNegotiator, Settlement));
                    Find.WindowStack.Add(new Dialog_TradeFromShips(worldShip, bestNegotiator, Settlement));
                    string empty = string.Empty;
                    string empty2 = string.Empty;
                    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(Settlement.Goods.OfType<Pawn>(), ref empty, ref empty2, "LetterRelatedPawnsTradingWithSettlement".Translate(), false);
                    if (!empty2.NullOrEmpty())
                    {
                        Find.LetterStack.ReceiveLetter(empty, empty2, LetterDefOf.PositiveEvent, Settlement, null);
                    }
                }
            };
            if (bestNegotiator == null)
            {
                command_Action.Disable("CommandTradeFailNoNegotiator".Translate());
            }
            return command_Action;
        }

        private static Pawn BestNegotiator(WorldShip worldShip)
        {
            IEnumerable<Pawn> pawns = worldShip.WorldShipData.SelectMany(d => d.Passengers);
            Pawn negotiator = null;
            int maxSocialSkill = 0;
            int currentSocialSkill;
            foreach (Pawn pawn in pawns)
            {
                currentSocialSkill = pawn.skills.GetSkill(SkillDefOf.Social).Level;
                if (currentSocialSkill > maxSocialSkill)
                {
                    maxSocialSkill = currentSocialSkill;
                    negotiator = pawn;
                }
            }
            return negotiator;
        }


        public static Command ShipTouchdownCommand(WorldShip worldShip, bool settlePermanent = false)
        {
            string comtitle = settlePermanent ? "CommandSettle".Translate() : "CommandShipTouchdown".Translate();
            string comdesc = settlePermanent ? "CommandSettleDesc".Translate() : "CommandShipTouchdownDesc".Translate();
            Command_Settle command_Settle = new Command_Settle();
            command_Settle.defaultLabel = comtitle;
            command_Settle.defaultDesc = comdesc;
            command_Settle.icon = settlePermanent ? SettleUtility.SettleCommandTex : DropShipUtility.TouchDownCommandTex;
            command_Settle.action = delegate
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                WorldShipUtility.Settle(worldShip, settlePermanent);
            };
            bool flag = false;
            List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < allWorldObjects.Count; i++)
            {
                WorldObject worldObject = allWorldObjects[i];
                if (worldObject.Tile == worldShip.Tile && worldObject != worldShip && settlePermanent)
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                command_Settle.Disable("CommandSettleFailOtherWorldObjectsHere".Translate());
            }
            else if (settlePermanent && SettleUtility.PlayerSettlementsCountLimitReached)
            {
                if (Prefs.MaxNumberOfPlayerSettlements > 1)
                {
                    command_Settle.Disable("CommandSettleFailReachedMaximumNumberOfBases".Translate());
                }
                else
                {
                    command_Settle.Disable("CommandSettleFailAlreadyHaveBase".Translate());
                }
            }
            return command_Settle;
        }


        public static void Settle(WorldShip worldShip, bool settlePermanent = false)
        {
            Faction faction = worldShip.Faction;
            PawnsArrivalModeDef arrivalMode = PawnsArrivalModeDefOf.CenterDrop;
            if (faction != Faction.OfPlayer)
            {
                Log.Error("Cannot settle with non-player faction.");
                return;
            }
            MapParent newWorldObject;
            Map mapToDropIn = null;
            bool foundMapParent = false;
            if (settlePermanent)
            {
                newWorldObject = SettleUtility.AddNewHome(worldShip.Tile, faction);
            }
            else
            {
                newWorldObject = Find.WorldObjects.MapParentAt(worldShip.Tile);
                if (newWorldObject != null)
                {
                    foundMapParent = true;
                }
                else
                {
                    newWorldObject = (ShipDropSite)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.ShipDropSite);
                    newWorldObject.SetFaction(faction);
                    newWorldObject.Tile = worldShip.Tile;
                    Find.WorldObjects.Add(newWorldObject);
                }
            }
            LongEventHandler.QueueLongEvent(delegate
            {
                IntVec3 vec3;
                if (settlePermanent)
                {
                    vec3 = Find.World.info.initialMapSize;
                    mapToDropIn = MapGenerator.GenerateMap(vec3, newWorldObject, MapGeneratorDefOf.Base_Player, null, null);
                }
                else if (newWorldObject != null && foundMapParent)
                {
                    if (newWorldObject.HasMap)
                    {
                        arrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
                        mapToDropIn = newWorldObject.Map;
                    }
                    else
                    {
                        Site site = newWorldObject as Site;
                        mapToDropIn = GetOrGenerateMapUtility.GetOrGenerateMap(worldShip.Tile, site == null ? Find.World.info.initialMapSize : SiteCoreWorker.MapSize, newWorldObject.def);
                        arrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
                    }
                }
                else
                {
                    vec3 = new IntVec3(100, 1, 100);
                    mapToDropIn = MapGenerator.GenerateMap(vec3, newWorldObject, MapGeneratorDefOf.Base_Player, null, null);
                }
                if (mapToDropIn == null)
                {
                    Log.Error("Failed to generate Map for Ship Dropdown");
                    return;
                }

                Current.Game.CurrentMap = mapToDropIn;
            }, "GeneratingMap", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap));
            LongEventHandler.QueueLongEvent(delegate
            {
                Map map = newWorldObject.Map;
                TravelingShipsUtility.EnterMapWithShip(worldShip, map, IntVec3.Zero, ShipArrivalAction.EnterMapFriendly, arrivalMode);
                Find.CameraDriver.JumpToCurrentMapLoc(map.Center);
                Find.MainTabsRoot.EscapeCurrentTab(false);
            }, "SpawningColonists", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap));
        }


        public static void EnterMapWithShip(WorldShip worldShip, Map map, IntVec3 targetCell, ShipArrivalAction arrivalAction, PawnsArrivalModeDef pawnsArrivalMode)
        {
            WorldShipUtility.Enter(worldShip, map, targetCell, arrivalAction, pawnsArrivalMode);
        }

        private static void Enter(WorldShip worldShip, Map map, IntVec3 targetCell, ShipArrivalAction arrivalAction, PawnsArrivalModeDef pawnsArrivalMode)
        {
            List<ShipBase> ships = worldShip.WorldShipData.Select(x => x.Ship).ToList();
            IntVec3 cell = GetCellForArrivalMode(worldShip.WorldShipData[0].Ship, targetCell, map, pawnsArrivalMode);
            DropShipUtility.DropShipGroups(cell, map, ships, arrivalAction, worldShip.WorldShipData.Count == 1);
            if (worldShip.Spawned)
            {
                Find.WorldObjects.Remove(worldShip);
            }
        }

        public static IntVec3 GetCellForArrivalMode(ShipBase ship, IntVec3 targetCell, Map map, PawnsArrivalModeDef pawnsArrivalMode)
        {
            if (targetCell != IntVec3.Zero)
            {
                return TryFindValidTargetCell(ship, targetCell, map);
            }
            else if (pawnsArrivalMode == PawnsArrivalModeDefOf.CenterDrop)
            {
                return CenterCell(map);
            }
            else if (pawnsArrivalMode == PawnsArrivalModeDefOf.EdgeDrop)
            {
                return EdgeCell(map);
            }

            return IntVec3.Zero;
        }

        private static IntVec3 EdgeCell(Map map)
        {
            IntVec3 cell;

            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => x.Standable(map) && CanReachCenter(map, x), map, 0f, out cell))
            {
                CellFinder.RandomEdgeCell(map);
            }

            return cell;

        }

        private static bool CanReachCenter(Map map, IntVec3 cell)
        {
            if (map.Parent.Faction != null)
            {
                return map.reachability.CanReachFactionBase(cell, map.Parent.Faction);
            }
            return true;
        }

        public static IntVec3 CenterCell(Map map)
        {
            IntVec3 result;
            TraverseParms traverseParms = TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false);
            Predicate<IntVec3> baseValidator = (IntVec3 x) => x.Standable(map) && map.reachability.CanReachMapEdge(x, traverseParms) && !(x.Roofed(map) && x.GetRoof(map).isThickRoof);
            if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(baseValidator, map, out result))
            {
                return result;
            }
            Log.Warning("Could not find any valid cell.");
            return CellFinder.RandomCell(map);
        }


        public static IntVec3 TryFindValidTargetCell(ShipBase ship, IntVec3 targetCell, Map map)
        {
            IntVec3 result;
            TraverseParms traverseParms = TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false);
            CellRect occupiedRect = GenAdj.OccupiedRect(targetCell, ship.Rotation, ship.def.Size);
            Predicate<IntVec3> baseValidator = (IntVec3 x) => x.Standable(map) && !(x.Roofed(map) && x.GetRoof(map).isThickRoof) && GenAdj.OccupiedRect(targetCell, ship.Rotation, ship.def.Size).InBounds(map);

            if (baseValidator(targetCell))
            {
                return targetCell;
            }

            if (RCellFinder.TryFindRandomCellNearWith(targetCell, baseValidator, map, out result))
            {
                return result;
            }
            Log.Warning("Could not find any valid cell.");
            return CellFinder.RandomCell(map);
        }

        public static MapGeneratorDef GetMapGeneratorDefForObject(WorldObject worldObject)
        {
            if (worldObject.Faction != Faction.OfPlayer && worldObject.GetType().IsAssignableFrom(typeof(SettlementBase)))
            {
                return MapGeneratorDefOf.Base_Faction;
            }

            if (worldObject.GetType() == typeof(Site))
            {
                if (worldObject.AllComps.Any(c => c.GetType() == typeof(EscapeShipComp)))
                {
                    return MapGeneratorDefOf.EscapeShip;
                }
                return MapGeneratorDefOf.Encounter;
            }

            return MapGeneratorDefOf.Base_Player;
        }

    }

}