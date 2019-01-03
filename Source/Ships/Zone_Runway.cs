using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OHUShips
{
    public class Zone_Runway : Zone
    {
        public Zone_Runway()
        {
        }

        public Zone_Runway(ZoneManager zoneManager) : base("Zone_Runway".Translate(), zoneManager)
        {
        }

        public override void AddCell(IntVec3 c)
        {
            base.AddCell(c);

        }

        protected override Color NextZoneColor
        {
            get
            {
                Color baseColor = Color.Lerp(new Color(1f, 0f, 0f), Color.gray, 0.5f);
                return new Color(baseColor.r, baseColor.g, baseColor.b, 0.09f);
            }
        }

        bool deleteAll = false;

        public override void Delete()
        {
            this.deleteAll = true;
            base.Delete();
        }

        public override void RemoveCell(IntVec3 c)
        {
            if (deleteAll == false)
            {
                IEnumerable<Thing> things = this.Map.thingGrid.ThingsAt(c);
                foreach (var thing in things)
                {
                    if (typeof(ShipBase).IsAssignableFrom(thing.GetType()) | typeof(ShipBase_Traveling).IsAssignableFrom(thing.GetType()))
                    {
                        //if (thing.def.Size.x * thing.def.Size.z > this.cells.Count)
                        //{
                            return;
                        //}
                    }
                }
            }
            base.RemoveCell(c);
        }

        public override bool IsMultiselectable
        {
            get
            {
                return true;
            }
        }
    }
}
