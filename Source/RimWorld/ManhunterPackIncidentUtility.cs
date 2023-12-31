using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public static class ManhunterPackIncidentUtility
{
	public const int MinAnimalCount = 2;

	public const int MaxAnimalCount = 100;

	public const float MinPoints = 70f;

	public static float ManhunterAnimalWeight(PawnKindDef animal, float points)
	{
		points = Mathf.Max(points, 70f);
		if (animal.combatPower * 2f > points)
		{
			return 0f;
		}
		int num = Mathf.Min(Mathf.RoundToInt(points / animal.combatPower), 100);
		return Mathf.Clamp01(Mathf.InverseLerp(100f, 10f, num));
	}

	public static bool TryFindManhunterAnimalKind(float points, int tile, out PawnKindDef animalKind)
	{
		IEnumerable<PawnKindDef> source = DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef k) => CanArriveManhunter(k) && (tile == -1 || Find.World.tileTemperatures.SeasonAndOutdoorTemperatureAcceptableFor(tile, k.race)));
		if (source.Any())
		{
			if (source.TryRandomElementByWeight((PawnKindDef a) => ManhunterAnimalWeight(a, points), out animalKind))
			{
				return true;
			}
			if (points > source.Min((PawnKindDef a) => a.combatPower) * 2f)
			{
				animalKind = source.MaxBy((PawnKindDef a) => a.combatPower);
				return true;
			}
		}
		animalKind = null;
		return false;
	}

	public static int GetAnimalsCount(PawnKindDef animalKind, float points)
	{
		return Mathf.Clamp(Mathf.RoundToInt(points / animalKind.combatPower), 2, 100);
	}

	public static List<Pawn> GenerateAnimals(PawnKindDef animalKind, int tile, float points, int animalCount = 0)
	{
		List<Pawn> list = new List<Pawn>();
		int num = ((animalCount > 0) ? animalCount : GetAnimalsCount(animalKind, points));
		for (int i = 0; i < num; i++)
		{
			Pawn item = PawnGenerator.GeneratePawn(new PawnGenerationRequest(animalKind, null, PawnGenerationContext.NonPlayer, tile));
			list.Add(item);
		}
		return list;
	}

	[DebugOutput]
	public static void ManhunterResults()
	{
		List<PawnKindDef> candidates = (from k in DefDatabase<PawnKindDef>.AllDefs.Where(CanArriveManhunter)
			orderby 0f - k.combatPower
			select k).ToList();
		List<float> list = new List<float>();
		for (int i = 0; i < 30; i++)
		{
			list.Add(20f * Mathf.Pow(1.25f, i));
		}
		DebugTables.MakeTablesDialog(list, (float points) => points.ToString("F0") + " pts", candidates, (PawnKindDef candidate) => candidate.defName + " (" + candidate.combatPower.ToString("F0") + ")", delegate(float points, PawnKindDef candidate)
		{
			float num = candidates.Sum((PawnKindDef k) => ManhunterAnimalWeight(k, points));
			float num2 = ManhunterAnimalWeight(candidate, points);
			return (num2 == 0f) ? "0%" : string.Format("{0}%, {1}", (num2 * 100f / num).ToString("F0"), Mathf.Max(Mathf.RoundToInt(points / candidate.combatPower), 1));
		});
	}

	private static bool CanArriveManhunter(PawnKindDef kind)
	{
		if (kind.RaceProps.Animal && kind.canArriveManhunter)
		{
			return kind.RaceProps.CanPassFences;
		}
		return false;
	}
}
