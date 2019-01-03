using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;

namespace OHUShips
{
    public static class TravelingShipsUtility
    {

        public static List<Pawn> tmpPawns = new List<Pawn>();

        public static void DistributePawnsOnShips(LandedShip landedObject)
        {
            List<ShipBase> ships = landedObject.ships;
            while (landedObject.PawnsListForReading.Count > 0)
            {
                for (int i = 0; i < landedObject.PawnsListForReading.Count; i++)
                {
                    ships.RandomElement().TryAcceptThing(landedObject.PawnsListForReading[i]);
                    landedObject.RemovePawn(landedObject.PawnsListForReading[i]);
                }
            }
            while (landedObject.Goods.Count() > 0)
            {

            }
        }

        public static Command TradeCommand(LandedShip caravan)
        {
            //Pawn bestNegotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan);
            //Command_Action command_Action = new Command_Action();
            //command_Action.defaultLabel = "CommandTrade".Translate();
            //command_Action.defaultDesc = "CommandTradeDesc".Translate();
            //command_Action.icon = DropShipUtility.TradeCommandTex;
            //command_Action.action = delegate
            //{
            //    SettlementBase Settlement = CaravanVisitUtility.SettlementVisitedNow(caravan);
            //    if (Settlement != null && Settlement.CanTradeNow)
            //    {
            //        caravan.UnloadCargoForTrading();
            //        //Find.WindowStack.Add(new Dialog_TradeFromShips(caravan, bestNegotiator, Settlement));
            //        Find.WindowStack.Add(new Dialog_TradeFromShips(this, bestNegotiator, Settlement));
            //        string empty = string.Empty;
            //        string empty2 = string.Empty;
            //        PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(Settlement.Goods.OfType<Pawn>(), ref empty, ref empty2, "LetterRelatedPawnsTradingWithSettlement".Translate(), false);
            //        if (!empty2.NullOrEmpty())
            //        {
            //            Find.LetterStack.ReceiveLetter(empty, empty2, LetterDefOf.PositiveEvent, Settlement, null);
            //        }
            //    }
            //};
            //if (bestNegotiator == null)
            //{
            //    command_Action.Disable("CommandTradeFailNoNegotiator".Translate());
            //}
            //return command_Action;
            return null;
        }

        public static Command ShipTouchdownCommand(LandedShip landedShip, bool settlePermanent = false)
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
                TravelingShipsUtility.Settle(landedShip, settlePermanent);
            };
            bool flag = false;
            List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < allWorldObjects.Count; i++)
            {
                WorldObject worldObject = allWorldObjects[i];
                if (worldObject.Tile == landedShip.Tile && worldObject != landedShip && settlePermanent)
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



        public static void Settle(LandedShip landedShip, bool settlePermanent = false)
        {
            Faction faction = landedShip.Faction;
            if (faction != Faction.OfPlayer)
            {
                Log.Error("Cannot settle with non-player faction.");
                return;
            }
            MapParent newWorldObject;
            Map mapToDropIn;
            bool foundMapParent = false;
            if (settlePermanent)
            {
                newWorldObject = SettleUtility.AddNewHome(landedShip.Tile, faction);
            }
            else
            {
                newWorldObject = Find.WorldObjects.MapParentAt(landedShip.Tile);
                if (newWorldObject != null)
                {
                    foundMapParent = true;
                }
                else
                {
                    newWorldObject = (ShipDropSite)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.ShipDropSite);
                    newWorldObject.SetFaction(faction);
                    newWorldObject.Tile = landedShip.Tile;
                    Find.WorldObjects.Add(newWorldObject);
                }
            }
            LongEventHandler.QueueLongEvent(delegate
            {
                IntVec3 vec3;
                if (settlePermanent)
                {
                    vec3 = Find.World.info.initialMapSize;
                    mapToDropIn = MapGenerator.GenerateMap(vec3, newWorldObject, MapGeneratorDefOf.Base_Faction, null, null);
                }
                else if (newWorldObject != null && foundMapParent)
                {
                    Site site = newWorldObject as Site;
                    mapToDropIn = GetOrGenerateMapUtility.GetOrGenerateMap(landedShip.Tile, site != null ? Find.World.info.initialMapSize : SiteCoreWorker.MapSize , newWorldObject.def);
                }
                else
                {
                    vec3 = new IntVec3(100, 1, 100);
                    mapToDropIn = MapGenerator.GenerateMap(vec3, newWorldObject, MapGeneratorDefOf.Base_Faction, null, null);
                }
                Current.Game.CurrentMap = mapToDropIn;
            }, "GeneratingMap", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap));
            LongEventHandler.QueueLongEvent(delegate
            {
                Map map = newWorldObject.Map;
                Pawn pawn = landedShip.PawnsListForReading[0];
                Predicate<IntVec3> extraCellValidator = (IntVec3 x) => x.GetRegion(map).CellCount >= 600;
                TravelingShipsUtility.EnterMapWithShip(landedShip, map);
                Find.CameraDriver.JumpToCurrentMapLoc(map.Center);
                Find.MainTabsRoot.EscapeCurrentTab(false);
            }, "SpawningColonists", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap));
        }

        public static void EnterMapWithShip(LandedShip caravan, Map map)
        {
            TravelingShipsUtility.ReimbarkPawnsFromLandedShip(caravan);
            IntVec3 enterCell = TravelingShipsUtility.CenterCell(map);
            Func<ShipBase, IntVec3> spawnCellGetter = (ShipBase p) => CellFinder.RandomSpawnCellForPawnNear(enterCell, map);
            TravelingShipsUtility.Enter(caravan, map, spawnCellGetter);
        }

        public static void EnterMapWithShip(WorldShip worldShip, Map map, IntVec3 targetCell, ShipArrivalAction arrivalAction, PawnsArrivalModeDef pawnsArrivalMode)
        {
            TravelingShipsUtility.Enter(worldShip, map, targetCell, arrivalAction, pawnsArrivalMode);
        }

        private static void Enter(WorldShip worldShip, Map map, IntVec3 targetCell, ShipArrivalAction arrivalAction, PawnsArrivalModeDef pawnsArrivalMode)
        {
            List<ShipBase> ships = worldShip.WorldShipData.Select(x => x.Ship).ToList();
            IntVec3 cell = GetCellForArrivalMode(worldShip.WorldShipData[0].Ship, targetCell ,map, pawnsArrivalMode);
            DropShipUtility.DropShipGroups(cell, map, ships, arrivalAction, worldShip.WorldShipData.Count == 1);
            if (worldShip.Spawned)
            {
                worldShip.WorldShipData.Clear();
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
                return DistantCell(map);
            }

            return IntVec3.Zero;
        }

        private static IntVec3 DistantCell(Map map)
        {
            IntVec3 cell;

            cell = DropCellFinder.FindRaidDropCenterDistant(map);

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
            Predicate<IntVec3> baseValidator = (IntVec3 x) => x.Standable(map) && map.reachability.CanReachMapEdge(x, traverseParms) && !(x.Roofed(map) && x.GetRoof(map).isThickRoof && !x.Fogged(map));
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

        public static void Enter(LandedShip caravan, Map map, Func<ShipBase, IntVec3> spawnCellGetter)
        {
            List<ShipBase> ships = caravan.ships;
            DropShipUtility.DropShipGroups(TravelingShipsUtility.CenterCell(map), map, ships, ShipArrivalAction.EnterMapFriendly);            
            //caravan.RemoveAllPawns();
            if (caravan.Spawned)
            {
                Find.WorldObjects.Remove(caravan);
            }
        }

        public static void Enter(List<ShipBase> ships, Map map, bool centerDrop = true)
        {
            IntVec3 loc;
            if (centerDrop)
            {
                loc = TravelingShipsUtility.CenterCell(map);
            }
            else
            {
                loc = DropCellFinder.FindRaidDropCenterDistant(map);
            }
            DropShipUtility.DropShipGroups(loc, map, ships, ShipArrivalAction.EnterMapFriendly);
        }

        public static string PawnInfoString(Pawn pawn)
        {
            return (pawn.Name + " of " + pawn.Faction.ToString());
        }

        public static void MakepawnInfos(ThingOwner container)
        {
            foreach (Thing t in container)
            {
                Pawn pawn = t as Pawn;
                if (pawn != null)
                {
                    Log.Message(TravelingShipsUtility.PawnInfoString(pawn));
                }
            }
        }

        public static void InitializePayloadAndTurrets(List<ShipBase> ships, List<Building_ShipTurret> turrets, List<WeaponSystemShipBomb> bombs)
        {
            for (int i = 0; i < ships.Count; i++)
            {
                turrets.AddRange(ships[i].assignedTurrets);
                bombs.AddRange(ships[i].loadedBombs);
            }
        }

        public static void ReimbarkPawnsFromLandedShip(LandedShip landedShip)
        {
            foreach (KeyValuePair<ShipBase, List<string>> entry in landedShip.shipsPassengerList)
            {
                List<Pawn> caravanPassengers = new List<Pawn>();
                caravanPassengers.AddRange(landedShip.PawnsListForReading);

                for (int i=0; i < caravanPassengers.Count; i++)
                {
                    if (entry.Value.Contains(caravanPassengers[i].ThingID))
                    {
                        landedShip.pawns.TryTransferToContainer(caravanPassengers[i], entry.Key.GetDirectlyHeldThings(), true);
                    }
                }
            }
        }


    
        public static void RemoveLandedShipPawns(LandedShip landedShip)
        {
            for (int i = 0; i< landedShip.PawnsListForReading.Count; i++)
            {
                Pawn pawn = landedShip.PawnsListForReading[i];
                if (Find.WorldPawns.Contains(pawn))
                {
                    Find.WorldPawns.RemovePawn(pawn);
                }
            }
        }

        public static void SetupShipTrading(LandedShip landedShip)
        {
            List<Pawn> allPawns = new List<Pawn>();
            foreach (ShipBase current in landedShip.ships)
            {
                allPawns = DropShipUtility.AllPawnsInShip(current);
                for (int k = 0; k < allPawns.Count; k++)
                {
                    ThingOwner innerContainer2 = current.GetDirectlyHeldThings();
                    for (int l = 0; l < innerContainer2.Count; l++)
                    {
                        if (!(innerContainer2[l] is Pawn))
                        {
                            Pawn pawn2 = CaravanInventoryUtility.FindPawnToMoveInventoryTo(innerContainer2[l], allPawns, null, null);
                            pawn2.inventory.innerContainer.TryAdd(innerContainer2[l], true);
                        }
                    }
                }
            }
        }

    }
}
