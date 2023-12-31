using UnityEngine;
using Verse;

namespace RimWorld;

public class ITab_Pawn_Health : ITab
{
	private const int HideBloodLossTicksThreshold = 60000;

	public const float Width = 630f;

	private Pawn PawnForHealth
	{
		get
		{
			if (base.SelPawn != null)
			{
				return base.SelPawn;
			}
			if (base.SelThing is Corpse corpse)
			{
				return corpse.InnerPawn;
			}
			return null;
		}
	}

	public ITab_Pawn_Health()
	{
		size = new Vector2(630f, 430f);
		labelKey = "TabHealth";
		tutorTag = "Health";
	}

	protected override void FillTab()
	{
		Pawn pawnForHealth = PawnForHealth;
		if (pawnForHealth == null)
		{
			Log.Error("Health tab found no selected pawn to display.");
			return;
		}
		bool showBloodLoss = !(base.SelThing is Corpse corpse) || corpse.Age < 60000;
		HealthCardUtility.DrawPawnHealthCard(new Rect(Vector2.zero, size), pawnForHealth, ShouldAllowOperations(), showBloodLoss, base.SelThing);
	}

	private bool ShouldAllowOperations()
	{
		Pawn pawn = PawnForHealth;
		if (pawn.Dead)
		{
			return false;
		}
		if (!base.SelThing.def.AllRecipes.Any((RecipeDef x) => x.AvailableNow && x.AvailableOnNow(pawn)))
		{
			return false;
		}
		if (pawn.Faction == Faction.OfPlayer)
		{
			return true;
		}
		if (pawn.IsPrisonerOfColony || (pawn.HostFaction == Faction.OfPlayer && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving)))
		{
			return true;
		}
		if (pawn.RaceProps.IsFlesh && pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer))
		{
			return false;
		}
		if (!pawn.RaceProps.Humanlike && pawn.Downed)
		{
			return true;
		}
		return false;
	}
}
