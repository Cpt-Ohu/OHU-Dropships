using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using System.Reflection;
using UnityEngine;
using RimWorld.Planet;

namespace OHUShips
{
    public class Dialog_TradeFromShips : Dialog_Trade
    {
        public LandedShip landedShip;

        public Dialog_TradeFromShips(LandedShip landedShip, Pawn playerNegotiator, ITrader trader) : base(playerNegotiator, trader)
        {
            this.landedShip = landedShip;
        }

        public override void PostOpen()
        {
            base.PostOpen();
        }

        public override void DoWindowContents(Rect inRect)
        {
            this.RecacheTradeablblesAndMassCapacity();
            base.DoWindowContents(inRect);
        }

        private void LoadShipTradeables()
        {

        }

        public override void PostClose()
        {
            this.ResolveTradedItems();
            base.PostClose();
        }

        private bool EnvironmentAllowsEatingVirtualPlantsNow
        {
            get
            {
                return VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(this.landedShip.Tile);
            }
        }

        private void RecacheTradeablblesAndMassCapacity()
        {
            List<Thing> tradeables = new List<Thing>();

            FieldInfo capacity = typeof(Dialog_TradeFromShips).BaseType.GetField("cachedMassCapacity", BindingFlags.NonPublic | BindingFlags.Instance);
            
            
            float num = 0;
            if (landedShip != null)
            {
                List<ShipBase> ships = landedShip.ships;
                for (int i = 0; i < ships.Count; i++)
                {
                    num += ships[i].compShip.sProps.maxCargo;
                }
            }
            else
            {
                throw new Exception("Tried to trade from landed ship, but ship is null");
            }
            capacity.SetValue(this, num);
        }

        private void ResolveTradedItems()
        {
            List<Thing> itemList = CaravanInventoryUtility.AllInventoryItems(this.landedShip);
            List<Thing> tmpToRemove = new List<Thing>();
            if (itemList != null)
            {
                for (int i = 0; i < landedShip.ships.Count; i++)
                {
                    ThingOwner container = landedShip.ships[i].GetDirectlyHeldThings();
                    tmpToRemove.Clear();
                    for (int k = 0; k < container.Count; k++)
                    {
                        if (!itemList.Contains(container[k]))
                        {
                            Pawn pawn = container[k] as Pawn;
                            if (pawn != null)
                            {
                                if (!pawn.IsColonist)
                                {
                                    tmpToRemove.Add(container[k]);
                                }
                            }
                            else
                            {
                                tmpToRemove.Add(container[k]);
                            }
                        }
                    }
                    container.RemoveAll(x => tmpToRemove.Contains(x));
                }
            }
            this.LoadNewCargo();
        }

        private void LoadNewCargo()
        {
            List<Pawn> pawns = this.landedShip.PawnsListForReading;
            for (int i=0; i < pawns.Count; i++)
            {
                ThingOwner<Thing> innerContainer = pawns[i].inventory.innerContainer;
                innerContainer.TryTransferAllToContainer(this.landedShip.ships.RandomElement().GetDirectlyHeldThings());
                //for (int j = 0; j < inventory.Count; j++)
                //{
                //    Thing thing = inventory[j];
                //    thingsToRemove.Add(thing);
                //}
            }
        }
    }
}
