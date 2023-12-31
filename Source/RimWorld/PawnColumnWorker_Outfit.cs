using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class PawnColumnWorker_Outfit : PawnColumnWorker
{
	public const int TopAreaHeight = 65;

	public const int ManageOutfitsButtonHeight = 32;

	public override void DoHeader(Rect rect, PawnTable table)
	{
		base.DoHeader(rect, table);
		MouseoverSounds.DoRegion(rect);
		Rect rect2 = new Rect(rect.x, rect.y + (rect.height - 65f), Mathf.Min(rect.width, 360f), 32f);
		if (Widgets.ButtonText(rect2, "ManageOutfits".Translate()))
		{
			Find.WindowStack.Add(new Dialog_ManageOutfits(null));
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Outfits, KnowledgeAmount.Total);
		}
		UIHighlighter.HighlightOpportunity(rect2, "ManageOutfits");
	}

	public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
	{
		if (pawn.outfits == null)
		{
			return;
		}
		int num = Mathf.FloorToInt((rect.width - 4f) * 0.71428573f);
		int num2 = Mathf.FloorToInt((rect.width - 4f) * 0.2857143f);
		float x = rect.x;
		bool somethingIsForced = pawn.outfits.forcedHandler.SomethingIsForced;
		Rect rect2 = new Rect(x, rect.y + 2f, num, rect.height - 4f);
		if (somethingIsForced)
		{
			rect2.width -= 4f + (float)num2;
		}
		if (pawn.IsQuestLodger())
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect2, "Unchangeable".Translate().Truncate(rect2.width));
			TooltipHandler.TipRegionByKey(rect2, "QuestRelated_Outfit");
			Text.Anchor = TextAnchor.UpperLeft;
		}
		else
		{
			Widgets.Dropdown(rect2, pawn, (Pawn p) => p.outfits.CurrentOutfit, Button_GenerateMenu, pawn.outfits.CurrentOutfit.label.Truncate(rect2.width), null, pawn.outfits.CurrentOutfit.label, null, null, paintable: true);
		}
		x += rect2.width;
		x += 4f;
		Rect rect3 = new Rect(x, rect.y + 2f, num2, rect.height - 4f);
		if (somethingIsForced)
		{
			if (Widgets.ButtonText(rect3, "ClearForcedApparel".Translate()))
			{
				pawn.outfits.forcedHandler.Reset();
			}
			if (Mouse.IsOver(rect3))
			{
				TooltipHandler.TipRegion(rect3, new TipSignal(delegate
				{
					string text = "ForcedApparel".Translate() + ":\n";
					foreach (Apparel item in pawn.outfits.forcedHandler.ForcedApparel)
					{
						text = text + "\n   " + item.LabelCap;
					}
					return text;
				}, pawn.GetHashCode() * 612));
			}
			x += (float)num2;
			x += 4f;
		}
		Rect rect4 = new Rect(x, rect.y + 2f, num2, rect.height - 4f);
		if (!pawn.IsQuestLodger() && Widgets.ButtonText(rect4, "AssignTabEdit".Translate()))
		{
			Find.WindowStack.Add(new Dialog_ManageOutfits(pawn.outfits.CurrentOutfit));
		}
		x += (float)num2;
	}

	private IEnumerable<Widgets.DropdownMenuElement<Outfit>> Button_GenerateMenu(Pawn pawn)
	{
		foreach (Outfit outfit in Current.Game.outfitDatabase.AllOutfits)
		{
			yield return new Widgets.DropdownMenuElement<Outfit>
			{
				option = new FloatMenuOption(outfit.label, delegate
				{
					pawn.outfits.CurrentOutfit = outfit;
				}),
				payload = outfit
			};
		}
	}

	public override int GetMinWidth(PawnTable table)
	{
		return Mathf.Max(base.GetMinWidth(table), Mathf.CeilToInt(194f));
	}

	public override int GetOptimalWidth(PawnTable table)
	{
		return Mathf.Clamp(Mathf.CeilToInt(251f), GetMinWidth(table), GetMaxWidth(table));
	}

	public override int GetMinHeaderHeight(PawnTable table)
	{
		return Mathf.Max(base.GetMinHeaderHeight(table), 65);
	}

	public override int Compare(Pawn a, Pawn b)
	{
		return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
	}

	private int GetValueToCompare(Pawn pawn)
	{
		if (pawn.outfits != null && pawn.outfits.CurrentOutfit != null)
		{
			return pawn.outfits.CurrentOutfit.uniqueId;
		}
		return int.MinValue;
	}
}
