using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class Alert_PollutedTerrain : Alert
{
	private const float ToxicBuildupSeverityThreshold = 0.3f;

	private List<Pawn> tmpPawnList = new List<Pawn>();

	public List<Pawn> Targets
	{
		get
		{
			tmpPawnList.Clear();
			foreach (Map map in Find.Maps)
			{
				foreach (Pawn item in map.mapPawns.FreeColonistsSpawned)
				{
					Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
					if (firstHediffOfDef != null && firstHediffOfDef.Severity >= 0.3f && item.Position.IsPolluted(map))
					{
						tmpPawnList.Add(item);
					}
				}
			}
			return tmpPawnList;
		}
	}

	public Alert_PollutedTerrain()
	{
		defaultLabel = "AlertPollutedTerrain".Translate();
		defaultPriority = AlertPriority.High;
	}

	public override AlertReport GetReport()
	{
		if (!ModsConfig.BiotechActive)
		{
			return false;
		}
		return AlertReport.CulpritsAre(Targets);
	}

	public override TaggedString GetExplanation()
	{
		string text = Targets.Select((Pawn p) => p.NameShortColored.Resolve()).ToLineList(" - ");
		return "AlertPollutedTerrainDesc".Translate(text);
	}
}
