using System;
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

        public PawnsArrivalModeDef arriveMode;

        public ShipArrivalAction arrivalAction;

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
                    this.cachedMat = MaterialPool.MatFrom(ships[0].def.graphicData.texPath, ShaderDatabase.WorldOverlayCutout, ships[0].DrawColor, WorldMaterials.WorldObjectRenderQueue);
                }
                return cachedMat;
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

        private bool isSingularShip
        {
            get
            {
                if (this.ships.Count == 1)
                {
                    return true;
                }
                return false;
            }
        }

        private float maxTravelingSpeed = -1;

        public float MaxTravelingSpeed
        {
            get
            {
                if (this.maxTravelingSpeed == -1)
                {
                    List<float> speedFactors = new List<float>();
                    foreach (ShipBase ship in this.ships)
                    {
                        speedFactors.Add(ship.compShip.sProps.WorldMapTravelSpeedFactor);
                    }
                    float chosenFactor = Mathf.Min(speedFactors.ToArray());
                    maxTravelingSpeed = chosenFactor * 0.0000416f;
                }
                
                return maxTravelingSpeed;
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
                return MaxTravelingSpeed / num;
            }
        }

        public bool PodsHaveAnyFreeColonist
        {
            get
            {
                for (int i = 0; i < this.ships.Count; i++)
                {
                    ThingOwner innerContainer = this.ships[i].GetDirectlyHeldThings();
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
                    ThingOwner things = this.ships[i].GetDirectlyHeldThings();
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
            Scribe_Collections.Look<ShipBase>(ref this.ships, "ships", LookMode.Deep, new object[0]);
            Scribe_Values.Look<int>(ref this.destinationTile, "destinationTile", 0, false);
            Scribe_Values.Look<IntVec3>(ref this.destinationCell, "destinationCell", default(IntVec3), false);
            Scribe_Values.Look<PawnsArrivalModeDef>(ref this.arriveMode, "arriveMode", PawnsArrivalModeDefOf.EdgeWalkIn, false);
            Scribe_Values.Look<bool>(ref this.arrived, "arrived", false, false);
            Scribe_Values.Look<int>(ref this.initialTile, "initialTile", 0, false);
            Scribe_Values.Look<float>(ref this.traveledPct, "traveledPct", 0f, false);
            Scribe_Values.Look<ShipArrivalAction>(ref this.arrivalAction, "arrivalAction", ShipArrivalAction.StayOnWorldMap, false);            
        }

        public override void PostAdd()
        {
            base.PostAdd();
            this.initialTile = base.Tile;
        }

        public override void Tick()
        {
            base.Tick();
            this.BurnFuel();
            if (this.ships.Count < 1)
            {
                this.Destroy();
            }
            this.traveledPct += this.TraveledPctStepPerTick;
            if (this.traveledPct >= 1f)
            {                
                this.traveledPct = 1f;
                this.Arrived();
            }
        }

        private void Destroy()
        {
            this.RemoveAllPawnsFromWorldPawns();
            this.RemoveAllShip();
            Find.WorldObjects.Remove(this);
        }

        private void BurnFuel()
        {
            foreach (ShipBase ship in this.ships)
            {
                ship.refuelableComp.ConsumeFuel(ship.refuelableComp.Props.fuelConsumptionRate / 60f);
                if (!ship.refuelableComp.HasFuel && !ship.Destroyed)
                {
                    Messages.Message("ShipOutOfFuelCrash".Translate(new object[] { ship.ShipNick }), MessageTypeDefOf.ThreatBig);
                    ship.Destroy();
                    DropShipUtility.CurrentShipTracker.AllPlanetShips.Remove(ship);
                }
            }

            this.ships.RemoveAll(x => x.Destroyed);
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
                if (this.ships[i].GetDirectlyHeldThings().Contains(p))
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
            if (this.arrivalAction == ShipArrivalAction.BombingRun)
            {
                MapParent parent = Find.World.worldObjects.MapParentAt(this.destinationTile);
                if (parent != null)
                {
                    Messages.Message("MessageBombedSettlement".Translate(new object[] { parent.ToString(), parent.Faction.Name }), parent, MessageTypeDefOf.NeutralEvent);
                    Find.World.worldObjects.Remove(parent);
                }
                this.SwitchOriginToDest();

                //TravelingShips travelingShips = (TravelingShips)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.TravelingSuborbitalShip);
                //travelingShips.ships.AddRange(this.ships);
                //travelingShips.Tile = this.destinationTile;
                //travelingShips.SetFaction(Faction.OfPlayer);
                //travelingShips.destinationTile = this.initialTile;
                //travelingShips.destinationCell = this.launchCell;
                //travelingShips.arriveMode = this.arriveMode;
                //travelingShips.arrivalAction = TravelingShipArrivalAction.EnterMapFriendly;
                //Find.WorldObjects.Add(travelingShips);
                //Find.WorldObjects.Remove(this);
            }
            else if (arrivalAction == ShipArrivalAction.Despawn)
            {
                this.Destroy();
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
                        this.ships[i].GetDirectlyHeldThings().ClearAndDestroyContentsOrPassToWorld(DestroyMode.Vanish);
                    }
                    this.RemoveAllShip();
                    Find.WorldObjects.Remove(this);
                    Messages.Message("MessageTransportPodsArrivedAndLost".Translate(), new GlobalTargetInfo(this.destinationTile), MessageTypeDefOf.NegativeEvent);
                }
                else
                {
                    Settlement Settlement = Find.WorldObjects.Settlements.Find((Settlement x) => x.Tile == this.destinationTile);
                    
                    if (Settlement != null && Settlement.Faction != Faction.OfPlayer && this.arrivalAction != ShipArrivalAction.StayOnWorldMap)
                    {
                        LongEventHandler.QueueLongEvent(delegate
                        {
                            Map map2 = GetOrGenerateMapUtility.GetOrGenerateMap(Settlement.Tile, Find.World.info.initialMapSize, null); ;
                            
                            string extraMessagePart = null;
                            if (this.arrivalAction == ShipArrivalAction.EnterMapAssault && !Settlement.Faction.HostileTo(Faction.OfPlayer))
                            {
                                Settlement.Faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Hostile);
                                extraMessagePart = "MessageTransportPodsArrived_BecameHostile".Translate(new object[]
                                {
                                Settlement.Faction.Name
                                }).CapitalizeFirst();
                            }
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                            Current.Game.CurrentMap = map2;
                            Find.CameraDriver.JumpToCurrentMapLoc(map2.Center);
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
                ThingOwner innerContainer = this.ships[i].GetDirectlyHeldThings();
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
            this.RemoveAllShip();
            Find.WorldObjects.Remove(this);
            
            Messages.Message("MessageShipsArrived".Translate(), landedShip, MessageTypeDefOf.NeutralEvent);
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
            else if (this.arriveMode == PawnsArrivalModeDefOf.CenterDrop)
            {
                intVec = DropCellFinder.FindRaidDropCenterDistant(map);
            }
            else
            {
                if (this.arriveMode != PawnsArrivalModeDefOf.EdgeDrop)
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
            DropShipUtility.DropShipGroups(intVec, map, this.ships, this.arrivalAction, this.isSingularShip);
            Messages.Message(text, new TargetInfo(intVec, map, false), MessageTypeDefOf.NeutralEvent);
            this.RemoveAllShip();
            Find.WorldObjects.Remove(this);
        }

        private bool LandedShipHasCaravanOwner
        {
            get
            {
                for (int i = 0; i < this.ships.Count; i++)
                {
                    ThingOwner innerContainer = this.ships[i].GetDirectlyHeldThings();
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
                ThingOwner innerContainer = this.ships[i].GetDirectlyHeldThings();
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

        private void RemoveAllShip()
        {
            this.ships.ForEach(x => x?.Destroy());
            this.ships.Clear();
        }

        public void SwitchOriginToDest()
        {
            this.traveledPct = 0f;
            this.arrived = false;
            this.arrivalAction = ShipArrivalAction.EnterMapFriendly;

            int bufferTile = this.destinationTile;

            this.destinationCell = this.launchCell;
            this.destinationTile = this.initialTile;

            this.initialTile = bufferTile;
            this.launchCell = IntVec3.Zero;
        }
    }
}
