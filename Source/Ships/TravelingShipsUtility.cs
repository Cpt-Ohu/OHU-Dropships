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
            Pawn bestNegotiator = CaravanVisitUtility.BestNegotiator(caravan);
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "CommandTrade".Translate();
            command_Action.defaultDesc = "CommandTradeDesc".Translate();
            command_Action.icon = DropShipUtility.TradeCommandTex;
            command_Action.action = delegate
            {
                Settlement factionBase = CaravanVisitUtility.SettlementVisitedNow(caravan);
                if (factionBase != null && factionBase.CanTradeNow)
                {
                    caravan.UnloadCargoForTrading();
                    //Find.WindowStack.Add(new Dialog_TradeFromShips(caravan, bestNegotiator, factionBase));
                    Find.WindowStack.Add(new Dialog_TradeFromShips(caravan, bestNegotiator, factionBase));
                    string empty = string.Empty;
                    string empty2 = string.Empty;
                    PawnRelationUtility.Notify_PawnsSeenByPlayer(factionBase.Goods.OfType<Pawn>(), ref empty, ref empty2, "LetterRelatedPawnsTradingWithFactionBase".Translate(), false);
                    if (!empty2.NullOrEmpty())
                    {
                        Find.LetterStack.ReceiveLetter(empty, empty2, LetterDefOf.Good, factionBase, null);
                    }
                }
            };
            if (bestNegotiator == null)
            {
                command_Action.Disable("CommandTradeFailNoNegotiator".Translate());
            }
            return command_Action;
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
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                TravelingShipsUtility.Settle(landedShip, settlePermanent);
            };
            bool flag = false;
            List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < allWorldObjects.Count; i++)
            {
                WorldObject worldObject = allWorldObjects[i];
                if (worldObject.Tile == landedShip.Tile && worldObject != landedShip)
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                command_Settle.Disable("CommandSettleFailOtherWorldObjectsHere".Translate());
            }
            else if (settlePermanent && SettleUtility.PlayerHomesCountLimitReached)
            {
                if (Prefs.MaxNumberOfPlayerHomes > 1)
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
            if (settlePermanent)
            {
                newWorldObject = SettleUtility.AddNewHome(landedShip.Tile, faction);
            }
            else
            {
                newWorldObject = (ShipDropSite)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.ShipDropSite);
                newWorldObject.SetFaction(faction);
                newWorldObject.Tile = landedShip.Tile;
                Find.WorldObjects.Add(newWorldObject);
            }
            LongEventHandler.QueueLongEvent(delegate
            {
                IntVec3 vec3;
                if (settlePermanent)
                {
                    vec3 = Find.World.info.initialMapSize;
                }
                else
                {
                    vec3 = new IntVec3(100, 1, 100);
                }
                Map visibleMap = MapGenerator.GenerateMap(vec3, newWorldObject, MapGeneratorDefOf.MainMap, null, null);
                Current.Game.VisibleMap = visibleMap;
            }, "GeneratingMap", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap));
            LongEventHandler.QueueLongEvent(delegate
            {
                Map map = newWorldObject.Map;
                Pawn pawn = landedShip.PawnsListForReading[0];
                Predicate<IntVec3> extraCellValidator = (IntVec3 x) => x.GetRegion(map).CellCount >= 600;
                TravelingShipsUtility.EnterMapWithShip(landedShip, map);
                Find.CameraDriver.JumpToVisibleMapLoc(map.Center);
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

        public static void Enter(LandedShip caravan, Map map, Func<ShipBase, IntVec3> spawnCellGetter)
        {
            List<ShipBase> ships = caravan.ships;
            DropShipUtility.DropShipGroups(TravelingShipsUtility.CenterCell(map), map, ships, TravelingShipArrivalAction.EnterMapFriendly);            
            caravan.RemoveAllPawns();
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
            DropShipUtility.DropShipGroups(loc, map, ships, TravelingShipArrivalAction.EnterMapFriendly);
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
                for (int i=0; i < landedShip.PawnsListForReading.Count; i++)
                {
                    if (entry.Value.Contains(landedShip.PawnsListForReading[i].ThingID))
                    {
                        landedShip.PawnsListForReading[i].holdingOwner = entry.Key.GetDirectlyHeldThings();
                    }
                }
            }
        }

        public static LandedShip MakeLandedShip(TravelingShips incomingShips, Faction faction, int startingTile, bool addToWorldPawnsIfNotAlready)
        {
        //    Log.Message("Making LandedShip");
       //     TravelingShipsUtility.MakepawnInfos(incomingShips.ships[0].GetDirectlyHeldThings());
            if (startingTile < 0 && addToWorldPawnsIfNotAlready)
            {
                Log.Warning("Tried to create a caravan but chose not to spawn a caravan but pass pawns to world. This can cause bugs because pawns can be discarded.");
            }
            TravelingShipsUtility.tmpPawns.Clear();
            TravelingShipsUtility.tmpPawns.AddRange(incomingShips.Pawns);

            LandedShip caravan = (LandedShip)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.LandedShip);
            if (startingTile >= 0)
            {
                caravan.Tile = startingTile;
            }
            caravan.SetFaction(faction);
            if (startingTile >= 0)
            {
                Find.WorldObjects.Add(caravan);
            }

            foreach (ShipBase current in incomingShips.ships)
            {
               // current.shouldDeepSave = false;
                List<Thing> passengers = current.GetDirectlyHeldThings().ToList();
                List<string> passengerIDs = new List<string>();
                for (int i = 0; i < passengers.Count; i++)
                {
                    Pawn pawn = passengers[i] as Pawn;
                    if (pawn != null)
                    {
                        if (pawn.Dead)
                        {
                            Log.Warning("Tried to form a caravan with a dead pawn " + pawn);
                        }
                        else
                        {
                            passengerIDs.Add(pawn.ThingID);
                            //pawn.holdingOwner = null;
                            current.GetDirectlyHeldThings().Remove(pawn);
                            caravan.AddPawn(pawn, addToWorldPawnsIfNotAlready);
                            if (addToWorldPawnsIfNotAlready && !pawn.IsWorldPawn())
                            {
                                if (pawn.Spawned)
                                {
                                    pawn.DeSpawn();
                                }
                                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                            }
                        }
                    }
                }
                caravan.shipsPassengerList.Add(current, passengerIDs);
            }
            string name;
            if (incomingShips.ships[0].fleetID != -1 && DropShipUtility.currentShipTracker.PlayerFleetManager.ContainsKey(incomingShips.ships[0].fleetID))
            {
                name = DropShipUtility.currentShipTracker.PlayerFleetManager[incomingShips.ships[0].fleetID];
            }
            else
            {
                name = incomingShips.ships[0].ShipNick;
            }
            caravan.Name = name;

            caravan.ships.AddRange(incomingShips.ships);
            foreach (ShipBase ship in caravan.ships)
            {
                //DropShipUtility.PassWorldPawnsForLandedShip(ship);
            }
            return caravan;
        }

        public static bool TryAddToLandedFleet(TravelingShips incomingShips, int tile)
        {
            if (tile >= 0)
            {
                LandedShip landedFleet = Find.World.worldObjects.AllWorldObjects.FirstOrDefault(x => x.Tile == tile && x.def == ShipNamespaceDefOfs.LandedShip) as LandedShip;
                if (landedFleet != null)
                {
                    for (int i = 0; i < incomingShips.ships.Count; i++)
                    {
                        ShipBase ship = incomingShips.ships[i];
                        if (landedFleet.ships[0].fleetID == ship.fleetID)
                        {
                            landedFleet.ships.Add(ship);
                            incomingShips.ships.Remove(ship);
                        }
                    }
                }
            }
            return false;
        }

        public static void LaunchLandedFleet(LandedShip landedShip, int destinationTile, IntVec3 destinationCell, PawnsArriveMode pawnArriveMode, TravelingShipArrivalAction arrivalAction)
        {
            if (destinationTile < 0)
            {
                Log.Error("Tried launching landed ship, but its destination tile is " + destinationTile);
                return;
            }

            TravelingShips travelingShips = (TravelingShips)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.TravelingSuborbitalShip);
            travelingShips.Tile = landedShip.Tile;
            travelingShips.SetFaction(landedShip.Faction);
            travelingShips.destinationTile = destinationTile;
            travelingShips.destinationCell = destinationCell;
    //        travelingShips.destinationCell = this.destinationCell;
            travelingShips.arriveMode = pawnArriveMode;
            travelingShips.arrivalAction = arrivalAction;
            Find.WorldObjects.Add(travelingShips);
            foreach(ShipBase current in landedShip.ships)
            {
                travelingShips.AddShip(current, true);
            }
            TravelingShipsUtility.ReimbarkPawnsFromLandedShip(landedShip);
            travelingShips.SetFaction(landedShip.Faction);
            TravelingShipsUtility.RemoveLandedShipPawns(landedShip);
            landedShip.ReloadStockIntoShip();
            if (Find.World.worldObjects.Contains(landedShip))
            {
                Find.World.worldObjects.Remove(landedShip);
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
