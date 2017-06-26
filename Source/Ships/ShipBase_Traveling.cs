using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace OHUShips
{
    public class ShipBase_Traveling : ThingWithComps
    {
        public PawnsArriveMode pawnArriveMode;
        public int destinationTile = -1;
        private bool alreadyLeft;
        public bool leavingForTarget;
        private bool launchAsFleet;
        private bool dropPawnsOnTochdown = true;
        private bool dropItemsOnTouchdown = false;
        private TravelingShipArrivalAction arrivalAction;

        public IntVec3 destinationCell = IntVec3.Invalid;

        public int fleetID = -1;

        public ShipBase_Traveling()
        {
            this.Rotation = Rot4.North;
            this.containingShip = new ShipBase();
        }

        public ShipBase_Traveling(ShipBase ship)
        {
            this.Rotation = Rot4.North;
            this.containingShip = ship;
            this.leavingForTarget = false;
        }

        public ShipBase_Traveling(ShipBase ship, bool launchAsFleet = false, TravelingShipArrivalAction arrivalAction = TravelingShipArrivalAction.StayOnWorldMap)
        {
            this.containingShip = ship;
            this.def = ship.compShip.sProps.LeavingShipDef;
            this.def.size = ship.def.size;
            this.def.graphicData = ship.def.graphicData;
            this.launchAsFleet = launchAsFleet;
            this.Rotation = ship.Rotation;

            this.arrivalAction = arrivalAction;
        }


        public ShipBase_Traveling(ShipBase ship, RimWorld.Planet.GlobalTargetInfo target, PawnsArriveMode arriveMode, TravelingShipArrivalAction arrivalAction = TravelingShipArrivalAction.StayOnWorldMap, bool leavingForTarget = true)
        {
            this.containingShip = ship;
            this.def = ship.compShip.sProps.LeavingShipDef;
            this.def.size = ship.def.size;
            this.def.graphicData = ship.def.graphicData;
            this.destinationTile = target.Tile;
            this.destinationCell = target.Cell;
            this.pawnArriveMode = arriveMode;
            this.leavingForTarget = leavingForTarget;
            this.Rotation = ship.Rotation;
            this.arrivalAction = arrivalAction;
        }

        public ShipBase containingShip;
        
        public override void Draw()
        {
            this.containingShip.DrawAt(DropShipUtility.DrawPosAt(this.containingShip, this.containingShip.drawTickOffset, this));
            foreach (KeyValuePair<ShipWeaponSlot, Building_ShipTurret> current in this.containingShip.installedTurrets)
            {
                if (current.Value != null)
                {
                    current.Value.Draw();
                }
            }
            DropShipUtility.DrawDropSpotShadow(this.containingShip, this.containingShip.drawTickOffset, this);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.containingShip.compShip.TryRemoveLord(map);
        }

        public override void Tick()
        {
            base.Tick();
            if (containingShip.shipState == ShipState.Incoming)
            {
                this.containingShip.drawTickOffset--;
                if (this.containingShip.drawTickOffset <= 0)
                {
                    this.ShipImpact();
                }
                this.containingShip.refuelableComp.ConsumeFuel(this.containingShip.refuelableComp.Props.fuelConsumptionRate / 60f);
            }
            
            if (this.containingShip.shipState == ShipState.Outgoing)
            {
                this.containingShip.drawTickOffset++;
                if (this.containingShip.drawTickOffset >= containingShip.compShip.sProps.TicksToDespawn)
                {
                    if (this.leavingForTarget)
                    {
                        this.GroupLeftMap();
                    }
                    else
                    {
                        List<Pawn> pawns = DropShipUtility.AllPawnsInShip(this.containingShip);
                        for (int i=0; i < pawns.Count; i++)
                        {
                            Find.WorldPawns.PassToWorld(pawns[i]);
                        }                        
                        this.Destroy();
                    }
                }
            }     
        }

        private void ShipImpact()
        {
       //     Log.Message("ShipImpact at " + this.Position.ToString() + " with truecenter" + Gen.TrueCenter(this).ToString() + " and ticks: " + this.containingShip.drawTickOffset.ToString());
            this.containingShip.shipState = ShipState.Stationary;

            for (int i = 0; i < 6; i++)
            {
                Vector3 loc = base.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f);
                MoteMaker.ThrowDustPuff(loc, base.Map, 1.2f);
            }
            MoteMaker.ThrowLightningGlow(base.Position.ToVector3Shifted(), base.Map, 2f);
            RoofDef roof = this.Position.GetRoof(this.Map);
            if (roof != null)
            {
                if (!roof.soundPunchThrough.NullOrUndefined())
                {
                    roof.soundPunchThrough.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                }
                if (roof.filthLeaving != null)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        FilthMaker.MakeFilth(base.Position, base.Map, roof.filthLeaving, 1);
                    }
                }
            }
            GenSpawn.Spawn(this.containingShip, base.Position, this.Map, this.containingShip.Rotation);

            this.containingShip.ShipUnload(false, this.dropPawnsOnTochdown, this.dropItemsOnTouchdown);
            this.DeSpawn();
        }

        private void GroupLeftMap()
        {            
            if (this.destinationTile < 0)
            {
                Log.Error("Drop pod left the map, but its destination tile is " + this.destinationTile);
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            TravelingShips travelingShips = (TravelingShips)WorldObjectMaker.MakeWorldObject(ShipNamespaceDefOfs.TravelingSuborbitalShip);
            travelingShips.Tile = base.Map.Tile;
            travelingShips.SetFaction(this.Faction);
            travelingShips.destinationTile = this.destinationTile;
            travelingShips.destinationCell = this.destinationCell;
            travelingShips.arriveMode = this.pawnArriveMode;
            travelingShips.arrivalAction = this.arrivalAction;
            Find.WorldObjects.Add(travelingShips);
            Predicate<Thing> predicate = delegate (Thing t)
            {
                if (t != this)
                {
                    if (t is ShipBase_Traveling)
                    {
                        ShipBase_Traveling ship = (ShipBase_Traveling)t;
                        if (ship.containingShip.shipState == ShipState.Outgoing)
                        {
                            return true;
                        }
                    }
                }
                return false;
            };
            List<Thing> tmpleavingShips = base.Map.listerThings.AllThings.FindAll(x => predicate(x));
            for (int i = 0; i < tmpleavingShips.Count; i++)
            {
                ShipBase_Traveling dropPodLeaving = tmpleavingShips[i] as ShipBase_Traveling;
                if (dropPodLeaving != null && dropPodLeaving.fleetID == this.fleetID)
                {
                    dropPodLeaving.alreadyLeft = true;
                    travelingShips.AddShip(dropPodLeaving.containingShip, true);
                    dropPodLeaving.Destroy(DestroyMode.Vanish);
                }
            }
            travelingShips.AddShip(this.containingShip, true);
            travelingShips.SetFaction(this.containingShip.Faction);

            foreach (ShipBase ship in travelingShips.ships)
            this.Destroy(DestroyMode.Vanish);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref this.fleetID, "fleetID", 0, false);
            Scribe_Values.Look<int>(ref this.destinationTile, "destinationTile", 0, false);
            Scribe_Values.Look<IntVec3>(ref this.destinationCell, "destinationCell", default(IntVec3), false);
            Scribe_Values.Look<TravelingShipArrivalAction>(ref this.arrivalAction, "arrivalAction", TravelingShipArrivalAction.StayOnWorldMap, false);
            Scribe_Values.Look<PawnsArriveMode>(ref this.pawnArriveMode, "pawnArriveMode", PawnsArriveMode.Undecided, false);

            Scribe_Values.Look<bool>(ref this.leavingForTarget, "leavingForTarget", true, false);
            Scribe_Values.Look<bool>(ref this.alreadyLeft, "alreadyLeft", false, false);
            Scribe_Deep.Look<ShipBase>(ref this.containingShip, "containingShip", new object[0]);
        }
    }
}
