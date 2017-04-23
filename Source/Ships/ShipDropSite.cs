using RimWorld.Planet;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OHUShips
{
    public class ShipDropSite : MapParent
    {
        private const int timeToRemove = 2000;

        private int timePresent = 0;


        private Material cachedMat;

        public override Material Material
        {
            get
            {
                if (this.cachedMat == null)
                {
                    this.cachedMat = MaterialPool.MatFrom("World/WorldObjects/AircraftDropSpot", ShaderDatabase.WorldOverlayTransparentLit, base.Faction.Color);
                }
                return this.cachedMat;
            }
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            if (!base.Map.mapPawns.AnyColonistTameAnimalOrPrisonerOfColony && timePresent > timeToRemove && !this.Map.listerThings.AllThings.Any(x => x.Faction == Faction.OfPlayer || x is ShipBase_Traveling))
            {
                alsoRemoveWorldObject = true;
                return true;
            }
            alsoRemoveWorldObject = false;
            return false;
        }

        public override void Tick()
        {
            base.Tick();
            timePresent ++;
        }
    }
}
