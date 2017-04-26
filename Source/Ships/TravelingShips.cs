﻿using System;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;

namespace OHUShips
{
    public class TravelingShips : WorldObject
    {
        public List<ShipBase> ships = new List<ShipBase>();

        private const float TravelSpeed = 0.00025f;

        public int destinationTile = -1;

        public IntVec3 destinationCell = IntVec3.Invalid;

        public PawnsArriveMode arriveMode;

        public TravelingShipArrivalAction arrivalAction;

        private bool arrived;

        private int initialTile = -1;

        public IntVec3 launchCell = IntVec3.Invalid;

        private float traveledPct;
        
        public Material cachedMat;
        
        public override Material Material
        {
            get
            {
                if (this.cachedMat == null)
                {
                    this.cachedMat = MaterialPool.MatFrom(ships[0].def.graphicData.texPath, ShaderDatabase.WorldOverlayTransparentLit, ships[0].DrawColor);
                }
                return cachedMat;
            }
        }

        public override void Draw()
        {
            if (this.ships.Count > 0)
            {
                base.Draw();
            }
        }

        public override Texture2D ExpandingIcon
        {
            get
            {
                if (this.ships.Count > 1)
                {
                    return DropShipUtility.movingFleet;
                }
                return DropShipUtility.movingShip;
            }
        }

        private Vector3 Start
        {
            get
            {
                return Find.WorldGrid.GetTileCenter(this.initialTile);
            }
        }

        private Vector3 End
        {
            get
            {
                return Find.WorldGrid.GetTileCenter(this.destinationTile);
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return Vector3.Slerp(this.Start, this.End, this.traveledPct);
            }
        }

        private float TraveledPctStepPerTick
        {
            get
            {
                Vector3 start = this.Start;
                Vector3 end = this.End;
                if (start == end)
                {
                    return 1f;
                }
                float num = GenMath.SphericalDistance(start.normalized, end.normalized);
                if (num == 0f)
                {
                    return 1f;
                }
                return 0.00010f / num;
            }
        }

        public bool PodsHaveAnyFreeColonist
        {
            get
            {
                for (int i = 0; i < this.ships.Count; i++)
                {
                    ThingContainer innerContainer = this.ships[i].GetInnerContainer();
                    for (int j = 0; j < innerContainer.Count; j++)
                    {
                        Pawn pawn = innerContainer[j] as Pawn;
                        if (pawn != null && pawn.IsColonist && pawn.HostFaction == null)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public IEnumerable<Pawn> Pawns
        {
            get
            {
                for (int i = 0; i < this.ships.Count; i++)
                {
                    ThingContainer things = this.ships[i].GetInnerContainer();
                    for (int j = 0; j < things.Count; j++)
                    {
                        Pawn p = things[j] as Pawn;
                        if (p != null)
                        {
                            yield return p;
                        }
                    }
                }
            }
        }

        public bool containsColonists
        {
            get
            {
                List<Pawn> pawns = this.Pawns.ToList();
                for (int i=0; i < pawns.Count; i++)
                {
                    if (pawns[i].IsColonist)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.LookList<ShipBase>(ref this.ships, "ships", LookMode.Deep, new object[0]);
            Scribe_Values.LookValue<int>(ref this.destinationTile, "destinationTile", 0, false);
            Scribe_Values.LookValue<IntVec3>(ref this.destinationCell, "destinationCell", default(IntVec3), false);
            Scribe_Values.LookValue<PawnsArriveMode>(ref this.arriveMode, "arriveMode", PawnsArriveMode.Undecided, false);
            Scribe_Values.LookValue<bool>(ref this.arrived, "arrived", false, false);
            Scribe_Values.LookValue<int>(ref this.initialTile, "initialTile", 0, false);
            Scribe_Values.LookValue<float>(ref this.traveledPct, "traveledPct", 0f, false);
            Scribe_Values.LookValue<TravelingShipArrivalAction>(ref this.arrivalAction, "arrivalAction", TravelingShipArrivalAction.StayOnWorldMap, false);
            
        }

        public override void PostAdd()
        {
            base.PostAdd();
            this.initialTile = base.Tile;
        }

        public override void Tick()
        {
            base.Tick();
            this.traveledPct += this.TraveledPctStepPerTick;
            if (this.traveledPct >= 1f)
            {                
                this.traveledPct = 1f;
                this.Arrived();
            }
        }

        public void AddShip(ShipBase ship, bool justLeftTheMap)
        {
            if (!this.ships.Contains(ship))
            {
                this.ships.Add(ship);
            }
        }

        public bool ContainsPawn(Pawn p)
        {
            for (int i = 0; i < this.ships.Count; i++)
            {
                if (this.ships[i].GetInnerContainer().Contains(p))
                {
                    return true;
                }
            }
            return false;
        }

        private void Arrived()
        {
            if (this.arrived)
            {
                return;
            }

            this.arrived = true;
            if (TravelingShipsUtility.TryAddToLandedFleet(this, this.destinationTile))
            {
                return;
            }
            if (this.arrivalAction == TravelingShipArrivalAction.BombingRun)
            {
                TravelingShips travelingShips = (TravelingShips)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.TravelingSuborbitalShip);
                travelingShips.Tile = this.destinationTile;
                travelingShips.SetFaction(Faction.OfPlayer);
                travelingShips.destinationTile = this.initialTile;
                travelingShips.destinationCell = this.launchCell;
                travelingShips.arriveMode = this.arriveMode;
                travelingShips.arrivalAction = TravelingShipArrivalAction.EnterMapFriendly;
                Find.WorldObjects.Add(travelingShips);
                Find.WorldObjects.Remove(this);
            }
            else
            {
                Map map = Current.Game.FindMap(this.destinationTile);
                if (map != null)
                {
                    this.SpawnShipsInMap(map, null);
                }
                else if (!this.LandedShipHasCaravanOwner)
                {
                    for (int i = 0; i < this.ships.Count; i++)
                    {
                        this.ships[i].GetInnerContainer().ClearAndDestroyContentsOrPassToWorld(DestroyMode.Vanish);
                    }
                    this.RemoveAllPods();
                    Find.WorldObjects.Remove(this);
                    Messages.Message("MessageTransportPodsArrivedAndLost".Translate(), new GlobalTargetInfo(this.destinationTile), MessageSound.Negative);
                }
                else
                {
                    FactionBase factionBase = Find.WorldObjects.FactionBases.Find((FactionBase x) => x.Tile == this.destinationTile);
                    
                    if (factionBase != null && factionBase.Faction != Faction.OfPlayer && this.arrivalAction != TravelingShipArrivalAction.StayOnWorldMap)
                    {
                        LongEventHandler.QueueLongEvent(delegate
                        {
                            Map map2 = AttackCaravanArrivalActionUtility.GenerateFactionBaseMap(factionBase);
                            
                            string extraMessagePart = null;
                            if (this.arrivalAction == TravelingShipArrivalAction.EnterMapAssault && !factionBase.Faction.HostileTo(Faction.OfPlayer))
                            {
                                factionBase.Faction.SetHostileTo(Faction.OfPlayer, true);
                                extraMessagePart = "MessageTransportPodsArrived_BecameHostile".Translate(new object[]
                                {
                                factionBase.Faction.Name
                                }).CapitalizeFirst();
                            }
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                            Current.Game.VisibleMap = map2;
                            Find.CameraDriver.JumpTo(map2.Center);
                            this.SpawnShipsInMap(map2, extraMessagePart);
                        }, "GeneratingMapForNewEncounter", false, null);
                    }
                    else
                    {
                        this.SpawnCaravanAtDestinationTile();
                    }
                }
            }
        }

        private void SpawnCaravanAtDestinationTile()
        {
            TravelingShipsUtility.tmpPawns.Clear();
            for (int i = 0; i < this.ships.Count; i++)
            {
                ThingContainer innerContainer = this.ships[i].GetInnerContainer();
            //    Log.Message("SpawningCaravan");
            //    TravelingShipsUtility.MakepawnInfos(innerContainer);
                for (int j = 0; j < innerContainer.Count; j++)
                {
                    Pawn pawn = innerContainer[j] as Pawn;
                    if (pawn != null)
                    {
                        TravelingShipsUtility.tmpPawns.Add(pawn);
                    }
                }
            }
            int startingTile;
            if (!GenWorldClosest.TryFindClosestPassableTile(this.destinationTile, out startingTile))
            {
                startingTile = this.destinationTile;
            }
            
            LandedShip landedShip = TravelingShipsUtility.MakeLandedShip(this, this.Faction, startingTile, true);
            this.RemoveAllPods();
            Find.WorldObjects.Remove(this);
            
            Messages.Message("MessageShipsArrived".Translate(), landedShip, MessageSound.Benefit);
        }

        public bool IsPlayerControlled
        {
            get
            {
                return base.Faction == Faction.OfPlayer;
            }
        }        

        private void SpawnShipsInMap(Map map, string extraMessagePart = null)
        {
            this.RemoveAllPawnsFromWorldPawns();
            IntVec3 intVec;
            if (this.destinationCell.IsValid && this.destinationCell.InBounds(map))
            {
                intVec = this.destinationCell;
            }
            else if (this.arriveMode == PawnsArriveMode.CenterDrop)
            {
                if (!DropCellFinder.TryFindRaidDropCenterClose(out intVec, map))
                {
                    intVec = DropCellFinder.FindRaidDropCenterDistant(map);
                }
            }
            else
            {
                if (this.arriveMode != PawnsArriveMode.EdgeDrop && this.arriveMode != PawnsArriveMode.Undecided)
                {
                    Log.Warning("Unsupported arrive mode " + this.arriveMode);
                }
                Log.Message("Invalid Cell");
                intVec = DropCellFinder.FindRaidDropCenterDistant(map);
            }

            string text = "MessageShipsArrived".Translate();
            if (extraMessagePart != null)
            {
                text = text + " " + extraMessagePart;
            }
            DropShipUtility.DropShipGroups(intVec, map, this.ships, this.arrivalAction);
            Messages.Message(text, new TargetInfo(intVec, map, false), MessageSound.Benefit);
            this.RemoveAllPods();
            Find.WorldObjects.Remove(this);
        }

        private bool LandedShipHasCaravanOwner
        {
            get
            {
                for (int i = 0; i < this.ships.Count; i++)
                {
                    ThingContainer innerContainer = this.ships[i].GetInnerContainer();
                    for (int j = 0; j < innerContainer.Count; j++)
                    {
                        Pawn pawn = innerContainer[j] as Pawn;
                        if (pawn != null)
                        {
                            if (CaravanUtility.IsOwner(pawn, this.Faction))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        private void RemoveAllPawnsFromWorldPawns()
        {
            for (int i = 0; i < this.ships.Count; i++)
            {
                ThingContainer innerContainer = this.ships[i].GetInnerContainer();
                for (int j = 0; j < innerContainer.Count; j++)
                {
                    Pawn pawn = innerContainer[j] as Pawn;
                    if (pawn != null && pawn.IsWorldPawn())
                    {
                        Find.WorldPawns.RemovePawn(pawn);
                    }
                }
            }
        }

        private void RemoveAllPods()
        {
            this.ships.Clear();
        }

    }
}
