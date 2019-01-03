using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace OHUShips
{
    public class TradeDeal_Worldship : TradeDeal
    {
        public TradeDeal_Worldship()
        {
            this.AddAllTradeables();
        }

        private List<Tradeable> tradeables
        {
            get
            {
                FieldInfo info = base.GetType().GetField("tradeables", BindingFlags.Instance | BindingFlags.NonPublic);
                if (info != null)
                {
                    return info.GetValue(this) as List<Tradeable>;
                }
                return null;
            }
        }

        public void AddAllTradeables()
        {
            foreach (Thing current in TradeSession.trader.ColonyThingsWillingToBuy(TradeSession.playerNegotiator))
            {
                if (TradeUtility.PlayerSellableNow(current))
                {
                    this.AddToTradeables(current, Transactor.Colony);
                }
            }

            if (!TradeSession.giftMode)
            {
                foreach (Thing current2 in TradeSession.trader.Goods)
                {
                    this.AddToTradeables(current2, Transactor.Trader);
                }
            }
            //if (!TradeSession.giftMode)
            //{
            //    if (this.tradeables.Find((Tradeable x) => x.IsCurrency) == null)
            //    {
            //        Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver, null);
            //        thing.stackCount = 0;
            //        this.AddToTradeables(thing, Transactor.Trader);
            //    }
            //}
        }

        private void AddToTradeables(Thing current, Transactor colony)
        {
            MethodInfo methodInfo = this.GetType().BaseType.GetMethod("AddToTradeables", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null)
            {
                methodInfo.Invoke(this, new object[] { current, colony });
            }
        }
    }
}
