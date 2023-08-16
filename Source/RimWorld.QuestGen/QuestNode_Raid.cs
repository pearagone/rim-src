using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace RimWorld.QuestGen;

public class QuestNode_Raid : QuestNode
{
	[NoTranslate]
	public SlateRef<string> inSignal;

	public SlateRef<IntVec3?> walkInSpot;

	public SlateRef<IntVec3?> dropSpot;

	public SlateRef<string> customLetterLabel;

	public SlateRef<string> customLetterText;

	public SlateRef<RulePack> customLetterLabelRules;

	public SlateRef<RulePack> customLetterTextRules;

	public SlateRef<PawnsArrivalModeDef> arrivalMode;

	public SlateRef<PawnKindDef> raidPawnKind;

	public SlateRef<bool?> canTimeoutOrFlee;

	[NoTranslate]
	public SlateRef<string> inSignalLeave;

	[NoTranslate]
	public SlateRef<string> tag;

	private const string RootSymbol = "root";

	protected override bool TestRunInt(Slate slate)
	{
		if (!Find.Storyteller.difficulty.allowViolentQuests)
		{
			return false;
		}
		if (!slate.Exists("map"))
		{
			return false;
		}
		if (!slate.Exists("enemyFaction"))
		{
			return false;
		}
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		Map target = QuestGen.slate.Get<Map>("map");
		float a = QuestGen.slate.Get("points", 0f);
		Faction faction = QuestGen.slate.Get<Faction>("enemyFaction");
		QuestPart_Incident questPart_Incident = new QuestPart_Incident();
		questPart_Incident.debugLabel = "raid";
		questPart_Incident.incident = IncidentDefOf.RaidEnemy;
		IncidentParms incidentParms = new IncidentParms();
		incidentParms.forced = true;
		incidentParms.target = target;
		incidentParms.points = Mathf.Max(a, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));
		incidentParms.faction = faction;
		incidentParms.pawnGroupMakerSeed = Rand.Int;
		incidentParms.inSignalEnd = QuestGenUtility.HardcodedSignalWithQuestID(inSignalLeave.GetValue(slate));
		incidentParms.questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(tag.GetValue(slate));
		incidentParms.canTimeoutOrFlee = canTimeoutOrFlee.GetValue(slate) ?? true;
		if (raidPawnKind.GetValue(slate) != null)
		{
			incidentParms.pawnKind = raidPawnKind.GetValue(slate);
			incidentParms.pawnCount = Mathf.Max(1, Mathf.RoundToInt(incidentParms.points / incidentParms.pawnKind.combatPower));
		}
		if (arrivalMode.GetValue(slate) != null)
		{
			incidentParms.raidArrivalMode = arrivalMode.GetValue(slate);
		}
		if (!customLetterLabel.GetValue(slate).NullOrEmpty() || customLetterLabelRules.GetValue(slate) != null)
		{
			QuestGen.AddTextRequest("root", delegate(string x)
			{
				incidentParms.customLetterLabel = x;
			}, QuestGenUtility.MergeRules(customLetterLabelRules.GetValue(slate), customLetterLabel.GetValue(slate), "root"));
		}
		if (!customLetterText.GetValue(slate).NullOrEmpty() || customLetterTextRules.GetValue(slate) != null)
		{
			QuestGen.AddTextRequest("root", delegate(string x)
			{
				incidentParms.customLetterText = x;
			}, QuestGenUtility.MergeRules(customLetterTextRules.GetValue(slate), customLetterText.GetValue(slate), "root"));
		}
		IncidentWorker_Raid obj = (IncidentWorker_Raid)questPart_Incident.incident.Worker;
		obj.ResolveRaidStrategy(incidentParms, PawnGroupKindDefOf.Combat);
		obj.ResolveRaidArriveMode(incidentParms);
		obj.ResolveRaidAgeRestriction(incidentParms);
		if (incidentParms.raidArrivalMode.walkIn)
		{
			incidentParms.spawnCenter = walkInSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("walkInSpot") ?? IntVec3.Invalid;
		}
		else
		{
			incidentParms.spawnCenter = dropSpot.GetValue(slate) ?? QuestGen.slate.Get<IntVec3?>("dropSpot") ?? IntVec3.Invalid;
		}
		PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, incidentParms);
		defaultPawnGroupMakerParms.points = IncidentWorker_Raid.AdjustedRaidPoints(defaultPawnGroupMakerParms.points, incidentParms.raidArrivalMode, incidentParms.raidStrategy, defaultPawnGroupMakerParms.faction, PawnGroupKindDefOf.Combat);
		IEnumerable<PawnKindDef> enumerable = PawnGroupMakerUtility.GeneratePawnKindsExample(defaultPawnGroupMakerParms);
		if (!enumerable.Any())
		{
			Log.Error("No pawnkinds example for " + QuestGen.quest.root.defName + " parms=" + defaultPawnGroupMakerParms);
		}
		questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
		questPart_Incident.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal");
		QuestGen.quest.AddPart(questPart_Incident);
		QuestGen.AddQuestDescriptionRules(new List<Rule>
		{
			new Rule_String("raidPawnKinds", PawnUtility.PawnKindsToLineList(enumerable, "  - ", ColoredText.ThreatColor)),
			new Rule_String("raidArrivalModeInfo", incidentParms.raidArrivalMode.textWillArrive.Formatted(faction))
		});
	}
}
