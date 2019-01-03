using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OHUShips
{
    public class CorruptionInterface
    {
        private Type GetCorruptionType(string typeName)
        {
            Assembly assembly = Assembly.LoadFrom("Corruption.dll");

            Type type = assembly.GetType(typeName);

            object instanceOfMyType = Activator.CreateInstance(type);
            return type;
        }

        public void FinishMission(string missionDefName)
        {
            var type = GetCorruptionType("CorruptionStoryTrackerUtilities");
            MethodInfo parseMethod = type.GetMethod("Parse");
            object value = parseMethod.Invoke(null, new object[] { missionDefName });
        }

    }
}
