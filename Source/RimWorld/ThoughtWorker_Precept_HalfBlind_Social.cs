using Verse;

namespace RimWorld;

public class ThoughtWorker_Precept_HalfBlind_Social : ThoughtWorker_Precept_Social
{
	protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
	{
		return ThoughtWorker_Precept_HalfBlind.IsHalfBlind(otherPawn);
	}
}
