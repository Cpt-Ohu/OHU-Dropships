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
        public WorldShip worldShip;

        public Dialog_TradeFromShips(WorldShip landedShip, Pawn playerNegotiator, ITrader trader) : base(playerNegotiator, trader)
        {
            this.worldShip = landedShip;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            TradeDeal_Worldship tradeDeal_Worldship = TradeSession.deal as TradeDeal_Worldship;
            if (tradeDeal_Worldship != null)
            {
                tradeDeal_Worldship.AddAllTradeables();
            }
        }

        private TransferableSorterDef sorter1;

        private TransferableSorterDef sorter2;
        public override void PostOpen()
        {
            base.PostOpen();
            this.sorter1 = TransferableSorterDefOf.Category;
            this.sorter2 = TransferableSorterDefOf.MarketValue;
            var tradeables = (from tr in TradeSession.deal.AllTradeables
                              where !tr.IsCurrency
                              orderby (!tr.TraderWillTrade) ? -1 : 0 descending
                              select tr).ThenBy((Tradeable tr) => tr, this.sorter1.Comparer).ThenBy((Tradeable tr) => tr, this.sorter2.Comparer).ThenBy(new Func<Tradeable, float>(TransferableUIUtility.DefaultListOrderPriority)).ThenBy((Tradeable tr) => tr.ThingDef.label).ThenBy(delegate (Tradeable tr)
                              {
                                  QualityCategory result;
                                  if (tr.AnyThing.TryGetQuality(out result))
                                  {
                                      return (int)result;
                                  }
                                  return -1;
                              }).ThenBy((Tradeable tr) => tr.AnyThing.HitPoints).ToList<Tradeable>();

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
                return VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(this.worldShip.Tile);
            }
        }

        private void RecacheTradeablblesAndMassCapacity()
        {
            List<Thing> tradeables = new List<Thing>();

            FieldInfo capacity = typeof(Dialog_TradeFromShips).BaseType.GetField("cachedMassCapacity", BindingFlags.NonPublic | BindingFlags.Instance);
            
            
            float num = 0;
            if (worldShip != null)
            {
                List<ShipBase> ships = worldShip.WorldShipData.Select<WorldShipData, ShipBase>(x => x.Ship).ToList();
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
            List<Thing> itemList = this.worldShip.WorldShipData.SelectMany(x => x.Cargo).ToList(); //CaravanInventoryUtility.AllInventoryItems(this.worldShip);
            List<Thing> tmpToRemove = new List<Thing>();
            if (itemList != null)
            {
                for (int i = 0; i < worldShip.WorldShipData.Count; i++)
                {
                    ThingOwner container = worldShip.WorldShipData[i].Ship.GetDirectlyHeldThings();
                    tmpToRemove.Clear();
                    for (int k = 0; k < container.Count; k++)
                    {
                        if (!itemList.Contains(container[k]))
                        {
                            Pawn pawn = container[k] as Pawn;
                            if (pawn != null)
                            {
                                if (!pawn.IsColonist && (pawn.Faction != null && pawn.Faction != worldShip.Faction) )
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
            List<Pawn> pawns = this.worldShip.WorldShipData.SelectMany(x => x.Passengers).ToList();
            for (int i=0; i < pawns.Count; i++)
            {
                ThingOwner<Thing> innerContainer = pawns[i].inventory.innerContainer;
                innerContainer.TryTransferAllToContainer(this.worldShip.WorldShipData.RandomElement().Ship.GetDirectlyHeldThings());
                //for (int j = 0; j < inventory.Count; j++)
                //{
                //    Thing thing = inventory[j];
                //    thingsToRemove.Add(thing);
                //}
            }
        }
    }
}
