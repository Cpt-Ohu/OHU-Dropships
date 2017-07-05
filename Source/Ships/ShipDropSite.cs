using RimWorld.Planet;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse.Sound;

namespace OHUShips
{
    public class ShipDropSite : MapParent
    {
        private const int timeToRemove = 2000;

        private bool forcedRemoval = false;
        
        private int timePresent = 0;

        private Material cachedMat;

        public override Material Material
        {
            get
            {
                if (this.cachedMat == null)
                { 
                    this.cachedMat = MaterialPool.MatFrom("World/WorldObjects/AircraftDropSpot", ShaderDatabase.WorldOverlayTransparentLit, base.Faction.Color, WorldMaterials.WorldObjectRenderQueue);
                }
                return this.cachedMat;
            }
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.timePresent, "timePresent", 0);
            Scribe_Values.Look<bool>(ref this.forcedRemoval, "forcedRemoval", false);
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            if ((!base.Map.mapPawns.AnyPawnBlockingMapRemoval && timePresent > timeToRemove && !this.Map.listerThings.AllThings.Any(x => x.Faction == Faction.OfPlayer || x is ShipBase_Traveling)) || this.forcedRemoval)
            {
                alsoRemoveWorldObject = true;
                return true;
            }

            alsoRemoveWorldObject = false ;
            return false;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerator<Gizmo> enumerator = base.GetGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }

            if (!this.Map.listerThings.AllThings.Any(x => x is ShipBase || x is ShipBase_Traveling))
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "CommandRemoveDropsite".Translate();
                command_Action.defaultDesc = "CommandRemoveDropsiteDesc".Translate();
                command_Action.icon = DropShipUtility.CancelTex;
                command_Action.action = delegate
                {
                    SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
                    this.forcedRemoval = true;
                };
                yield return command_Action;
            }
        }

        public override void Tick()
        {
            base.Tick();
            timePresent ++;
        }
    }
}
