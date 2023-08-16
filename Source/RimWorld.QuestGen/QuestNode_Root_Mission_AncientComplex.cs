using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace RimWorld.QuestGen;

public class QuestNode_Root_Mission_AncientComplex : QuestNode_Root_AncientComplex
{
	private const int MinTilesFromColony = 5;

	private const int MaxTilesFromColony = 85;

	private FloatRange TimeLimitDays = new FloatRange(2f, 5f);

	private static IntRange HackDefenceRange = new IntRange(300, 1800);

	private const float MinMaxHackDefenceChance = 0.5f;

	private static SimpleCurve MinFreeColonistsOverPointsCurve = new SimpleCurve
	{
		new CurvePoint(0f, 3f),
		new CurvePoint(500f, 3f),
		new CurvePoint(10000f, 8f)
	};

	private static SimpleCurve ThreatPointsFactorOverColonyWealthCurve = new SimpleCurve
	{
		new CurvePoint(10000f, 0.5f),
		new CurvePoint(100000f, 1f),
		new CurvePoint(1000000f, 1.5f)
	};

	private static SimpleCurve ThreatPointsFactorOverPawnCountCurve = new SimpleCurve
	{
		new CurvePoint(1f, 0.5f),
		new CurvePoint(2f, 0.55f),
		new CurvePoint(5f, 0.75f),
		new CurvePoint(20f, 5f)
	};

	private static readonly FloatRange RandomRaidPointsFactorRange = new FloatRange(0.45f, 0.55f);

	private const string RootSymbol = "root";

	private const float ChanceToSpawnAllTerminalsHackedRaid = 0.5f;

	protected override void RunInt()
	{
		if (ModLister.CheckRoyaltyAndIdeology("Ancient Complex mission"))
		{
			Slate slate = QuestGen.slate;
			Quest quest = QuestGen.quest;
			float points = slate.Get("points", 0f);
			string questTagToAdd = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("AncientComplexMission");
			Precept_Relic precept_Relic = slate.Get<Precept_Relic>("relic");
			if (precept_Relic == null)
			{
				precept_Relic = Faction.OfPlayer.ideos.PrimaryIdeo.GetAllPreceptsOfType<Precept_Relic>().RandomElement();
				Log.Warning("Ancient Complex quest requires relic from parent quest. None found so picking random player relic");
			}
			TryFindEnemyFaction(out var enemyFaction);
			TryFindAsker(enemyFaction, out var pawn);
			TileFinder.TryFindNewSiteTile(out var tile, 5, 85, allowCaravans: false, TileFinderMode.Furthest);
			TryFindColonyMapWithFreeColonists(out var map, slate, points);
			int population = (slate.Exists("population") ? slate.Get("population", 0) : map.mapPawns.FreeColonists.Where(QuestNode_Root_Mission.PawnCanFight).Count());
			TryGetRequiredColonistCount(map, points, population, out var requiredColonistCount);
			string text = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.SentSatisfied");
			string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("askerFaction.BecameHostileToPlayer");
			string inSignal2 = QuestGenUtility.HardcodedSignalWithQuestID("terminals.Destroyed");
			string text2 = QuestGenUtility.HardcodedSignalWithQuestID("site.Destroyed");
			string text3 = QuestGen.GenerateNewSignal("TerminalHacked");
			string text4 = QuestGen.GenerateNewSignal("AllTerminalsHacked");
			QuestGen.GenerateNewSignal("RaidArrives");
			string text5 = QuestGen.GenerateNewSignal("MissionSuccess");
			string text6 = QuestGen.GenerateNewSignal("RequiredThingsLoaded");
			string text7 = QuestGen.GenerateNewSignal("SendShuttleAway");
			string text8 = QuestGen.GenerateNewSignal("EmptyShuttle");
			string text9 = QuestGen.GenerateNewSignal("CheckShuttleContents");
			string text10 = QuestGen.GenerateNewSignal("ShipThingAdded");
			QuestGen.GenerateNewSignal("PawnsLeftMap");
			ComplexSketch complexSketch = GenerateSketch(points);
			complexSketch.thingDiscoveredMessage = "MessageAncientTerminalDiscovered".Translate(precept_Relic.Label);
			List<string> list = new List<string>();
			for (int i = 0; i < complexSketch.thingsToSpawn.Count; i++)
			{
				Thing thing = complexSketch.thingsToSpawn[i];
				string text11 = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("terminal" + i);
				QuestUtility.AddQuestTag(thing, text11);
				string item = QuestGenUtility.HardcodedSignalWithQuestID(text11 + ".Hacked");
				list.Add(item);
				thing.TryGetComp<CompHackable>().defence = (Rand.Chance(0.5f) ? HackDefenceRange.min : HackDefenceRange.max);
			}
			float num = (Find.Storyteller.difficulty.allowViolentQuests ? GetThreatPoints(map.wealthWatcher.WealthTotal, points, requiredColonistCount) : 0f);
			SitePartParams parms = new SitePartParams
			{
				ancientComplexSketch = complexSketch,
				threatPoints = num,
				ancientComplexRewardMaker = ThingSetMakerDefOf.MapGen_AncientComplexRoomLoot_Default
			};
			SitePartDefWithParams val = new SitePartDefWithParams(SitePartDefOf.AncientComplex, parms);
			Site site = QuestGen_Sites.GenerateSite(Gen.YieldSingle(val), tile, Faction.OfAncients);
			TimedDetectionRaids component = site.GetComponent<TimedDetectionRaids>();
			if (component != null)
			{
				component.alertRaidsArrivingIn = true;
			}
			QuestPart_PassAllActivable questPart_PassAllActivable = new QuestPart_PassAllActivable();
			questPart_PassAllActivable.inSignalEnable = QuestGen.slate.Get<string>("inSignal");
			questPart_PassAllActivable.inSignals = list;
			questPart_PassAllActivable.outSignalsCompleted.Add(text4);
			questPart_PassAllActivable.outSignalAny = text3;
			questPart_PassAllActivable.expiryInfoPartKey = "TerminalsHacked";
			quest.AddPart(questPart_PassAllActivable);
			quest.Message("[terminalHackedMessage]", null, getLookTargetsFromSignal: true, null, null, text3);
			if (Find.Storyteller.difficulty.allowViolentQuests && Rand.Chance(0.5f))
			{
				quest.RandomRaid(site, RandomRaidPointsFactorRange * num, enemyFaction, text4, PawnsArrivalModeDefOf.EdgeWalkIn, RaidStrategyDefOf.ImmediateAttack);
			}
			Thing shuttle = QuestGen_Shuttle.GenerateShuttle(null, null, null, acceptColonists: true, onlyAcceptColonists: false, onlyAcceptHealthy: false, requiredColonistCount, dropEverythingIfUnsatisfied: true, leaveImmediatelyWhenSatisfied: false, dropEverythingOnArrival: false, stayAfterDroppedEverythingOnArrival: true, site, map.Parent, requiredColonistCount, null, permitShuttle: false, hideControls: false, allowSlaves: false, requireAllColonistsOnMap: false, acceptColonyPrisoners: true);
			slate.Set("shuttle", shuttle);
			QuestUtility.AddQuestTag(ref shuttle.questTags, questTagToAdd);
			quest.Message("MessageAncientComplexBackToShuttle".Translate(precept_Relic.Label), MessageTypeDefOf.PositiveEvent, getLookTargetsFromSignal: false, null, new LookTargets(shuttle), text4);
			TransportShip transportShip = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, null, shuttle).transportShip;
			slate.Set("transportShip", transportShip);
			QuestUtility.AddQuestTag(ref transportShip.questTags, questTagToAdd);
			quest.SendTransportShipAwayOnCleanup(transportShip, unloadContents: true, TransportShipDropMode.None);
			QuestPart_PassWhileActive questPart_PassWhileActive = new QuestPart_PassWhileActive();
			questPart_PassWhileActive.inSignalEnable = slate.Get<string>("inSignal");
			questPart_PassWhileActive.inSignal = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.ThingAdded");
			questPart_PassWhileActive.outSignal = text10;
			questPart_PassWhileActive.inSignalDisable = text7;
			quest.AddPart(questPart_PassWhileActive);
			QuestPart_Filter_AllRequiredThingsLoaded questPart_Filter_AllRequiredThingsLoaded = new QuestPart_Filter_AllRequiredThingsLoaded();
			questPart_Filter_AllRequiredThingsLoaded.inSignal = text10;
			questPart_Filter_AllRequiredThingsLoaded.shuttle = shuttle;
			questPart_Filter_AllRequiredThingsLoaded.outSignal = text6;
			quest.AddPart(questPart_Filter_AllRequiredThingsLoaded);
			QuestPart_Filter_AnyOnTransporterCapableOfHacking questPart_Filter_AnyOnTransporterCapableOfHacking = new QuestPart_Filter_AnyOnTransporterCapableOfHacking();
			questPart_Filter_AnyOnTransporterCapableOfHacking.transporter = shuttle;
			questPart_Filter_AnyOnTransporterCapableOfHacking.inSignal = text6;
			questPart_Filter_AnyOnTransporterCapableOfHacking.outSignal = text7;
			questPart_Filter_AnyOnTransporterCapableOfHacking.outSignalElse = text9;
			quest.AddPart(questPart_Filter_AnyOnTransporterCapableOfHacking);
			QuestPart_Dialog.Option option = new QuestPart_Dialog.Option();
			option.text = "SendShuttleAnyway".Translate();
			option.outSignal = text7;
			QuestPart_Dialog.Option option2 = new QuestPart_Dialog.Option();
			option2.text = "ReLoadShuttle".Translate();
			option2.outSignal = text8;
			QuestPart_Dialog dialog = new QuestPart_Dialog();
			dialog.title = "[passengersIncapableOfHackingDialogLabel]";
			dialog.text = "[passengersIncapableOfHackingDialogText]";
			dialog.options.Add(option);
			dialog.options.Add(option2);
			dialog.inSignal = text9;
			QuestGen.AddTextRequest("root", delegate(string x)
			{
				dialog.title = x;
			}, QuestGenUtility.MergeRules(null, dialog.title, "root"));
			QuestGen.AddTextRequest("root", delegate(string x)
			{
				dialog.text = x;
			}, QuestGenUtility.MergeRules(null, dialog.text, "root"));
			quest.AddPart(dialog);
			quest.AddShipJob_Arrive(transportShip, map.Parent, null, null, ShipJobStartMode.Instant, Faction.OfEmpire);
			quest.AddShipJob(transportShip, ShipJobDefOf.Unload, ShipJobStartMode.Force, text8);
			quest.Signal(text7, delegate
			{
				quest.SpawnWorldObject(site);
				quest.TendPawns(null, shuttle);
				quest.AddShipJob_WaitSendable_NewTemp(transportShip, site, leaveImmeiatelyWhenSatisfied: true);
				quest.AddShipJob(transportShip, ShipJobDefOf.Unload);
				quest.AddShipJob_WaitSendable_NewTemp(transportShip, map.Parent, leaveImmeiatelyWhenSatisfied: true, targetPlayerSettlment: true);
				quest.AddShipJob(transportShip, ShipJobDefOf.Unload);
				quest.AddShipJob_FlyAway(transportShip, -1, null, TransportShipDropMode.None);
			});
			quest.RequiredShuttleThings(shuttle, site, QuestGenUtility.HardcodedSignalWithQuestID("transportShip.FlewAway"), requireAllColonistsOnMap: true);
			quest.ShuttleLeaveDelay(shuttle, 60000, null, Gen.YieldSingle(text), null, delegate
			{
				quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true);
			});
			string inSignal3 = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Destroyed");
			quest.FactionGoodwillChange(pawn.Faction, 0, inSignal3, canSendMessage: true, canSendHostilityLetter: true, getLookTargetFromSignal: true, HistoryEventDefOf.ShuttleDestroyed, QuestPart.SignalListenMode.OngoingOnly, ensureMakesHostile: true);
			quest.End(QuestEndOutcome.Fail, 0, null, inSignal3, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true);
			quest.SignalPass(delegate
			{
				quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true);
			}, inSignal);
			quest.FeedPawns(null, shuttle, text);
			quest.TendPawns(null, shuttle, text);
			Reward_RelicInfo reward_RelicInfo = new Reward_RelicInfo();
			reward_RelicInfo.relic = precept_Relic;
			reward_RelicInfo.quest = quest;
			QuestPart_Choice questPart_Choice = quest.RewardChoice();
			QuestPart_Choice.Choice item2 = new QuestPart_Choice.Choice
			{
				rewards = { (Reward)reward_RelicInfo }
			};
			questPart_Choice.choices.Add(item2);
			int num2 = (int)(TimeLimitDays.RandomInRange * 60000f);
			slate.Set("timeoutTicks", num2);
			quest.WorldObjectTimeout(site, num2);
			QuestPart_Filter_Hacked questPart_Filter_Hacked = new QuestPart_Filter_Hacked();
			questPart_Filter_Hacked.inSignal = inSignal2;
			questPart_Filter_Hacked.outSignalElse = QuestGen.GenerateNewSignal("FailQuestTerminalDestroyed");
			quest.AddPart(questPart_Filter_Hacked);
			quest.End(QuestEndOutcome.Fail, 0, null, questPart_Filter_Hacked.outSignalElse, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true);
			Quest quest2 = quest;
			Action action = delegate
			{
				quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true);
			};
			string inSignalDisable = text4;
			quest2.SignalPassActivable(action, null, text2, null, null, inSignalDisable);
			QuestPart_PassAll questPart_PassAll = new QuestPart_PassAll();
			questPart_PassAll.inSignals.Add(text4);
			questPart_PassAll.inSignals.Add(text2);
			questPart_PassAll.outSignal = text5;
			quest.AddPart(questPart_PassAll);
			quest.End(QuestEndOutcome.Success, 0, null, text5, QuestPart.SignalListenMode.OngoingOnly, sendStandardLetter: true);
			QuestPart_PawnKilled questPart_PawnKilled = new QuestPart_PawnKilled();
			questPart_PawnKilled.mapParent = site;
			questPart_PawnKilled.faction = Faction.OfPlayer;
			questPart_PawnKilled.outSignal = QuestGen.GenerateNewSignal("PawnKilled");
			quest.AddPart(questPart_PawnKilled);
			quest.SignalPassActivable(delegate
			{
				QuestPart_Filter_AnyColonistCapableOfHacking questPart_Filter_AnyColonistCapableOfHacking = new QuestPart_Filter_AnyColonistCapableOfHacking
				{
					mapParent = site,
					inSignal = slate.Get<string>("inSignal"),
					outSignalElse = QuestGen.GenerateNewSignal("NoHackerLetter")
				};
				quest.AddPart(questPart_Filter_AnyColonistCapableOfHacking);
				quest.Letter(LetterDefOf.NegativeEvent, questPart_Filter_AnyColonistCapableOfHacking.outSignalElse, null, null, null, useColonistsFromCaravanArg: false, QuestPart.SignalListenMode.OngoingOnly, null, filterDeadPawnsFromLookTargets: false, label: "LetterLabelLastHackerLost".Translate(), text: "LetterTextLastHackerLost".Translate());
			}, null, questPart_PawnKilled.outSignal);
			slate.Set("relic", precept_Relic);
			slate.Set("terminals", complexSketch.thingsToSpawn);
			slate.Set("terminalCount", complexSketch.thingsToSpawn.Count);
			slate.Set("asker", pawn);
			slate.Set("enemyFaction", enemyFaction);
			slate.Set("colonistCount", requiredColonistCount);
			slate.Set("site", site);
		}
	}

	private static float GetThreatPoints(float colonyWealth, float points, int pawnCount)
	{
		return QuestNode_Root_AncientComplex.ThreatPointsOverPointsCurve.Evaluate(points) * ThreatPointsFactorOverColonyWealthCurve.Evaluate(colonyWealth) * ThreatPointsFactorOverPawnCountCurve.Evaluate(pawnCount);
	}

	private bool TryFindEnemyFaction(out Faction enemyFaction)
	{
		if (Find.FactionManager.AllFactionsVisible.Where((Faction f) => f.HostileTo(Faction.OfPlayer)).TryRandomElement(out var result))
		{
			enemyFaction = result;
			return true;
		}
		enemyFaction = null;
		return false;
	}

	private bool TryFindAsker(Faction enemyFaction, out Pawn pawn)
	{
		IEnumerable<Faction> source = Find.FactionManager.AllFactionsVisible.Where((Faction f) => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer) && f.HostileTo(enemyFaction) && f.leader != null);
		if (source.Where((Faction f) => (int)f.def.techLevel >= 4).TryRandomElement(out var result))
		{
			pawn = result.leader;
			return true;
		}
		if (source.TryRandomElement(out var result2))
		{
			pawn = result2.leader;
			return true;
		}
		pawn = null;
		return false;
	}

	private bool TryFindColonyMapWithFreeColonists(out Map map, Slate slate, float points)
	{
		int value = (int)MinFreeColonistsOverPointsCurve.Evaluate(points);
		map = QuestGen_Get.GetMap(mustBeInfestable: false, value);
		return map != null;
	}

	private bool TryFindSiteTile(out int tile)
	{
		return TileFinder.TryFindNewSiteTile(out tile, 5, 85);
	}

	private bool TryGetRequiredColonistCount(Map map, float points, int population, out int requiredColonistCount)
	{
		requiredColonistCount = -1;
		int num = (int)MinFreeColonistsOverPointsCurve.Evaluate(points);
		if (population <= num)
		{
			return false;
		}
		requiredColonistCount = Rand.Range(num, population);
		return true;
	}

	protected override bool TestRunInt(Slate slate)
	{
		if (!ModsConfig.RoyaltyActive)
		{
			return false;
		}
		float points = slate.Get("points", 0f);
		if (!TryFindEnemyFaction(out var enemyFaction) || !TryFindAsker(enemyFaction, out var _))
		{
			return false;
		}
		if (!TryFindColonyMapWithFreeColonists(out var map, slate, points))
		{
			return false;
		}
		if (!TileFinder.TryFindNewSiteTile(out var tile, 5, 85, allowCaravans: false, TileFinderMode.Near, -1, exitOnFirstTileFound: true))
		{
			return false;
		}
		int population = (slate.Exists("population") ? slate.Get("population", 0) : map.mapPawns.FreeColonists.Where(QuestNode_Root_Mission.PawnCanFight).Count());
		return TryGetRequiredColonistCount(map, points, population, out tile);
	}

	[DebugOutput("Quests", false)]
	public static void MissionAncientComplex()
	{
		List<Tuple<float, int, float>> list = new List<Tuple<float, int, float>>();
		int[] array = new int[4] { 1, 5, 10, 20 };
		float[] array2 = new float[3] { 10000f, 100000f, 1000000f };
		foreach (float item3 in DebugActionsUtility.PointsOptions(extended: false))
		{
			int[] array3 = array;
			foreach (int item in array3)
			{
				float[] array4 = array2;
				foreach (float item2 in array4)
				{
					list.Add(new Tuple<float, int, float>(item3, item, item2));
				}
			}
		}
		DebugTables.MakeTablesDialog(list, new TableDataGetter<Tuple<float, int, float>>("points", (Tuple<float, int, float> x) => x.Item1), new TableDataGetter<Tuple<float, int, float>>("wealth", (Tuple<float, int, float> x) => x.Item3), new TableDataGetter<Tuple<float, int, float>>("wealth threat factor", (Tuple<float, int, float> x) => $"x{ThreatPointsFactorOverColonyWealthCurve.Evaluate(x.Item3)}"), new TableDataGetter<Tuple<float, int, float>>("colonists", (Tuple<float, int, float> x) => x.Item2), new TableDataGetter<Tuple<float, int, float>>("colonist threat factor", (Tuple<float, int, float> x) => $"x{ThreatPointsFactorOverPawnCountCurve.Evaluate(x.Item2)}"), new TableDataGetter<Tuple<float, int, float>>("threat points", (Tuple<float, int, float> x) => GetThreatPoints(x.Item3, x.Item1, x.Item2)));
	}
}
