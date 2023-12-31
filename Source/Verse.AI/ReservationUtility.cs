using System.Collections.Generic;

namespace Verse.AI;

public static class ReservationUtility
{
	public static bool CanReserveSittableOrSpot(this Pawn pawn, IntVec3 exactSittingPos, bool ignoreOtherReservations = false)
	{
		Building edifice = exactSittingPos.GetEdifice(pawn.Map);
		if (exactSittingPos.Impassable(pawn.Map))
		{
			return false;
		}
		if (edifice == null || edifice.def.building.multiSittable)
		{
			return pawn.CanReserve(exactSittingPos, 1, -1, null, ignoreOtherReservations);
		}
		if (edifice != null && edifice.def.building.isSittable && edifice.def.hasInteractionCell && exactSittingPos != edifice.InteractionCell)
		{
			return false;
		}
		return pawn.CanReserve(edifice, 1, -1, null, ignoreOtherReservations);
	}

	public static bool ReserveSittableOrSpot(this Pawn pawn, IntVec3 exactSittingPos, Job job, bool errorOnFailed = true)
	{
		Building edifice = exactSittingPos.GetEdifice(pawn.Map);
		if (exactSittingPos.Impassable(pawn.Map))
		{
			Log.Error("Tried reserving impassable sittable or spot.");
			return false;
		}
		if (edifice == null || edifice.def.building.multiSittable)
		{
			return pawn.Reserve(exactSittingPos, job, 1, -1, null, errorOnFailed);
		}
		if (edifice != null && edifice.def.building.isSittable && edifice.def.hasInteractionCell && exactSittingPos != edifice.InteractionCell)
		{
			return false;
		}
		return pawn.Reserve(edifice, job, 1, -1, null, errorOnFailed);
	}

	public static bool CanReserve(this Pawn p, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
	{
		if (!p.Spawned)
		{
			return false;
		}
		return p.Map.reservationManager.CanReserve(p, target, maxPawns, stackCount, layer, ignoreOtherReservations);
	}

	public static bool CanReserveAndReach(this Pawn p, LocalTargetInfo target, PathEndMode peMode, Danger maxDanger, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
	{
		if (!p.Spawned)
		{
			return false;
		}
		if (p.CanReach(target, peMode, maxDanger))
		{
			return p.Map.reservationManager.CanReserve(p, target, maxPawns, stackCount, layer, ignoreOtherReservations);
		}
		return false;
	}

	public static bool Reserve(this Pawn p, LocalTargetInfo target, Job job, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true)
	{
		if (!p.Spawned)
		{
			return false;
		}
		return p.Map.reservationManager.Reserve(p, job, target, maxPawns, stackCount, layer, errorOnFailed);
	}

	public static void ReserveAsManyAsPossible(this Pawn p, List<LocalTargetInfo> target, Job job, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null)
	{
		if (p.Spawned)
		{
			for (int i = 0; i < target.Count; i++)
			{
				p.Map.reservationManager.Reserve(p, job, target[i], maxPawns, stackCount, layer, errorOnFailed: false);
			}
		}
	}

	public static bool HasReserved(this Pawn p, LocalTargetInfo target, Job job = null)
	{
		if (!p.Spawned)
		{
			return false;
		}
		return p.Map.reservationManager.ReservedBy(target, p, job);
	}

	public static bool HasReserved<TDriver>(this Pawn p, LocalTargetInfo target, LocalTargetInfo? targetAIsNot = null, LocalTargetInfo? targetBIsNot = null, LocalTargetInfo? targetCIsNot = null)
	{
		if (!p.Spawned)
		{
			return false;
		}
		return p.Map.reservationManager.ReservedBy<TDriver>(target, p, targetAIsNot, targetBIsNot, targetCIsNot);
	}

	public static bool CanReserveNew(this Pawn p, LocalTargetInfo target)
	{
		if (target.IsValid && !p.HasReserved(target))
		{
			return p.CanReserve(target);
		}
		return false;
	}
}
