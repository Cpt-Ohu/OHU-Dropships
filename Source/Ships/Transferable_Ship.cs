using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OHUShips
{
    public class Transferable_Ship : TransferableOneWay
    {
        public override AcceptanceReport UnderflowReport()
        {
            return true;
        }

        public override AcceptanceReport OverflowReport()
        {
            return true;
        }

    }
}
