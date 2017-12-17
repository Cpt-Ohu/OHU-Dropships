using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;
using Verse.Sound;
using System.Reflection;

namespace OHUShips
{
    public class LandedShip : Caravan
    {
        public List<ShipBase> ships = new List<ShipBase>();
        public Dictionary<ShipBase, List<string>> shipsPassengerList = new Dictionary<ShipBase, List<string>>();

        public bool isTargeting = false;

        public LandedShip()
        {
            this.ReloadStockIntoShip();
        }

        public LandedShip(List<ShipBase> incomingShips)
        {
            this.ships = incomingShips;
            this.ReloadStockIntoShip();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<ShipBase>(ref this.ships, "ships", LookMode.Deep, new object[0]);
        }

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

        public override void Tick()
        {
            base.Tick();
            if (Find.Targeter.IsTargeting)
            {
                if (this.isTargeting)
                {
                    GhostDrawer.DrawGhostThing(UI.MouseCell(), this.ships[0].Rotation, this.ships[0].def, null, new Color(0.5f, 1f, 0.6f, 0.4f), AltitudeLayer.Blueprint);
                }
            }
            else
            {
                this.isTargeting = false;
            }
        }

        public override void Draw()
        {
            base.Draw();

        }

        public override void PostRemove()
        {
            base.PostRemove();
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

        //public List<Thing> AllLandedShipCargo
        //{
        //    get
        //    {
        //        List<Thing> list = new List<Thing>();
        //        Log.Message("A");
        //        list.AddRange(this.allLandedShipCargo);
        //        Log.Message("B");
        //        List<Thing> inventory = (CaravanInventoryUtility.AllInventoryItems(this));
        //        for (int i = 0; i < inventory.Count; i++)
        //        {
        //            if (!list.Contains(inventory[i]))
        //            {
        //                list.Add(inventory[i]);
        //            }
        //        }
        //        Log.Message("D");
        //        list.AddRange(this.PawnsListForReading.FindAll(x => !x.IsColonist).Cast<Thing>());

        //        Log.Message("E");
        //        return list;
        //    }
        //}

        public IEnumerable<Thing> AllLandedShipCargo
        {
            get
            {
                for (int i = 0; i < this.ships.Count; i++)
                {
                    ThingOwner innerContainer = this.ships[i].GetDirectlyHeldThings();
                   
                    for (int j = 0; j < innerContainer.Count; j++)
                    {
                        Pawn pawn = innerContainer[j] as Pawn;
                        if (pawn != null && !pawn.IsColonist)
                        {
                                yield return innerContainer[j];
                        }
                        else
                        {
                            yield return innerContainer[j];
                        }
                    }
                }
            }
        }

        public float allLandedShipMassCapacity
        {
            get
            {
                float num = 0;
                List<ShipBase> ships = this.ships;
                for (int i = 0; i < ships.Count; i++)
                {
                    num += ships[i].compShip.sProps.maxCargo;
                }
                return num;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (this.Resting)
            {
                stringBuilder.Append("CaravanResting".Translate());
            }
            else if (this.AllOwnersDowned)
            {
                stringBuilder.Append("AllCaravanMembersDowned".Translate());
            }
            else if (this.pather.Moving)
            {
                if (this.pather.arrivalAction != null)
                {
                    stringBuilder.Append(this.pather.arrivalAction.ReportString);
                }
                else
                {
                    stringBuilder.Append("CaravanTraveling".Translate());
                }
            }
            else
            {
                Settlement factionBase = CaravanVisitUtility.SettlementVisitedNow(this);
                if (factionBase != null)
                {
                    stringBuilder.Append("CaravanVisiting".Translate(new object[]
                    {
                        factionBase.Label
                    }));
                }
                else
                {
                    stringBuilder.Append("CaravanWaiting".Translate());
                }
            }
            
            return stringBuilder.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (this.IsPlayerControlled)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "CommandLaunchShip".Translate();
                command_Action.defaultDesc = "CommandLaunchShipDesc".Translate();
                command_Action.icon = DropShipUtility.LaunchSingleCommandTex;
                command_Action.action = delegate
                {
                    SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
                    this.ships[0].StartChoosingDestination(this.ships[0], false);
                };
                yield return command_Action;

                if (Find.WorldSelector.SingleSelectedObject == this)
                {
                    yield return TravelingShipsUtility.ShipTouchdownCommand(this, true);
                    yield return TravelingShipsUtility.ShipTouchdownCommand(this, false);
                }
                Settlement factionBase = CaravanVisitUtility.SettlementVisitedNow(this);
                if (factionBase != null && factionBase.CanTradeNow)
                {
                    yield return TravelingShipsUtility.TradeCommand(this);
                }
                //if (CaravanJourneyDestinationUtility.AnyJurneyDestinationAt(base.Tile))
                //{
                //    yield return CaravanJourneyDestinationUtility.TakeOffCommand(base.Tile);
                //}

                if (!this.ships.Any(x => x.ParkingMap == null))
                {
                    Command_Action command_Action4 = new Command_Action();
                    command_Action4.defaultLabel = "CommandTravelParkingPosition".Translate();
                    command_Action4.defaultDesc = "CommandTravelParkingPositionDesc".Translate();
                    command_Action4.icon = DropShipUtility.ReturnParkingFleet;
                    command_Action4.action = delegate
                    {
                        foreach (ShipBase ship in this.ships)
                        {
                            ship.TryLaunch(new GlobalTargetInfo(ship.ParkingPosition, ship.ParkingMap), PawnsArriveMode.CenterDrop, TravelingShipArrivalAction.EnterMapFriendly, false);
                        }
                    };
                    yield return command_Action4;
                }
            }
        }

        public void UnloadCargoForTrading()
        {
            for (int i = 0; i < this.ships.Count; i++)
            {
                ThingOwner container = this.ships[i].GetDirectlyHeldThings();
                for (int k = 0; k < container.Count; k++)
                {
                    if (!this.Goods.Contains(container[k]))
                    {
                        Pawn pawn = container[k] as Pawn;
                        if (pawn != null)
                        {
                            if (!pawn.IsColonist)
                            {
                                this.GetDirectlyHeldThings().TryAdd(pawn);
                            }
                        }
                        else
                        {
                            this.GetDirectlyHeldThings().TryAdd(container[k]);
                        }
                    }
                }
            }
        }

        private List<Thing> tmpThingsToRemove = new List<Thing>();
        
        public void ReloadStockIntoShip()
        {
            List<Thing> allCargo = this.AllLandedShipCargo.ToList<Thing>();
            allCargo.AddRange(this.PawnsListForReading.Cast<Thing>().ToList());
            List<Thing> remainingCargo = new List<Thing>();
            for (int i = 0; i < this.PawnsListForReading.Count; i++)
            {
                this.tmpThingsToRemove.Clear();
                ThingOwner carrier = this.PawnsListForReading[i].inventory.GetDirectlyHeldThings();
                if (carrier != null)
                {
                    for (int k = 0; k < carrier.Count; k++)
                    {
                        if (allCargo.Contains(carrier[k]))
                        {
                            this.tmpThingsToRemove.Add(carrier[k]);
                        }
                        else
                        {
                            remainingCargo.Add(carrier[k]);
                        }
                    }
                    carrier.RemoveAll(x => this.tmpThingsToRemove.Contains(x));
                }
            }

            List<Thing> stockInShips = new List<Thing>();
            foreach(ShipBase ship in this.ships)
            {
                stockInShips.AddRange(ship.GetDirectlyHeldThings());
            }

            for (int i=0; i < allCargo.Count; i++)
            {
                if (!stockInShips.Contains(allCargo[i]))
                {
                    remainingCargo.Add(allCargo[i]);
                }
            }
            DropShipUtility.LoadNewCargoIntoRandomShips(remainingCargo, this.ships);
        }
    }       
    
}
