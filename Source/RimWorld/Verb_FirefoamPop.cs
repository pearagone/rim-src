using Verse;

namespace RimWorld;

public class Verb_FirefoamPop : Verb
{
	private CompExplosive Explosive => base.EquipmentSource.GetComp<CompExplosive>();

	private CompReloadable Reloadable => base.EquipmentSource.GetComp<CompReloadable>();

	private float Radius => Explosive.ExplosiveRadius();

	protected override bool TryCastShot()
	{
		return Pop(caster, Explosive, Reloadable);
	}

	public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
	{
		needLOSToCenter = true;
		return Radius;
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		DrawHighlightFieldRadiusAroundTarget(caster);
	}

	public static bool Pop(Thing caster, CompExplosive explosive, CompReloadable reloadable)
	{
		if (reloadable == null || explosive == null || !reloadable.CanBeUsed)
		{
			return false;
		}
		explosive.StartWick(caster);
		reloadable.UsedOnce();
		return true;
	}
}
