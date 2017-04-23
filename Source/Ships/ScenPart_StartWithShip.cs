using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OHUShips
{
    public class ScenPart_StartWithShip : ScenPart
    {
        public List<ShipBase> StartingShips = new List<ShipBase>();

        public ThingDef ShipDef;

        private List<Thing> startingCargo = new List<Thing>();

        public void AddToStartingCargo(Thing newCargo)
        {
            this.startingCargo.Add(newCargo);
        }

        public void AddToStartingCargo(IEnumerable<Thing> newCargo)
        {
            this.startingCargo.AddRange(newCargo);
        }
        public override IEnumerable<Thing> PlayerStartingThings()
        {
            return this.startingCargo;
        }

        public override void Randomize()
        {
            this.ShipDef = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => this.shipValidator(x)).RandomElement();
        }

        public override void GenerateIntoMap(Map map)
        {
            ShipBase newShip = (ShipBase)ThingMaker.MakeThing(this.ShipDef);
            newShip.SetFaction(Faction.OfPlayer);
            Thing initialFuel = ThingMaker.MakeThing(ShipNamespaceDefOfs.Chemfuel);
            initialFuel.stackCount = 500;
            newShip.refuelableComp.Refuel(initialFuel);
            this.StartingShips.Add(newShip);
            DropShipUtility.LoadNewCargoIntoRandomShips(this.PlayerStartingThings().ToList(), this.StartingShips);
            DropShipUtility.DropShipGroups(map.Center, map, this.StartingShips, TravelingShipArrivalAction.EnterMapFriendly);
        }

        private Predicate<ThingDef> shipValidator = delegate (ThingDef t)
        {
            if (t.thingClass == typeof(ShipBase))
            {
                CompProperties_Ship compProps = t.GetCompProperties<CompProperties_Ship>();
                if (compProps != null)
                {
                    if (compProps.CanBeStartingShip)
                    {
                        return true;
                    }
                }
            }
            return false;
        };

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight);
            if (Widgets.ButtonText(scenPartRect, this.ShipDef.label, true, false, true))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();

                

                List<ThingDef> listThings = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => this.shipValidator(x));
                for (int i=0; i < listThings.Count; i++)
                {
                    list.Add(new FloatMenuOption(listThings[i].label, delegate
                    {
                       this.ShipDef = listThings[i];
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }                    
                
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.LookDef<ThingDef>(ref this.ShipDef, "ShipDef");

        }


    }
}
