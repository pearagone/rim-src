using Verse.AI;

namespace RimWorld;

[DefOf]
public static class DutyDefOf
{
	public static DutyDef TravelOrLeave;

	public static DutyDef TravelOrWait;

	public static DutyDef Kidnap;

	public static DutyDef Steal;

	public static DutyDef TakeWoundedGuest;

	public static DutyDef Follow;

	public static DutyDef PrisonerEscape;

	public static DutyDef PrisonerEscapeSapper;

	public static DutyDef DefendAndExpandHive;

	public static DutyDef DefendHiveAggressively;

	public static DutyDef LoadAndEnterTransporters;

	public static DutyDef EnterTransporter;

	public static DutyDef EnterTransporterAndDefendSelf;

	public static DutyDef ManClosestTurret;

	public static DutyDef SleepForever;

	public static DutyDef Idle;

	public static DutyDef IdleNoInteraction;

	public static DutyDef WanderClose;

	public static DutyDef AssaultColony;

	public static DutyDef Breaching;

	public static DutyDef Sapper;

	public static DutyDef Escort;

	public static DutyDef Defend;

	public static DutyDef Build;

	public static DutyDef HuntEnemiesIndividual;

	public static DutyDef DefendBase;

	[MayRequireRoyalty]
	public static DutyDef AssaultThing;

	public static DutyDef PrisonerAssaultColony;

	public static DutyDef ExitMapRandom;

	public static DutyDef ExitMapBest;

	public static DutyDef ExitMapBestAndDefendSelf;

	public static DutyDef ExitMapNearDutyTarget;

	public static DutyDef MarryPawn;

	public static DutyDef GiveSpeech;

	public static DutyDef Spectate;

	public static DutyDef SpectateSocial;

	public static DutyDef Party;

	public static DutyDef BestowingCeremony_MoveInPlace;

	[MayRequireRoyalty]
	public static DutyDef Bestow;

	[MayRequireIdeology]
	public static DutyDef Pilgrims_Spectate;

	[MayRequireIdeology]
	public static DutyDef PlayTargetInstrument;

	[MayRequireBiotech]
	public static DutyDef SocialMeeting;

	public static DutyDef PrepareCaravan_GatherItems;

	public static DutyDef PrepareCaravan_Wait;

	public static DutyDef PrepareCaravan_GatherAnimals;

	public static DutyDef PrepareCaravan_CollectAnimals;

	public static DutyDef PrepareCaravan_GatherDownedPawns;

	public static DutyDef PrepareCaravan_Pause;

	public static DutyDef ReturnedCaravan_PenAnimals;

	static DutyDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(DutyDefOf));
	}
}
