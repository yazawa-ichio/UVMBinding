using System.Collections.Generic;

namespace UVMBinding.Sample
{
	public class Item
	{
		public const int HerbHeal = 20;
		public const int DriedFood = 6;
		public const int SwordAttack = 12;

		public readonly ItemKind Kind;
		public readonly string Name;
		public readonly int Price;
		public readonly string Effect;

		Item(ItemKind kind, string name, int price, string effect)
		{
			Kind = kind;
			Name = name;
			Price = price;
			Effect = effect;
		}

		public bool IsSoldOut(Journey journey) => Kind == ItemKind.Sword && journey.WeaponAttack > 0;

		public bool CanBuy(Journey journey) => journey.CanAfford(Price) && !IsSoldOut(journey);

		public static IReadOnlyList<Item> Catalog { get; } = new[]
		{
			new Item(ItemKind.Herb, "やくそう", 8, $"体力を {HerbHeal} 回復する"),
			new Item(ItemKind.Dried, "干し肉", 15, $"食料が {DriedFood} 増える"),
			new Item(ItemKind.Torch, "松明", 25, "洞窟を抜けられるようになる"),
			new Item(ItemKind.Sword, "鉄の剣", 120, $"武器攻撃力 +{SwordAttack}"),
		};
	}

	public enum ItemKind
	{
		Herb,
		Dried,
		Torch,
		Sword,
	}

	public static class Tavern
	{
		public const int InnPrice = 20;
		public const int RumorPrice = 10;
	}
}
