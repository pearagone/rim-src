using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld;

public static class PawnApparelGenerator
{
	private class PossibleApparelSet
	{
		private List<ThingStuffPair> aps = new List<ThingStuffPair>();

		private HashSet<ApparelUtility.LayerGroupPair> lgps = new HashSet<ApparelUtility.LayerGroupPair>();

		private BodyDef body;

		private ThingDef raceDef;

		private Pawn pawn;

		private const float StartingMinTemperature = 12f;

		private const float TargetMinTemperature = -40f;

		private const float StartingMaxTemperature = 32f;

		private const float TargetMaxTemperature = 30f;

		private const float MinToxicEnvironmentResistanceForFreeApparel = 0.25f;

		private const float MinToxicEnvironmentResistanceImprovement = 0.15f;

		private static readonly SimpleCurve ToxicEnvironmentResistanceOverPollutionCurve = new SimpleCurve
		{
			new CurvePoint(0f, 0f),
			new CurvePoint(0.5f, 0.5f),
			new CurvePoint(1f, 0.85f)
		};

		public int Count => aps.Count;

		public float TotalPrice => aps.Sum((ThingStuffPair pa) => pa.Price);

		public float TotalInsulationCold => aps.Sum((ThingStuffPair a) => a.InsulationCold);

		public List<ThingStuffPair> ApparelsForReading => aps;

		public void Reset(BodyDef body, ThingDef raceDef)
		{
			aps.Clear();
			lgps.Clear();
			this.body = body;
			this.raceDef = raceDef;
			pawn = null;
		}

		public void Reset(Pawn pawn)
		{
			aps.Clear();
			lgps.Clear();
			this.pawn = pawn;
			body = pawn?.RaceProps?.body;
			raceDef = pawn?.def;
		}

		public void Add(ThingStuffPair pair)
		{
			aps.Add(pair);
			for (int i = 0; i < pair.thing.apparel.layers.Count; i++)
			{
				ApparelLayerDef layer = pair.thing.apparel.layers[i];
				BodyPartGroupDef[] interferingBodyPartGroups = pair.thing.apparel.GetInterferingBodyPartGroups(body);
				for (int j = 0; j < interferingBodyPartGroups.Length; j++)
				{
					lgps.Add(new ApparelUtility.LayerGroupPair(layer, interferingBodyPartGroups[j]));
				}
			}
		}

		public bool PairOverlapsAnything(ThingStuffPair pair)
		{
			if (!lgps.Any())
			{
				return false;
			}
			for (int i = 0; i < pair.thing.apparel.layers.Count; i++)
			{
				ApparelLayerDef layer = pair.thing.apparel.layers[i];
				BodyPartGroupDef[] interferingBodyPartGroups = pair.thing.apparel.GetInterferingBodyPartGroups(body);
				for (int j = 0; j < interferingBodyPartGroups.Length; j++)
				{
					if (lgps.Contains(new ApparelUtility.LayerGroupPair(layer, interferingBodyPartGroups[j])))
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool CoatButNoShirt()
		{
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < aps.Count; i++)
			{
				if (!aps[i].thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
				{
					continue;
				}
				for (int j = 0; j < aps[i].thing.apparel.layers.Count; j++)
				{
					ApparelLayerDef apparelLayerDef = aps[i].thing.apparel.layers[j];
					if (apparelLayerDef == ApparelLayerDefOf.OnSkin)
					{
						flag2 = true;
					}
					if (apparelLayerDef == ApparelLayerDefOf.Shell || apparelLayerDef == ApparelLayerDefOf.Middle)
					{
						flag = true;
					}
				}
			}
			if (flag)
			{
				return !flag2;
			}
			return false;
		}

		public bool Covers(BodyPartGroupDef bp)
		{
			for (int i = 0; i < aps.Count; i++)
			{
				if ((bp != BodyPartGroupDefOf.Legs || !aps[i].thing.apparel.legsNakedUnlessCoveredBySomethingElse) && aps[i].thing.apparel.bodyPartGroups.Contains(bp))
				{
					return true;
				}
			}
			return false;
		}

		public bool IsNaked(Gender gender)
		{
			switch (gender)
			{
			case Gender.Male:
				return !Covers(BodyPartGroupDefOf.Legs);
			case Gender.Female:
				if (Covers(BodyPartGroupDefOf.Legs))
				{
					return !Covers(BodyPartGroupDefOf.Torso);
				}
				return true;
			case Gender.None:
				return false;
			default:
				return false;
			}
		}

		public bool SatisfiesNeededWarmth(NeededWarmth warmth, bool mustBeSafe = false, float mapTemperature = 21f)
		{
			if (warmth == NeededWarmth.Any)
			{
				return true;
			}
			if (mustBeSafe && !SafeTemperature(mapTemperature))
			{
				return false;
			}
			return warmth switch
			{
				NeededWarmth.Cool => aps.Sum((ThingStuffPair a) => a.InsulationHeat) >= -2f, 
				NeededWarmth.Warm => aps.Sum((ThingStuffPair a) => a.InsulationCold) >= 52f, 
				_ => throw new NotImplementedException(), 
			};
		}

		private bool SafeTemperature(float temp)
		{
			if (pawn != null)
			{
				return pawn.SafeTemperatureRange(aps).Includes(temp);
			}
			return GenTemperature.SafeTemperatureRange(raceDef, aps).Includes(temp);
		}

		public void AddFreeWarmthAsNeeded(NeededWarmth warmth, float mapTemperature, Pawn pawn)
		{
			if (warmth == NeededWarmth.Any || warmth == NeededWarmth.Cool)
			{
				return;
			}
			if (DebugViewSettings.logApparelGeneration)
			{
				debugSb.AppendLine();
				debugSb.AppendLine("Trying to give free warm layer.");
			}
			for (int i = 0; i < 3; i++)
			{
				if (!SatisfiesNeededWarmth(warmth, mustBeSafe: true, mapTemperature))
				{
					if (DebugViewSettings.logApparelGeneration)
					{
						debugSb.AppendLine("Checking to give free torso-cover at max price " + freeWarmParkaMaxPrice);
					}
					Predicate<ThingStuffPair> parkaPairValidator = delegate(ThingStuffPair pa)
					{
						if (pa.Price > freeWarmParkaMaxPrice)
						{
							return false;
						}
						if (pa.InsulationCold <= 0f)
						{
							return false;
						}
						if (!pa.thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
						{
							return false;
						}
						if (!pa.thing.apparel.canBeGeneratedToSatisfyWarmth)
						{
							return false;
						}
						if (!pa.thing.apparel.CorrectAgeForWearing(pawn))
						{
							return false;
						}
						return (!(GetReplacedInsulationCold(pa) >= pa.InsulationCold)) ? true : false;
					};
					for (int j = 0; j < 2; j++)
					{
						ThingStuffPair candidate;
						if (j == 0)
						{
							if (!allApparelPairs.Where((ThingStuffPair pa) => parkaPairValidator(pa) && pa.InsulationCold < 40f).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality / (pa.Price * pa.Price), out candidate))
							{
								continue;
							}
						}
						else if (!allApparelPairs.Where((ThingStuffPair pa) => parkaPairValidator(pa)).TryMaxBy((ThingStuffPair x) => x.InsulationCold - GetReplacedInsulationCold(x), out candidate))
						{
							continue;
						}
						if (DebugViewSettings.logApparelGeneration)
						{
							debugSb.AppendLine(string.Concat("Giving free torso-cover: ", candidate, " insulation=", candidate.InsulationCold));
							foreach (ThingStuffPair item in aps.Where((ThingStuffPair a) => !ApparelUtility.CanWearTogether(a.thing, candidate.thing, body)))
							{
								debugSb.AppendLine("    -replaces " + item.ToString() + " InsulationCold=" + item.InsulationCold);
							}
						}
						aps.RemoveAll((ThingStuffPair pa) => !ApparelUtility.CanWearTogether(pa.thing, candidate.thing, body));
						aps.Add(candidate);
						break;
					}
				}
				if (SafeTemperature(mapTemperature))
				{
					break;
				}
			}
			if (!SatisfiesNeededWarmth(warmth, mustBeSafe: true, mapTemperature))
			{
				if (DebugViewSettings.logApparelGeneration)
				{
					debugSb.AppendLine("Checking to give free hat at max price " + freeWarmHatMaxPrice);
				}
				Predicate<ThingStuffPair> hatPairValidator = delegate(ThingStuffPair pa)
				{
					if (pa.Price > freeWarmHatMaxPrice)
					{
						return false;
					}
					if (pa.InsulationCold < 7f)
					{
						return false;
					}
					if (!pa.thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead) && !pa.thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead))
					{
						return false;
					}
					return (!(GetReplacedInsulationCold(pa) >= pa.InsulationCold)) ? true : false;
				};
				if (allApparelPairs.Where((ThingStuffPair pa) => hatPairValidator(pa)).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality / (pa.Price * pa.Price), out var hatPair))
				{
					if (DebugViewSettings.logApparelGeneration)
					{
						debugSb.AppendLine(string.Concat("Giving free hat: ", hatPair, " insulation=", hatPair.InsulationCold));
						foreach (ThingStuffPair item2 in aps.Where((ThingStuffPair a) => !ApparelUtility.CanWearTogether(a.thing, hatPair.thing, body)))
						{
							debugSb.AppendLine("    -replaces " + item2.ToString() + " InsulationCold=" + item2.InsulationCold);
						}
					}
					aps.RemoveAll((ThingStuffPair pa) => !ApparelUtility.CanWearTogether(pa.thing, hatPair.thing, body));
					aps.Add(hatPair);
				}
			}
			if (DebugViewSettings.logApparelGeneration)
			{
				debugSb.AppendLine("New TotalInsulationCold: " + TotalInsulationCold);
			}
		}

		public bool SatisfiesNeededToxicEnvironmentResistance(float pollution)
		{
			if (pollution <= 0f)
			{
				return true;
			}
			return aps.Sum((ThingStuffPair ap) => ap.ToxicEnvironmentResistance) >= ToxicEnvironmentResistanceOverPollutionCurve.Evaluate(pollution);
		}

		public void AddFreeToxicEnvironmentResistanceAsNeeded(float pollution, Func<ThingStuffPair, bool> extraValidator = null)
		{
			Predicate<ThingStuffPair> pollutionApparelValidator = delegate(ThingStuffPair pa)
			{
				if (!pa.thing.apparel.canBeGeneratedToSatisfyToxicEnvironmentResistance)
				{
					return false;
				}
				if (pa.ToxicEnvironmentResistance <= 0.25f)
				{
					return false;
				}
				if (pa.Price > freeToxicEnvironmentResistanceApparelMaxPrice)
				{
					return false;
				}
				if (extraValidator != null && !extraValidator(pa))
				{
					return false;
				}
				for (int j = 0; j < aps.Count; j++)
				{
					if (!ApparelUtility.CanWearTogether(aps[j].thing, pa.thing, body) && aps[j].ToxicEnvironmentResistance >= 0.25f && Mathf.Abs(aps[j].ToxicEnvironmentResistance - pa.ToxicEnvironmentResistance) <= 0.15f)
					{
						return false;
					}
				}
				return true;
			};
			for (int i = 0; i < 5; i++)
			{
				if (SatisfiesNeededToxicEnvironmentResistance(pollution))
				{
					break;
				}
				if (!allApparelPairs.Where((ThingStuffPair pa) => pollutionApparelValidator(pa)).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality / (pa.Price * pa.Price), out var pollutionPair))
				{
					continue;
				}
				if (DebugViewSettings.logApparelGeneration)
				{
					debugSb.AppendLine(string.Concat("Giving free toxic environment resistance: ", pollutionPair, " ToxicEnvironmentResistance=", pollutionPair.ToxicEnvironmentResistance));
					foreach (ThingStuffPair item in aps.Where((ThingStuffPair a) => !ApparelUtility.CanWearTogether(a.thing, pollutionPair.thing, body)))
					{
						debugSb.AppendLine("    -replaces " + item.ToString() + " ToxicEnvironmentResistance=" + item.ToxicEnvironmentResistance);
					}
				}
				aps.RemoveAll((ThingStuffPair pa) => !ApparelUtility.CanWearTogether(pa.thing, pollutionPair.thing, body));
				aps.Add(pollutionPair);
			}
			if (DebugViewSettings.logApparelGeneration)
			{
				debugSb.AppendLine("New ToxicEnvironmentResistance: " + aps.Sum((ThingStuffPair a) => a.ToxicEnvironmentResistance));
			}
		}

		public void GiveToPawn(Pawn pawn)
		{
			for (int i = 0; i < aps.Count; i++)
			{
				Apparel apparel = (Apparel)ThingMaker.MakeThing(aps[i].thing, aps[i].stuff);
				PawnGenerator.PostProcessGeneratedGear(apparel, pawn);
				if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
				{
					pawn.apparel.Wear(apparel, dropReplacedApparel: false);
				}
			}
			for (int j = 0; j < aps.Count; j++)
			{
				for (int k = 0; k < aps.Count; k++)
				{
					if (j != k && !ApparelUtility.CanWearTogether(aps[j].thing, aps[k].thing, pawn.RaceProps.body))
					{
						Log.Error(string.Concat(pawn, " generated with apparel that cannot be worn together: ", aps[j], ", ", aps[k]));
						return;
					}
				}
			}
		}

		public float GetReplacedInsulationCold(ThingStuffPair newAp)
		{
			float num = 0f;
			for (int i = 0; i < aps.Count; i++)
			{
				if (!ApparelUtility.CanWearTogether(aps[i].thing, newAp.thing, body))
				{
					num += aps[i].InsulationCold;
				}
			}
			return num;
		}

		public override string ToString()
		{
			string text = "[";
			for (int i = 0; i < aps.Count; i++)
			{
				text = text + aps[i].ToString() + ", ";
			}
			return text + "]";
		}
	}

	private static List<ThingStuffPair> allApparelPairs;

	private static float freeWarmParkaMaxPrice;

	private static float freeWarmHatMaxPrice;

	private static float freeToxicEnvironmentResistanceApparelMaxPrice;

	private static PossibleApparelSet workingSet;

	private static StringBuilder debugSb;

	private const int PracticallyInfinity = 9999999;

	private const float MinMapPollutionForFreeToxicResistanceApparel = 0.05f;

	private static List<ThingStuffPair> tmpApparelCandidates;

	private static List<ThingStuffPair> usableApparel;

	static PawnApparelGenerator()
	{
		allApparelPairs = new List<ThingStuffPair>();
		workingSet = new PossibleApparelSet();
		debugSb = null;
		tmpApparelCandidates = new List<ThingStuffPair>();
		usableApparel = new List<ThingStuffPair>();
		Reset();
	}

	public static void Reset()
	{
		allApparelPairs = ThingStuffPair.AllWith((ThingDef td) => td.IsApparel);
		freeWarmParkaMaxPrice = (int)(StatDefOf.MarketValue.Worker.GetValueAbstract(ThingDefOf.Apparel_Parka, ThingDefOf.Cloth) * 1.3f);
		freeWarmHatMaxPrice = (int)(StatDefOf.MarketValue.Worker.GetValueAbstract(ThingDefOf.Apparel_Tuque, ThingDefOf.Cloth) * 1.3f);
		freeToxicEnvironmentResistanceApparelMaxPrice = (ModsConfig.BiotechActive ? ((int)(StatDefOf.MarketValue.Worker.GetValueAbstract(ThingDefOf.Apparel_GasMask) * 1.3f)) : 0);
	}

	public static void GenerateStartingApparelFor(Pawn pawn, PawnGenerationRequest request)
	{
		if (!pawn.RaceProps.ToolUser || !pawn.RaceProps.IsFlesh)
		{
			return;
		}
		pawn.apparel.DestroyAll();
		pawn.outfits?.forcedHandler?.Reset();
		float randomInRange = pawn.kindDef.apparelMoney.RandomInRange;
		float mapTemperature;
		NeededWarmth neededWarmth = ApparelWarmthNeededNow(pawn, request, out mapTemperature);
		bool allowHeadgear = Rand.Value < pawn.kindDef.apparelAllowHeadgearChance;
		float num = ApparelToxicEnvironmentToAddress(pawn, request);
		debugSb = null;
		if (DebugViewSettings.logApparelGeneration)
		{
			debugSb = new StringBuilder();
			debugSb.AppendLine("Generating apparel for " + pawn);
			debugSb.AppendLine("Money: " + randomInRange.ToString("F0"));
			debugSb.AppendLine("Needed warmth: " + neededWarmth);
			debugSb.AppendLine("Needed toxic environment resistance: " + num);
			debugSb.AppendLine("Headgear allowed: " + allowHeadgear);
		}
		int @int = Rand.Int;
		tmpApparelCandidates.Clear();
		for (int i = 0; i < allApparelPairs.Count; i++)
		{
			ThingStuffPair thingStuffPair = allApparelPairs[i];
			if (CanUsePair(thingStuffPair, pawn, randomInRange, allowHeadgear, @int))
			{
				tmpApparelCandidates.Add(thingStuffPair);
			}
		}
		if (randomInRange < 0.001f)
		{
			GenerateWorkingPossibleApparelSetFor(pawn, randomInRange, tmpApparelCandidates);
		}
		else
		{
			int num2 = 0;
			while (true)
			{
				GenerateWorkingPossibleApparelSetFor(pawn, randomInRange, tmpApparelCandidates);
				if (DebugViewSettings.logApparelGeneration)
				{
					debugSb.Append(num2.ToString().PadRight(5) + "Trying: " + workingSet.ToString());
				}
				if (num2 < 10 && Rand.Value < 0.85f && randomInRange < 9999999f)
				{
					float num3 = Rand.Range(0.45f, 0.8f);
					float totalPrice = workingSet.TotalPrice;
					if (totalPrice < randomInRange * num3)
					{
						if (DebugViewSettings.logApparelGeneration)
						{
							debugSb.AppendLine(" -- Failed: Spent $" + totalPrice.ToString("F0") + ", < " + (num3 * 100f).ToString("F0") + "% of money.");
						}
						goto IL_045c;
					}
				}
				if (num2 < 20 && Rand.Value < 0.97f && !workingSet.Covers(BodyPartGroupDefOf.Torso))
				{
					if (DebugViewSettings.logApparelGeneration)
					{
						debugSb.AppendLine(" -- Failed: Does not cover torso.");
					}
				}
				else if (num2 < 30 && Rand.Value < 0.8f && workingSet.CoatButNoShirt())
				{
					if (DebugViewSettings.logApparelGeneration)
					{
						debugSb.AppendLine(" -- Failed: Coat but no shirt.");
					}
				}
				else
				{
					if (num2 < 50)
					{
						bool mustBeSafe = num2 < 17;
						if (!workingSet.SatisfiesNeededWarmth(neededWarmth, mustBeSafe, mapTemperature))
						{
							if (DebugViewSettings.logApparelGeneration)
							{
								debugSb.AppendLine(" -- Failed: Wrong warmth.");
							}
							goto IL_045c;
						}
					}
					if (ModsConfig.BiotechActive && num2 < 10 && !workingSet.SatisfiesNeededToxicEnvironmentResistance(num))
					{
						if (DebugViewSettings.logApparelGeneration)
						{
							debugSb.AppendLine(" -- Failed: Wrong toxic environment resistance.");
						}
					}
					else
					{
						if (num2 >= 80 || !workingSet.IsNaked(pawn.gender))
						{
							break;
						}
						if (DebugViewSettings.logApparelGeneration)
						{
							debugSb.AppendLine(" -- Failed: Naked.");
						}
					}
				}
				goto IL_045c;
				IL_045c:
				num2++;
			}
			if (DebugViewSettings.logApparelGeneration)
			{
				debugSb.Append(" -- Approved! Total price: $" + workingSet.TotalPrice.ToString("F0") + ", TotalInsulationCold: " + workingSet.TotalInsulationCold);
			}
		}
		if ((!pawn.kindDef.apparelIgnoreSeasons || request.ForceAddFreeWarmLayerIfNeeded) && !workingSet.SatisfiesNeededWarmth(neededWarmth, mustBeSafe: true, mapTemperature))
		{
			workingSet.AddFreeWarmthAsNeeded(neededWarmth, mapTemperature, pawn);
		}
		if (ModsConfig.BiotechActive && !pawn.kindDef.apparelIgnorePollution && num > 0.05f && !workingSet.SatisfiesNeededToxicEnvironmentResistance(num))
		{
			workingSet.AddFreeToxicEnvironmentResistanceAsNeeded(num, delegate(ThingStuffPair pa)
			{
				if (!pa.thing.apparel.CorrectAgeForWearing(pawn))
				{
					return false;
				}
				if (pawn.kindDef.apparelIgnoreSeasons && !request.ForceAddFreeWarmLayerIfNeeded)
				{
					return true;
				}
				return (!(workingSet.GetReplacedInsulationCold(pa) > pa.InsulationCold)) ? true : false;
			});
		}
		if (DebugViewSettings.logApparelGeneration)
		{
			Log.Message(debugSb.ToString());
		}
		workingSet.GiveToPawn(pawn);
		workingSet.Reset(null, null);
		foreach (Apparel item in pawn.apparel.WornApparel)
		{
			PostProcessApparel(item, pawn);
			CompBiocodable compBiocodable = item.TryGetComp<CompBiocodable>();
			if (compBiocodable != null && !compBiocodable.Biocoded && Rand.Chance(request.BiocodeApparelChance))
			{
				compBiocodable.CodeFor(pawn);
			}
		}
	}

	public static void PostProcessApparel(Apparel apparel, Pawn pawn)
	{
		if (pawn.kindDef.apparelColor != Color.white)
		{
			apparel.SetColor(pawn.kindDef.apparelColor, reportFailure: false);
		}
		apparel.SetStyleDef(pawn.Ideo?.GetStyleFor(apparel.def));
		List<SpecificApparelRequirement> specificApparelRequirements = pawn.kindDef.specificApparelRequirements;
		if (specificApparelRequirements == null)
		{
			return;
		}
		for (int i = 0; i < specificApparelRequirements.Count; i++)
		{
			if (ApparelRequirementHandlesThing(specificApparelRequirements[i], apparel.def))
			{
				Color color = specificApparelRequirements[i].GetColor();
				if (color != default(Color))
				{
					apparel.SetColor(color, reportFailure: false);
				}
				if (specificApparelRequirements[i].StyleDef != null)
				{
					apparel.SetStyleDef(specificApparelRequirements[i].StyleDef);
				}
				if (specificApparelRequirements[i].Locked)
				{
					pawn.apparel.Lock(apparel);
				}
				if (specificApparelRequirements[i].Biocode)
				{
					apparel.TryGetComp<CompBiocodable>()?.CodeFor(pawn);
				}
			}
		}
	}

	public static Apparel GenerateApparelOfDefFor(Pawn pawn, ThingDef apparelDef)
	{
		if (!allApparelPairs.Where((ThingStuffPair pa) => pa.thing == apparelDef && CanUseStuff(pawn, pa)).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality, out var result) && !allApparelPairs.Where((ThingStuffPair pa) => pa.thing == apparelDef).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality, out result))
		{
			return null;
		}
		return (Apparel)ThingMaker.MakeThing(result.thing, result.stuff);
	}

	private static void GenerateWorkingPossibleApparelSetFor(Pawn pawn, float money, List<ThingStuffPair> apparelCandidates)
	{
		workingSet.Reset(pawn);
		float num = money;
		List<SpecificApparelRequirement> att = pawn.kindDef.specificApparelRequirements;
		if (att != null)
		{
			int j;
			for (j = 0; j < att.Count; j++)
			{
				if ((!att[j].RequiredTag.NullOrEmpty() || !att[j].AlternateTagChoices.NullOrEmpty()) && allApparelPairs.Where((ThingStuffPair pa) => ApparelRequirementTagsMatch(att[j], pa.thing) && ApparelRequirementHandlesThing(att[j], pa.thing) && CanUseStuff(pawn, pa) && pa.thing.apparel.PawnCanWear(pawn) && !workingSet.PairOverlapsAnything(pa)).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality, out var result))
				{
					workingSet.Add(result);
					num -= result.Price;
				}
			}
		}
		List<ThingDef> reqApparel = pawn.kindDef.apparelRequired;
		if (reqApparel != null)
		{
			int i;
			for (i = 0; i < reqApparel.Count; i++)
			{
				ThingStuffPair result2;
				if (!reqApparel[i].apparel.CorrectAgeForWearing(pawn))
				{
					Log.Warning($"Required apparel {reqApparel[i]} not wearable by pawn at development stage: {pawn.DevelopmentalStage}");
				}
				else if (allApparelPairs.Where((ThingStuffPair pa) => pa.thing == reqApparel[i] && CanUseStuff(pawn, pa) && !workingSet.PairOverlapsAnything(pa)).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality, out result2))
				{
					workingSet.Add(result2);
					num -= result2.Price;
				}
			}
		}
		usableApparel.Clear();
		for (int k = 0; k < apparelCandidates.Count; k++)
		{
			if (!workingSet.PairOverlapsAnything(apparelCandidates[k]))
			{
				usableApparel.Add(apparelCandidates[k]);
			}
		}
		ThingStuffPair result3;
		while ((pawn.Ideo == null || !pawn.Ideo.IdeoPrefersNudityForGender(pawn.gender) || (pawn.Faction != null && pawn.Faction.IsPlayer)) && (pawn.IsColonist || pawn.story?.traits == null || !pawn.story.traits.HasTrait(TraitDefOf.Nudist)) && (!(Rand.Value < 0.1f) || !(money < 9999999f)) && usableApparel.Where((ThingStuffPair pa) => CanUseStuff(pawn, pa)).TryRandomElementByWeight((ThingStuffPair pa) => pa.Commonality, out result3))
		{
			workingSet.Add(result3);
			num -= result3.Price;
			for (int num2 = usableApparel.Count - 1; num2 >= 0; num2--)
			{
				if (usableApparel[num2].Price > num || workingSet.PairOverlapsAnything(usableApparel[num2]))
				{
					usableApparel.RemoveAt(num2);
				}
			}
		}
	}

	private static bool CanUseStuff(Pawn pawn, ThingStuffPair pair)
	{
		List<SpecificApparelRequirement> specificApparelRequirements = pawn.kindDef.specificApparelRequirements;
		if (specificApparelRequirements != null)
		{
			for (int i = 0; i < specificApparelRequirements.Count; i++)
			{
				if (!ApparelRequirementCanUseStuff(specificApparelRequirements[i], pair))
				{
					return false;
				}
			}
		}
		if (pair.stuff != null && pawn.Faction != null && !pawn.kindDef.ignoreFactionApparelStuffRequirements && !pawn.Faction.def.CanUseStuffForApparel(pair.stuff))
		{
			return false;
		}
		return true;
	}

	public static bool IsDerpApparel(ThingDef thing, ThingDef stuff)
	{
		if (stuff == null)
		{
			return false;
		}
		if (!thing.IsApparel)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < thing.stuffCategories.Count; i++)
		{
			if (thing.stuffCategories[i] != StuffCategoryDefOf.Woody && thing.stuffCategories[i] != StuffCategoryDefOf.Stony)
			{
				flag = true;
				break;
			}
		}
		if (flag && (stuff.stuffProps.categories.Contains(StuffCategoryDefOf.Woody) || stuff.stuffProps.categories.Contains(StuffCategoryDefOf.Stony)) && (thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) || thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs)))
		{
			return true;
		}
		return false;
	}

	public static bool ApparelRequirementHandlesThing(SpecificApparelRequirement req, ThingDef thing)
	{
		if (req.BodyPartGroup != null && !thing.apparel.bodyPartGroups.Contains(req.BodyPartGroup))
		{
			return false;
		}
		if (req.ApparelLayer != null && !thing.apparel.layers.Contains(req.ApparelLayer))
		{
			return false;
		}
		if (req.ApparelDef != null && thing != req.ApparelDef)
		{
			return false;
		}
		return true;
	}

	public static bool ApparelRequirementTagsMatch(SpecificApparelRequirement req, ThingDef thing)
	{
		if (!req.RequiredTag.NullOrEmpty() && thing.apparel.tags.Contains(req.RequiredTag))
		{
			return true;
		}
		if (!req.AlternateTagChoices.NullOrEmpty())
		{
			return req.AlternateTagChoices.Where((SpecificApparelRequirement.TagChance x) => thing.apparel.tags.Contains(x.tag) && Rand.Value < x.chance).Any();
		}
		return false;
	}

	private static bool ApparelRequirementCanUseStuff(SpecificApparelRequirement req, ThingStuffPair pair)
	{
		if (req.Stuff == null)
		{
			return true;
		}
		if (!ApparelRequirementHandlesThing(req, pair.thing))
		{
			return true;
		}
		if (pair.stuff != null)
		{
			return req.Stuff == pair.stuff;
		}
		return false;
	}

	private static bool CanUsePair(ThingStuffPair pair, Pawn pawn, float moneyLeft, bool allowHeadgear, int fixedSeed)
	{
		if (pair.Price > moneyLeft)
		{
			return false;
		}
		if (!allowHeadgear && IsHeadgear(pair.thing))
		{
			return false;
		}
		if (!pair.thing.apparel.PawnCanWear(pawn))
		{
			return false;
		}
		if (!pawn.kindDef.apparelTags.NullOrEmpty())
		{
			bool flag = false;
			for (int i = 0; i < pawn.kindDef.apparelTags.Count; i++)
			{
				for (int j = 0; j < pair.thing.apparel.tags.Count; j++)
				{
					if (pawn.kindDef.apparelTags[i] == pair.thing.apparel.tags[j])
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		if (!pawn.kindDef.apparelDisallowTags.NullOrEmpty())
		{
			for (int k = 0; k < pawn.kindDef.apparelDisallowTags.Count; k++)
			{
				if (pair.thing.apparel.tags.Contains(pawn.kindDef.apparelDisallowTags[k]))
				{
					return false;
				}
			}
		}
		if (pair.thing.generateAllowChance < 1f && !Rand.ChanceSeeded(pair.thing.generateAllowChance, fixedSeed ^ pair.thing.shortHash ^ 0x3D28557))
		{
			return false;
		}
		return true;
	}

	public static bool IsHeadgear(ThingDef td)
	{
		if (!td.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
		{
			return td.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead);
		}
		return true;
	}

	private static NeededWarmth ApparelWarmthNeededNow(Pawn pawn, PawnGenerationRequest request, out float mapTemperature)
	{
		int tile = request.Tile;
		if (tile == -1)
		{
			Map anyPlayerHomeMap = Find.AnyPlayerHomeMap;
			if (anyPlayerHomeMap != null)
			{
				tile = anyPlayerHomeMap.Tile;
			}
		}
		if (tile == -1)
		{
			mapTemperature = 21f;
			return NeededWarmth.Any;
		}
		NeededWarmth neededWarmth = NeededWarmth.Any;
		Twelfth twelfth = GenLocalDate.Twelfth(tile);
		mapTemperature = GenTemperature.AverageTemperatureAtTileForTwelfth(tile, twelfth);
		for (int i = 0; i < 2; i++)
		{
			NeededWarmth neededWarmth2 = CalculateNeededWarmth(pawn, tile, twelfth);
			if (neededWarmth2 != 0)
			{
				neededWarmth = neededWarmth2;
				break;
			}
			twelfth = twelfth.NextTwelfth();
		}
		if (pawn.kindDef.apparelIgnoreSeasons)
		{
			if (request.ForceAddFreeWarmLayerIfNeeded && neededWarmth == NeededWarmth.Warm)
			{
				return neededWarmth;
			}
			return NeededWarmth.Any;
		}
		return neededWarmth;
	}

	public static NeededWarmth CalculateNeededWarmth(Pawn pawn, int tile, Twelfth twelfth)
	{
		float num = GenTemperature.AverageTemperatureAtTileForTwelfth(tile, twelfth);
		if (num < pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin) - 4f)
		{
			return NeededWarmth.Warm;
		}
		if (num > pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin) + 4f)
		{
			return NeededWarmth.Cool;
		}
		return NeededWarmth.Any;
	}

	private static float ApparelToxicEnvironmentToAddress(Pawn pawn, PawnGenerationRequest request)
	{
		if (pawn.kindDef.apparelIgnorePollution)
		{
			return 0f;
		}
		int tile = request.Tile;
		if (tile == -1)
		{
			Map anyPlayerHomeMap = Find.AnyPlayerHomeMap;
			if (anyPlayerHomeMap != null)
			{
				tile = anyPlayerHomeMap.Tile;
			}
		}
		if (tile == -1)
		{
			return 0f;
		}
		return Mathf.Clamp01(Find.WorldGrid[tile].pollution);
	}

	[DebugOutput]
	private static void ApparelPairs()
	{
		DebugTables.MakeTablesDialog(allApparelPairs.OrderByDescending((ThingStuffPair p) => p.thing.defName), new TableDataGetter<ThingStuffPair>("thing", (ThingStuffPair p) => p.thing.defName), new TableDataGetter<ThingStuffPair>("stuff", (ThingStuffPair p) => (p.stuff == null) ? "" : p.stuff.defName), new TableDataGetter<ThingStuffPair>("price", (ThingStuffPair p) => p.Price.ToString()), new TableDataGetter<ThingStuffPair>("commonality", (ThingStuffPair p) => (p.Commonality * 100f).ToString("F4")), new TableDataGetter<ThingStuffPair>("generateCommonality", (ThingStuffPair p) => p.thing.generateCommonality.ToString("F4")), new TableDataGetter<ThingStuffPair>("insulationCold", (ThingStuffPair p) => (p.InsulationCold != 0f) ? p.InsulationCold.ToString() : ""), new TableDataGetter<ThingStuffPair>("headgear", (ThingStuffPair p) => (!IsHeadgear(p.thing)) ? "" : "*"), new TableDataGetter<ThingStuffPair>("derp", (ThingStuffPair p) => (!IsDerpApparel(p.thing, p.stuff)) ? "" : "D"));
	}

	[DebugOutput]
	private static void ApparelPairsByThing()
	{
		DebugOutputsGeneral.MakeTablePairsByThing(allApparelPairs);
	}
}
