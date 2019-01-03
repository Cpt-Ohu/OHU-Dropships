using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OHUShips
{
    public class Designator_ZoneAddRunway : Designator_ZoneAdd
    {

        public Designator_ZoneAddRunway()
        {
            this.zoneTypeToPlace = typeof(Zone_Growing);
            this.defaultLabel = "Runway".Translate();
            this.defaultDesc = "DesignatorRunwayZoneDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/Zone_AddRunway", true);
            this.hotKey = KeyBindingDefOf.Misc2;
            this.tutorTag = "ZoneAdd_Growing";
        }
        protected override string NewZoneLabel
        {
            get
            {
                return "Runway".Translate();
            }
        }
        protected override Zone MakeNewZone()
        {
            return new Zone_Runway(Find.CurrentMap.zoneManager);
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            AcceptanceReport result = base.CanDesignateCell(c);
            if (!result.Accepted)
            {
                return result;
            }
            TerrainDef terrain = c.GetTerrain(base.Map);
            if (terrain.passability == Traversability.Impassable)
            {
                return false;
            }
            List<Thing> list = base.Map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].def.CanOverlapZones)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
