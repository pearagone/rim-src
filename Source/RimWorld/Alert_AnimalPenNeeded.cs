using System.Collections.Generic;
using System.Text;
using RimWorld.Planet;
using Verse;

namespace RimWorld;

public class Alert_AnimalPenNeeded : Alert
{
	private List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();

	private bool anyHandlers;

	public Alert_AnimalPenNeeded()
	{
		defaultLabel = "AlertAnimalPenNeeded".Translate();
	}

	private void CalculateTargets()
	{
		targets.Clear();
		anyHandlers = false;
		foreach (Map map in Find.Maps)
		{
			if (!map.IsPlayerHome)
			{
				continue;
			}
			Pawn pawn = null;
			int num = -1;
			foreach (Pawn item in map.mapPawns.FreeColonistsSpawned)
			{
				if (item.workSettings.WorkIsActive(WorkTypeDefOf.Handling))
				{
					int level = item.skills.GetSkill(SkillDefOf.Animals).Level;
					if (level > num)
					{
						pawn = item;
						num = level;
					}
				}
			}
			foreach (Pawn item2 in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
			{
				if (AnimalPenUtility.NeedsToBeManagedByRope(item2) && item2.CanReachMapEdge())
				{
					bool flag = !AnimalPenUtility.AnySuitablePens(item2, allowUnenclosedPens: true) && !AnimalPenUtility.AnySuitableHitch(item2);
					if (pawn == null)
					{
						flag = true;
					}
					else
					{
						anyHandlers = true;
					}
					if (flag)
					{
						targets.Add(item2);
					}
				}
			}
		}
	}

	public override TaggedString GetExplanation()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("AlertAnimalPenNeededDesc".Translate());
		if (!anyHandlers)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("AlertAnimalPenNeededNoHandlers".Translate());
		}
		stringBuilder.AppendLine();
		stringBuilder.Append("AlertAnimalPenNeededDescExplanation".Translate());
		return stringBuilder.ToString();
	}

	public override AlertReport GetReport()
	{
		CalculateTargets();
		return AlertReport.CulpritsAre(targets);
	}
}
