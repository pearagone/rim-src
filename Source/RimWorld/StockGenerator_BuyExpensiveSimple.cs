using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class StockGenerator_BuyExpensiveSimple : StockGenerator
{
	public float minValuePerUnit = 15f;

	public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
	{
		return Enumerable.Empty<Thing>();
	}

	public override bool HandlesThingDef(ThingDef thingDef)
	{
		if (thingDef.category != ThingCategory.Item || thingDef.IsApparel || thingDef.IsWeapon || thingDef.IsMedicine || thingDef.IsDrug)
		{
			return false;
		}
		if (thingDef == ThingDefOf.InsectJelly)
		{
			return true;
		}
		return thingDef.BaseMarketValue / thingDef.VolumePerUnit >= minValuePerUnit;
	}
}
