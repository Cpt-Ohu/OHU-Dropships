using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OHUShips
{
    public static class RunwayUtility
    {

        private static Rot4[] tmpRotations = new Rot4[]
        {
            Rot4.North,
            Rot4.South,
            Rot4.West,
            Rot4.East
        };

        public static IntVec3 FindShipDropPoint(Map map, CellRect rect, ThingDef thingDef, ref Rot4? rot, out bool hasToWipeBuilding, out bool doesntFit)
        {
            if (!thingDef.rotatable)
            {
                rot = new Rot4?(Rot4.North);
            }
            if (!rot.HasValue)
            {
                RunwayUtility.tmpRotations.Shuffle<Rot4>();
                for (int i = 0; i < RunwayUtility.tmpRotations.Length; i++)
                {
                    IntVec3 result = RunwayUtility.FindBestSpawnCellForNonItem(map, rect, thingDef, RunwayUtility.tmpRotations[i], out hasToWipeBuilding, out doesntFit);
                    if (!hasToWipeBuilding && !doesntFit)
                    {
                        rot = new Rot4?(RunwayUtility.tmpRotations[i]);
                        return result;
                    }
                }
                for (int j = 0; j < RunwayUtility.tmpRotations.Length; j++)
                {
                    IntVec3 result2 = RunwayUtility.FindBestSpawnCellForNonItem(map, rect, thingDef, RunwayUtility.tmpRotations[j], out hasToWipeBuilding, out doesntFit);
                    if (!doesntFit)
                    {
                        rot = new Rot4?(RunwayUtility.tmpRotations[j]);
                        return result2;
                    }
                }
                rot = new Rot4?(Rot4.Random);
                return RunwayUtility.FindBestSpawnCellForNonItem(map, rect, thingDef, rot.Value, out hasToWipeBuilding, out doesntFit);
            }
            return RunwayUtility.FindBestSpawnCellForNonItem(map, rect, thingDef, rot.Value, out hasToWipeBuilding, out doesntFit);
        }

        // RimWorld.BaseGen.SymbolResolver_SingleThing
        private static IntVec3 FindBestSpawnCellForNonItem(Map map, CellRect rect, ThingDef thingDef, Rot4 rot, out bool hasToWipeBuilding, out bool doesntFit)
        {
            if (thingDef.category == ThingCategory.Building)
            {
                foreach (IntVec3 current in rect.Cells.InRandomOrder(null))
                {
                    CellRect rect2 = GenAdj.OccupiedRect(current, rot, thingDef.size);
                    if (rect2.FullyContainedWithin(rect) && !BaseGenUtility.AnyDoorAdjacentCardinalTo(rect2, map) && !RunwayUtility.AnyNonStandableCellOrAnyBuildingInside(map, rect2) && GenConstruct.TerrainCanSupport(rect2, map, thingDef))
                    {
                        hasToWipeBuilding = false;
                        doesntFit = false;
                        IntVec3 result = current;
                        return result;
                    }
                }
                foreach (IntVec3 current2 in rect.Cells.InRandomOrder(null))
                {
                    CellRect rect3 = GenAdj.OccupiedRect(current2, rot, thingDef.size);
                    if (rect3.FullyContainedWithin(rect) && !BaseGenUtility.AnyDoorAdjacentCardinalTo(rect3, map) && !RunwayUtility.AnyNonStandableCellOrAnyBuildingInside(map, rect3))
                    {
                        hasToWipeBuilding = false;
                        doesntFit = false;
                        IntVec3 result = current2;
                        return result;
                    }
                }
            }
            foreach (IntVec3 current3 in rect.Cells.InRandomOrder(null))
            {
                CellRect rect4 = GenAdj.OccupiedRect(current3, rot, thingDef.size);
                if (rect4.FullyContainedWithin(rect) && !RunwayUtility.AnyNonStandableCellOrAnyBuildingInside(map, rect4))
                {
                    hasToWipeBuilding = false;
                    doesntFit = false;
                    IntVec3 result = current3;
                    return result;
                }
            }
            foreach (IntVec3 current4 in rect.Cells.InRandomOrder(null))
            {
                if (GenAdj.OccupiedRect(current4, rot, thingDef.size).FullyContainedWithin(rect))
                {
                    hasToWipeBuilding = true;
                    doesntFit = false;
                    IntVec3 result = current4;
                    return result;
                }
            }
            IntVec3 centerCell = rect.CenterCell;
            CellRect cellRect = GenAdj.OccupiedRect(centerCell, rot, thingDef.size);
            if (cellRect.minX < 0)
            {
                centerCell.x += -cellRect.minX;
            }
            if (cellRect.minZ < 0)
            {
                centerCell.z += -cellRect.minZ;
            }
            if (cellRect.maxX >= map.Size.x)
            {
                centerCell.x -= cellRect.maxX - map.Size.x + 1;
            }
            if (cellRect.maxZ >= map.Size.z)
            {
                centerCell.z -= cellRect.maxZ - map.Size.z + 1;
            }
            hasToWipeBuilding = true;
            doesntFit = true;
            return centerCell;
        }

        static bool AnyNonStandableCellOrAnyBuildingInside(Map map, CellRect rect)
        {
            CellRect.CellRectIterator iterator = rect.GetIterator();
            while (!iterator.Done())
            {
                if (!iterator.Current.Standable(map))
                {
                    return true;
                }
                if (iterator.Current.GetEdifice(map) != null)
                {
                    return true;
                }
                iterator.MoveNext();
            }
            return false;
        }

    }
}
