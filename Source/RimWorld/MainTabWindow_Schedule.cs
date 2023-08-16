using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public class MainTabWindow_Schedule : MainTabWindow_PawnTable
{
	private const int TimeAssignmentSelectorWidth = 191;

	private const int TimeAssignmentSelectorHeight = 65;

	protected override PawnTableDef PawnTableDef => PawnTableDefOf.Restrict;

	protected override IEnumerable<Pawn> Pawns => base.Pawns.Where((Pawn pawn) => !pawn.DevelopmentalStage.Baby());

	public override void DoWindowContents(Rect fillRect)
	{
		base.DoWindowContents(fillRect);
		TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid(new Rect(0f, 0f, 191f, 65f));
	}
}
