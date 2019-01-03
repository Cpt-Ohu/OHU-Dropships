using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OHUShips
{

    [StaticConstructorOnStartup]
    public class WITab_Worldship_Cargo : WITab
    {

        private const float TopPadding = 20f;

        private const float ThingIconSize = 28f;

        private List<Thing> cachedItems = new List<Thing>();
        
        private enum Tab
        {
            Passengers,
            Cargo
        }

        private WITab_Worldship_Cargo.Tab tab;

        private const float ThingRowHeight = 28f;

        private const float ThingLeftX = 36f;

        private const float StandardLineHeight = 22f;

        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;

        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static List<Thing> workingInvList = new List<Thing>();

        public WorldShip worldShip
        {
            get
            {
                return (WorldShip)this.SelObject;
            }
        }

        private bool CanControl
        {
            get
            {
                return this.worldShip.Faction == Faction.OfPlayer;
            }
        }

        public WITab_Worldship_Cargo()
        {
            this.size = new Vector2(600f, 500f);
            this.labelKey = "TabShipCargo";
        }

        public override void OnOpen()
        {
            base.OnOpen();
            this.cachedItems = this.AllCargo.ToList();
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, this.size.x, this.size.y);
            GUI.BeginGroup(rect);
            Rect rect2 = new Rect(rect.x, rect.y + 20f, rect.width, 30f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect2, this.worldShip.Label);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect rect3 = rect2;
            rect3.y = rect2.yMax + 100;
            rect3.height = rect.height - rect2.height;

            Widgets.DrawMenuSection(rect3);
            List<TabRecord> list = new List<TabRecord>();

            list.Add(new TabRecord("ShipPassengers".Translate(), delegate
            {
                this.tab = WITab_Worldship_Cargo.Tab.Passengers;
            }, this.tab == WITab_Worldship_Cargo.Tab.Passengers));

            list.Add(new TabRecord("ShipCargo".Translate(), delegate
            {
                this.tab = WITab_Worldship_Cargo.Tab.Cargo;
            }, this.tab == WITab_Worldship_Cargo.Tab.Cargo));
            TabDrawer.DrawTabs(rect3, list);
            rect3 = rect3.ContractedBy(9f);
            //    GUI.BeginGroup(rect3);

            GUI.color = Color.white;

            if (this.tab == Tab.Passengers)
            {
                DrawCargo(rect3, false);
            }
            else if (this.tab == Tab.Cargo)
            {
                DrawCargo(rect3, true);
            }

            //      GUI.EndGroup();
            GUI.EndGroup();
        }

        private IEnumerable<Thing> AllCargo
        {
            get
            {
                foreach (var data in this.worldShip.WorldShipData)
                {
                    foreach (Thing t in data.Ship.GetDirectlyHeldThings())
                    {
                        yield return t;
                    }
                }
            }
        }

        private void DrawCargo(Rect inRect, bool nonPawn)
        {
            Text.Font = GameFont.Small;
            Rect rect = inRect.ContractedBy(4f);
            GUI.BeginGroup(rect);
            GUI.color = Color.white;
            Rect totalRect = new Rect(0f, 0f, rect.width - 50f, 300f);
            Rect viewRect = new Rect(0f, 0f, rect.width, this.scrollViewHeight);
            Widgets.BeginScrollView(totalRect, ref this.scrollPosition, viewRect);
            float num = 0f;
            
            if (!this.worldShip.WorldShipData.NullOrEmpty())
            {
                Text.Font = GameFont.Small;
                for (int i = 0; i < this.cachedItems.Count; i++)
                {
                    Thing thing = this.cachedItems[i];
                    Pawn pawn = thing as Pawn;
                    if (nonPawn)
                    {
                        if (pawn == null || (pawn != null && !pawn.def.race.Humanlike))
                        {
                            this.DrawThingRow(ref num, viewRect.width - 100f, thing);
                        }
                    }
                    else
                    {
                        if (pawn != null && pawn.def.race.Humanlike)
                        {
                            this.DrawThingRow(ref num, viewRect.width - 100f, thing);
                        }
                    }
                }
            }
            this.scrollViewHeight = num + 30f;
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private void DrawThingRow(ref float y, float width, Thing thing)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            Widgets.InfoCardButton(rect.width - 24f, y, thing);
            rect.width -= 24f;
            if (Mouse.IsOver(rect))
            {
                GUI.color = WITab_Worldship_Cargo.HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = WITab_Worldship_Cargo.ThingLabelColor;
            Rect rect3 = new Rect(36f, y, width - 36f, 28f);
            string text = thing.LabelCap;

            Widgets.Label(rect3, text);
            y += 32f;
        }

    }
}
