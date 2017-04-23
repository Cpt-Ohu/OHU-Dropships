using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace OHUShips
{
    [StaticConstructorOnStartup]
    public class ITab_Fleet : ITab
    {
        private static Vector2 ScrollPosition = Vector2.zero;
        private static float billsScrollHeight = 600f;

        public ShipBase ship
        {
            get
            {
                return (ShipBase)SelThing;
            }
        }

        private bool CanControl
        {
            get
            {
                return this.ship.Faction == Faction.OfPlayer;
            }
        }

        private ShipTracker shipTacker
        {
            get
            {
                return DropShipUtility.currentShipTracker;
            }
        }

        public ITab_Fleet()
        {
            this.size = new Vector2(600f, 500f);
            this.labelKey = "TabFleetManagement";
        }

        public override bool IsVisible
        {
            get
            {
                return this.CanControl;
            }
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, this.size.x, this.size.y);
            GUI.BeginGroup(rect);
            Rect rect2 = new Rect(rect.x, rect.y + 20f, rect.width, 30f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect2, "FleetManagement".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            rect = rect.ContractedBy(10f);
            GUI.BeginGroup(rect);

            Rect newFleetRect = new Rect(10f, rect2.yMax + 10f, 100f, 40f);
            Rect assignFleetRect = new Rect(newFleetRect.xMax + 10f, newFleetRect.y, 100f, 40f);
            Rect assignedFleetRect = new Rect(assignFleetRect.xMax + 10f, newFleetRect.y, 300f, 40f);

            if (Widgets.ButtonText(newFleetRect, "AddFleetEntry".Translate()))
            {
                shipTacker.AddNewFleetEntry();
            }
            if (Widgets.ButtonText(assignFleetRect, "AssignFleet".Translate()))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                opts.Add(new FloatMenuOption("None", delegate
                {
                    this.ship.fleetID = -1;
                }));
                foreach (KeyValuePair<int, string> currentFleet in this.shipTacker.PlayerFleetManager)
                {
                    FloatMenuOption option = new FloatMenuOption(currentFleet.Value, delegate
                    {
                        this.ship.fleetID = currentFleet.Key;
                    });
                    opts.Add(option);
                }

                Find.WindowStack.Add(new FloatMenu(opts));
            }
            string curFleetString = "AssignedFleet".Translate() + (this.ship.fleetID != -1 && this.shipTacker.PlayerFleetManager.Count > 0 ?  this.shipTacker.PlayerFleetManager[this.ship.fleetID] : "None".Translate());
            Widgets.Label(assignedFleetRect, curFleetString);

            Rect fleetRect = new Rect(newFleetRect.x, newFleetRect.yMax + 20f, rect.width, 500f);

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, ITab_Fleet.billsScrollHeight);
            Widgets.BeginScrollView(fleetRect, ref ITab_Fleet.ScrollPosition, viewRect);
            float num = 0;
            foreach(KeyValuePair<int, string> fleetEntry in this.shipTacker.PlayerFleetManager)
            {
                this.DrawFleetEntry(ref num, viewRect.width, fleetEntry);
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.EndGroup();
        }

        public void DrawFleetEntry(ref float curY, float width, KeyValuePair<int, string> currentFleet)
        {
            Rect nameRect = new Rect(0f, curY, width, 30f);
            Rect deleteRect = new Rect(width - 65f, curY, 30f, 30f);
            Rect renameRect = new Rect(width - 35f, curY, 30f, 30f);
            Widgets.Label(nameRect, currentFleet.Value);
            if (Widgets.ButtonImage(renameRect, DropShipUtility.Rename))
            {
                Find.WindowStack.Add(new Dialog_RenameShip(currentFleet.Key));
            }

            float curX = width/2;
            List<ShipBase> fleetShips = this.shipTacker.ShipsInFleet(currentFleet.Key);
            if (!fleetShips.NullOrEmpty())
            {
                foreach (ShipBase ship in fleetShips)
                {
                    this.DrawFleetMember(ref curX, curY, ship);
                }
            }
            if (Widgets.ButtonImage(deleteRect, DropShipUtility.CancelTex))
            {
                foreach (ShipBase ship in this.shipTacker.ShipsInFleet(currentFleet.Key))
                {
                    ship.fleetID = -1;
                }
                this.shipTacker.DeleteFleetEntry(currentFleet.Key);
            }

            curY += 35f;
        }

        private void DrawFleetMember(ref float curX, float curY, ShipBase ship)
        {
            Rect buttonRect = new Rect(curX, curY, 20f, 20f);
            GUI.DrawTexture(buttonRect, ship.compShip.fleetIconTexture);
            if (Mouse.IsOver(buttonRect))
            {
                Rect rect = new Rect(buttonRect.x, buttonRect.y, 100f, 30f);
                TipSignal tip = ship.LabelCap + " : " + ship.ShipNick;
                TooltipHandler.TipRegion(rect, tip);
            }
            curX += 30f;
        }        

        internal class Dialog_RenameShip : Dialog_GiveName
        {
            int num;

            public Dialog_RenameShip(int ID)
            {
                this.num = ID;
                this.curName = DropShipUtility.currentShipTracker.PlayerFleetManager[num];
                this.nameMessageKey = "NameFleetMessage";
                this.gainedNameMessageKey = "RenamedFleetMessage";
                this.invalidNameMessageKey = "FleetNameIsInvalid";
            }

            protected override bool IsValidName(string s)
            {
                return NamePlayerFactionDialogUtility.IsValidName(s);
            }

            protected override void Named(string s)
            {
                DropShipUtility.currentShipTracker.PlayerFleetManager[num] = s;
            }
        }
    }
}

