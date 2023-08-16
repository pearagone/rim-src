using Verse;

namespace RimWorld.Planet;

public class FeatureWorker_Peninsula : FeatureWorker_Protrusion
{
	protected override bool IsRoot(int tile)
	{
		BiomeDef biome = Find.WorldGrid[tile].biome;
		if (biome != BiomeDefOf.Ocean)
		{
			return biome != BiomeDefOf.Lake;
		}
		return false;
	}
}
