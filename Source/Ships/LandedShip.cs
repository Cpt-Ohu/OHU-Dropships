using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using RimWorld;
using Verse.Sound;

namespace OHUShips
{
    public class LandedShip : Caravan
    {
        public List<ShipBase> ships = new List<ShipBase>();

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
            Scribe_Collections.LookList<ShipBase>(ref this.ships, "ships", LookMode.Deep, new object[0]);
        }

        private Material cachedMat;

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
        
        public override void Tick()
        {
            base.Tick();
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

        public IEnumerable<Thing> allLandedShipCargo
        {
            get
            {
                for (int i = 0; i < this.ships.Count; i++)
                {
                    ThingContainer innerContainer = this.ships[i].GetInnerContainer();
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
                FactionBase factionBase = CaravanVisitUtility.FactionBaseVisitedNow(this);
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
                FactionBase factionBase = CaravanVisitUtility.FactionBaseVisitedNow(this);
                if (factionBase != null && factionBase.CanTradeNow)
                {
                    yield return TravelingShipsUtility.TradeCommand(this);
                }
                if (CaravanJourneyDestinationUtility.AnyJurneyDestinationAt(base.Tile))
                {
                    yield return CaravanJourneyDestinationUtility.TakeOffCommand(base.Tile);
                }


            }
        }

        public void UnloadCargoForTrading()
        {
            for (int i = 0; i < this.ships.Count; i++)
            {
                ThingContainer container = this.ships[i].GetInnerContainer();
                for (int k = 0; k < container.Count; k++)
                {
                    if (!this.Goods.Contains(container[k]))
                    {
                        Pawn pawn = container[k] as Pawn;
                        if (pawn != null)
                        {
                            if (!pawn.IsColonist)
                            {
                                this.AddToStock(pawn, this.PawnsListForReading[0]);
                            }
                        }
                        else
                        {
                            this.AddToStock(container[k], this.PawnsListForReading[0]);
                        }
                    }
                }
            }
        }

        private List<Thing> tmpThingsToRemove = new List<Thing>();

        public void ReloadStockIntoShip()
        {
            List<Thing> allCargo = this.allLandedShipCargo.ToList<Thing>();
            allCargo.AddRange(this.PawnsListForReading.Cast<Thing>().ToList());
            List<Thing> remainingCargo = new List<Thing>();
            for (int i = 0; i < this.PawnsListForReading.Count; i++)
            {
                this.tmpThingsToRemove.Clear();
                ThingContainer carrier = this.PawnsListForReading[i].inventory.GetInnerContainer();
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

            List<Thing> stockInShips = new List<Thing>();
            foreach(ShipBase ship in this.ships)
            {
                stockInShips.AddRange(ship.GetInnerContainer());
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
