using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OHUShips
{
    [StaticConstructorOnStartup]
    public class ITab_ShipCargo : ITab
    {

        private const float TopPadding = 20f;
        
        private const float ThingIconSize = 28f;

        private enum Tab
        {
            Passengers,
            Cargo,
            Weapons
        }

        private ITab_ShipCargo.Tab tab;

        private const float ThingRowHeight = 28f;

        private const float ThingLeftX = 36f;

        private const float StandardLineHeight = 22f;

        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;
                
        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static List<Thing> workingInvList = new List<Thing>();

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

        public ITab_ShipCargo()
        {
            this.size = new Vector2(600f, 500f);
            this.labelKey = "TabShipCargo";
        }
        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, this.size.x, this.size.y);
            GUI.BeginGroup(rect);
            Rect rect2 = new Rect(rect.x, rect.y + 20f, rect.width, 30f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect2, ship.ShipNick);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect rect3 = rect2;
            rect3.y = rect2.yMax + 100;
            rect3.height = rect.height - rect2.height ;

            Widgets.DrawMenuSection(rect3, true);
            List<TabRecord> list = new List<TabRecord>();

            list.Add(new TabRecord("ShipPassengers".Translate(), delegate
            {
                this.tab = ITab_ShipCargo.Tab.Passengers;
            }, this.tab == ITab_ShipCargo.Tab.Passengers));
            
            list.Add(new TabRecord("ShipCargo".Translate(), delegate
            {
                this.tab = ITab_ShipCargo.Tab.Cargo;
            }, this.tab == ITab_ShipCargo.Tab.Cargo));

            list.Add(new TabRecord("ShipWeapons".Translate(), delegate
            {
                this.tab = ITab_ShipCargo.Tab.Weapons;
            }, this.tab == ITab_ShipCargo.Tab.Weapons));
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
            else if (this.tab == Tab.Weapons)
            {
                this.DrawWeaponSlots(rect3);
            }
          
      //      GUI.EndGroup();
            GUI.EndGroup();
        }

        private void DrawCargo(Rect inRect, bool nonPawn)
        {
            Text.Font = GameFont.Small;
            Rect rect = inRect.ContractedBy(4f);
            GUI.BeginGroup(rect);
            GUI.color = Color.white;
            Rect totalRect = new Rect(0f, 0f, rect.width-50f, 400f);
            Rect viewRect = new Rect(0f, 0f, rect.width, this.scrollViewHeight);
            Widgets.BeginScrollView(viewRect, ref this.scrollPosition, totalRect);
            float num = 0f;
            if (this.ship.GetInnerContainer() != null)
            {
                Text.Font = GameFont.Small;
                for (int i = 0; i < this.ship.GetInnerContainer().Count; i++)
                {
                    Thing thing = this.ship.GetInnerContainer()[i];
                    Pawn pawn = thing as Pawn;
                    if (nonPawn)
                    {
                        if (pawn == null || (pawn != null && !pawn.def.race.Humanlike))
                        {
                            this.DrawThingRow(ref num, viewRect.width-100f, thing);
                        }
                    }
                    else
                    {
                        if (pawn != null && pawn.def.race.Humanlike)
                        {
                            this.DrawThingRow(ref num, viewRect.width-100f, thing);
                        }
                    }
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                this.scrollViewHeight = num + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public void DrawWeaponSlots(Rect inRect)
        {
            Rect rect1 = inRect;
            float num = inRect.y;
            foreach (KeyValuePair<ShipWeaponSlot, Building_ShipTurret> currentWeapon in ship.installedTurrets)
            {
                DrawWeaponsTurretRow(ref num, rect1.width, currentWeapon);
            }
            foreach (KeyValuePair<ShipWeaponSlot, WeaponSystemShipBomb> currentbomb in ship.Payload)
            {
                DrawWeaponsPayloadRow(ref num, rect1.width, currentbomb);
            }
        }

        private void DrawWeaponsTurretRow(ref float y, float width, KeyValuePair<ShipWeaponSlot, Building_ShipTurret> currentWeapon)
        {
            Rect rectslotName = new Rect(10f, y, 100f, 30f);
            Widgets.Label(rectslotName, currentWeapon.Key.SlotName);

            Rect rectslotIcon = new Rect(rectslotName.xMax + 5f, y, 30f, 30f);
            if (currentWeapon.Value == null)
            {
                Widgets.DrawWindowBackground(rectslotIcon);
            }
            else
            {
                Texture2D tex = ContentFinder<Texture2D>.Get(currentWeapon.Value.def.building.turretTopGraphicPath);
                GUI.DrawTexture(rectslotIcon, tex);
                Widgets.DrawWindowBackground(rectslotIcon);
            }

            if (Mouse.IsOver(rectslotIcon))
            {
                GUI.color = ITab_ShipCargo.HighlightColor;
                GUI.DrawTexture(rectslotIcon, TexUI.HighlightTex);
            }
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(rectslotIcon))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                if (currentWeapon.Value == null)
                {
                    List<Thing> list = DropShipUtility.availableWeaponsForSlot(this.ship.Map, currentWeapon.Key);
                    //         Log.Message("List of potentials  " + list.Count.ToString());
                    list.OrderBy(x => x.Position.DistanceToSquared(this.ship.Position));
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing weapon = list[i];
                        Action action = new Action(delegate
                        {
                            ship.TryModifyWeaponSystem(currentWeapon.Key, weapon, true);
                        });

                        FloatMenuOption newOption = new FloatMenuOption("Install".Translate() + weapon.Label, action);
                        opts.Add(newOption);
                    }
                }
                else
                {
                    Action action = new Action(delegate
                    {
                        ship.TryModifyWeaponSystem(currentWeapon.Key, currentWeapon.Value, false);
                    });
                    FloatMenuOption newOption = new FloatMenuOption("Uninstall".Translate() + currentWeapon.Value.Label, action);
                    opts.Add(newOption);
                }
                if (opts.Count < 1)
                {
                    opts.Add(new FloatMenuOption("None", null));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            Rect rect3 = new Rect(rectslotIcon.xMax + 10f, y, width - rectslotName.width - rectslotIcon.width - 10f, 30f);

            if (currentWeapon.Value == null && !ship.weaponsToInstall.Any(x => x.Key == currentWeapon.Key))
            {
                Widgets.Label(rect3, "NoneInstalled".Translate());
            }
            else
            {
                ShipWeaponSlot installingSlot = ship.weaponsToInstall.FirstOrDefault(x => x.Key == currentWeapon.Key).Key;
                if (installingSlot != null)
                {
                    Widgets.Label(rect3, "InstallingShipWeapon".Translate(new object[]
                        {
                        ship.weaponsToInstall[installingSlot].LabelCap
                        }));
                }
                else
                {
                    Widgets.Label(rect3, currentWeapon.Value.def.LabelCap);
                }

            }
            y += 35f;
        }

        private void DrawWeaponsPayloadRow(ref float y, float width, KeyValuePair<ShipWeaponSlot, WeaponSystemShipBomb> currentWeapon)
        {
            Rect rectslotName = new Rect(10f, y, 100f, 30f);
            Widgets.Label(rectslotName, currentWeapon.Key.SlotName);

            Rect rectslotIcon = new Rect(rectslotName.xMax + 5f, y, 30f, 30f);
            if (currentWeapon.Value == null)
            {
                Widgets.DrawWindowBackground(rectslotIcon);
            }
            else
            {
                Texture2D tex = currentWeapon.Value.def.uiIcon;
                GUI.DrawTexture(rectslotIcon, currentWeapon.Value.def.uiIcon);
                Widgets.DrawWindowBackground(rectslotIcon);
            }

            if (Mouse.IsOver(rectslotIcon))
            {
                GUI.color = ITab_ShipCargo.HighlightColor;
                GUI.DrawTexture(rectslotIcon, TexUI.HighlightTex);
            }
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(rectslotIcon))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                if (currentWeapon.Value == null)
                {
                    List<Thing> list = DropShipUtility.availableWeaponsForSlot(this.ship.Map, currentWeapon.Key);
                    list.OrderBy(x => x.Position.DistanceToSquared(this.ship.Position));
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing weapon = list[i];
                        Action action = new Action(delegate
                        {
                            ship.TryModifyWeaponSystem(currentWeapon.Key, weapon, true);
                        });

                        FloatMenuOption newOption = new FloatMenuOption("Install".Translate() + weapon.Label, action);
                        opts.Add(newOption);
                    }
                }
                else
                {
                    Action action = new Action(delegate
                    {
                        ship.TryModifyWeaponSystem(currentWeapon.Key, currentWeapon.Value, false);
                    });
                    FloatMenuOption newOption = new FloatMenuOption("Uninstall".Translate() + currentWeapon.Value.Label, action);
                    opts.Add(newOption);
                }
                if (opts.Count < 1)
                {
                    opts.Add(new FloatMenuOption("None", null));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            Rect rect3 = new Rect(rectslotIcon.xMax + 10f, y, width - rectslotName.width - rectslotIcon.width - 10f, 30f);

            if (currentWeapon.Value == null)
            {
                Widgets.Label(rect3, "NoneInstalled".Translate());
            }
            else if (ship.weaponsToInstall.Any(x => x.Key == currentWeapon.Key))
            {
                Widgets.Label(rect3, "InstallingShipWeapon".Translate(new object[]
                    {
                        currentWeapon.Value.LabelCap
                    }));
            }
            else
            {
                Widgets.Label(rect3, currentWeapon.Value.def.LabelCap);
            }

            y += 35f;
        }

        private void DrawThingRow(ref float y, float width, Thing thing)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            Widgets.InfoCardButton(rect.width - 24f, y, thing);
            rect.width -= 24f;
            if (this.CanControl)
            {
                Rect rect2 = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                if (Widgets.ButtonImage(rect2, DropShipUtility.DropTexture))
                {
                    Verse.Sound.SoundStarter.PlayOneShotOnCamera(SoundDefOf.TickHigh);
                    this.InterfaceDrop(thing, this.ship);
                }
                rect.width -= 24f;
            }
            if (Mouse.IsOver(rect))
            {
                GUI.color = ITab_ShipCargo.HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);        
            }
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ITab_ShipCargo.ThingLabelColor;
            Rect rect3 = new Rect(36f, y, width - 36f, 28f);
            string text = thing.LabelCap;

            Widgets.Label(rect3, text);
            y += 32f;
        }

        private void InterfaceDrop(Thing thing, ShipBase ship)
        {
            ship.GetInnerContainer().TryDrop(thing, ThingPlaceMode.Near, out thing);
            if (thing is Pawn)
            {
                Pawn pawn = (Pawn)thing;
                Lord LoadLord = LoadShipCargoUtility.FindLoadLord(ship, ship.Map);
                if (LoadLord != null)
                {
                    LoadLord.ownedPawns.Remove(pawn);
                }
            }
        }
    }
}
