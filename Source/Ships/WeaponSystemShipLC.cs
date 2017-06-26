using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace OHUShips
{
    [StaticConstructorOnStartup]
    public class WeaponSystemShipLC : WeaponSystem
    {
        public CompShipWeapon compWeapon;

        public ShipBase ship;
        
        public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));
        
        public WeaponSystemShipLC()
        {
        }

        public override void PostMake()
        {
            base.PostMake();
            this.compWeapon = base.GetComp<CompShipWeapon>();            
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }
    }
}
