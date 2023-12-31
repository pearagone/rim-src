using System.Collections.Generic;
using RimWorld;

namespace Verse.AI;

public class JobDriver_AttackStatic : JobDriver
{
	private bool startedIncapacitated;

	private int numAttacksMade;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref startedIncapacitated, "startedIncapacitated", defaultValue: false);
		Scribe_Values.Look(ref numAttacksMade, "numAttacksMade", 0);
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
		Toil init = ToilMaker.MakeToil("MakeNewToils");
		init.initAction = delegate
		{
			if (base.TargetThingA is Pawn pawn2)
			{
				startedIncapacitated = pawn2.Downed;
			}
			pawn.pather.StopDead();
		};
		init.tickAction = delegate
		{
			if (!base.TargetA.IsValid)
			{
				EndJobWith(JobCondition.Succeeded);
			}
			else
			{
				if (base.TargetA.HasThing)
				{
					Pawn pawn = base.TargetA.Thing as Pawn;
					if (base.TargetA.Thing.Destroyed || (pawn != null && !startedIncapacitated && pawn.Downed) || (pawn != null && pawn.IsInvisible()))
					{
						EndJobWith(JobCondition.Succeeded);
						return;
					}
				}
				if (numAttacksMade >= job.maxNumStaticAttacks && !base.pawn.stances.FullBodyBusy)
				{
					EndJobWith(JobCondition.Succeeded);
				}
				else if (base.pawn.TryStartAttack(base.TargetA))
				{
					numAttacksMade++;
				}
				else if (!base.pawn.stances.FullBodyBusy)
				{
					Verb verb = base.pawn.TryGetAttackVerb(base.TargetA.Thing, !base.pawn.IsColonist);
					if (job.endIfCantShootTargetFromCurPos && (verb == null || !verb.CanHitTargetFrom(base.pawn.Position, base.TargetA)))
					{
						EndJobWith(JobCondition.Incompletable);
					}
					else if (job.endIfCantShootInMelee)
					{
						if (verb == null)
						{
							EndJobWith(JobCondition.Incompletable);
						}
						else
						{
							float num = verb.verbProps.EffectiveMinRange(base.TargetA, base.pawn);
							if ((float)base.pawn.Position.DistanceToSquared(base.TargetA.Cell) < num * num && base.pawn.Position.AdjacentTo8WayOrInside(base.TargetA.Cell))
							{
								EndJobWith(JobCondition.Incompletable);
							}
						}
					}
				}
			}
		};
		init.defaultCompleteMode = ToilCompleteMode.Never;
		init.activeSkill = () => Toils_Combat.GetActiveSkillForToil(init);
		yield return init;
	}
}
