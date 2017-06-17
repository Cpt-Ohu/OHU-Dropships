using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace OHUShips
{
    public class CompShip : ThingComp
    {
        public List<TransferableOneWay> leftToLoad;
        
        public bool cargoLoadingActive;
        
        public ShipBase ship
        {
            get
            {
                return (ShipBase)this.parent;
            }
        }

        public CompProperties_Ship sProps
        {
            get
            {
                return this.props as CompProperties_Ship;
            }
        }

        public Graphic dropShadow
        {
            get
            {
                return  GraphicDatabase.Get<Graphic_Single>(sProps.ShadowGraphicPath, ShaderDatabase.Transparent, Vector2.one, Color.white);
            }
        }

        public Texture2D fleetIconTexture
        {
            get
            {
                return ContentFinder<Texture2D>.Get(this.sProps.FleetIconGraphicPath, true);
            }
        }

        public Thing FirstThingLeftToLoad
        {
            get
            {
                if (this.leftToLoad == null)
                {
                    return null;
                }
                TransferableOneWay transferableOneWay = this.leftToLoad.Find((TransferableOneWay x) => x.CountToTransfer != 0 && x.HasAnyThing);
                if (transferableOneWay != null)
                {
                    return transferableOneWay.AnyThing;
                }
                return null;
            }
        }

        public bool AnythingLeftToLoad
        {
            get
            {
                return this.FirstThingLeftToLoad != null;
            }
        }


        public bool CancelLoadCargo(Map map)
        {
            if (!this.cargoLoadingActive)
            {
                return false;
            }
            this.TryRemoveLord(map);
            return true;
        }

        public void AddToTheToLoadList(TransferableOneWay t, int count)
        {
            if (!t.HasAnyThing || t.CountToTransfer <= 0)
            {
                Log.Message("NoThingsToTransfer");
                return;
            }
            if (this.leftToLoad == null)
            {
                this.leftToLoad = new List<TransferableOneWay>();
            }
            if (TransferableUtility.TransferableMatching<TransferableOneWay>(t.AnyThing, this.leftToLoad) != null)
            {
                Log.Error("Transferable already exists.");
                return;
            }

            TransferableOneWay transferableOneWay = new TransferableOneWay();
            this.leftToLoad.Add(transferableOneWay);
            transferableOneWay.things.AddRange(t.things);
            transferableOneWay.AdjustTo(count);
        }


        public void TryRemoveLord(Map map)
        {
            Lord lord = LoadShipCargoUtility.FindLoadLord(ship, map);
            if (lord != null)
            {
                map.lordManager.RemoveLord(lord);
            }
        }

        public void Notify_PawnEntered(Pawn p)
        {
            p.ClearMind(true);
            this.SubtractFromToLoadList(p);
        }

        public void SubtractFromToLoadList(Thing t)
        {
            if (this.leftToLoad == null)
            {
                return;
            }
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, this.leftToLoad);
            if (transferableOneWay == null)
            {
                return;
            }
            transferableOneWay.AdjustBy(-t.stackCount);
            if (transferableOneWay.CountToTransfer <= 0)
            {
                this.leftToLoad.Remove(transferableOneWay);
            }
            if (!this.AnythingLeftToLoad)
            {
                this.cargoLoadingActive = false;
                this.TryRemoveLord(this.parent.Map);
                this.leftToLoad.Clear();
                this.leftToLoad = new List<TransferableOneWay>();
                Messages.Message("MessageFinishedLoadingShipCargo".Translate(new object[] { this.ship.ShipNick }), this.parent, MessageSound.Benefit);
            }
        }


    }
}
