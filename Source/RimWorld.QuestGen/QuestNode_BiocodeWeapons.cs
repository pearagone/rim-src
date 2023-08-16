using System.Collections.Generic;
using Verse;

namespace RimWorld.QuestGen;

public class QuestNode_BiocodeWeapons : QuestNode
{
	[NoTranslate]
	public SlateRef<string> inSignal;

	public SlateRef<IEnumerable<Pawn>> pawns;

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		if (pawns.GetValue(slate) != null)
		{
			QuestPart_BiocodeWeapons questPart_BiocodeWeapons = new QuestPart_BiocodeWeapons();
			questPart_BiocodeWeapons.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal");
			questPart_BiocodeWeapons.pawns.AddRange(pawns.GetValue(slate));
			QuestGen.quest.AddPart(questPart_BiocodeWeapons);
		}
	}
}
