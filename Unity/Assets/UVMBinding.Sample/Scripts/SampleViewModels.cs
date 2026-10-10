using System;

namespace UVMBinding.Sample
{

	public class TitleViewModel : ViewModel
	{
		[Bind] public UnityEngine.Sprite Scenery { get; set; }
		[Bind] public string Title { get; set; }
		[Bind] public string Prologue { get; set; }
		[Bind] public Collection<DifficultyViewModel> Difficulties { get; private set; }
	}

	public class DifficultyViewModel : ViewModel
	{
		[Bind] public string Label { get; set; }
		[Bind] public string Summary { get; set; }
		[Bind] public string Terms { get; set; }

		[Event, Consequence(ConsequenceKind.Safe)] public Action<int> OnChooseDifficulty { get; set; }
	}

	public class FieldViewModel : ViewModel
	{
		[Bind] public UnityEngine.Sprite Scenery { get; set; }
		[Bind] public string Description { get; set; }
		[Bind] public string Day { get; set; }
		[Bind] public string Hp { get; set; }
		[Bind] public string Gold { get; set; }
		[Bind] public string Food { get; set; }
		[Bind] public string RemainingDistance { get; set; }
		[Bind] public string RemainingDays { get; set; }
		[Bind] public string DifficultyLabel { get; set; }

		[Bind] public Collection<RouteViewModel> Routes { get; private set; }

		[Bind] public string CampBlockReason { get; set; }
		[Bind] public bool CanCamp { get; set; }
		[Bind] public bool CanGrow { get; set; }
		[Bind] public string HerbCount { get; set; }
		[Bind] public bool CanUseHerb { get; set; }

		[Event, Consequence(ConsequenceKind.Irreversible)] public Action OnCamp { get; set; }
		[Event, Consequence(ConsequenceKind.Irreversible)] public Action OnUseHerb { get; set; }
		[Event, Consequence(ConsequenceKind.Safe)] public Action OnOpenGrowth { get; set; }
	}

	public class RouteViewModel : ViewModel
	{
		[Bind] public string Label { get; set; }
		[Bind] public string Cost { get; set; }
		[Bind] public string BlockReason { get; set; }
		[Bind] public bool CanChoose { get; set; }

		[Event, Consequence(ConsequenceKind.Irreversible)] public Action<int> OnChooseRoute { get; set; }
	}

	public class BattleViewModel : ViewModel
	{
		[Bind] public UnityEngine.Sprite EnemyPortrait { get; set; }
		[Bind] public string EnemyName { get; set; }
		[Bind] public string EnemyHp { get; set; }
		[Bind] public string Hp { get; set; }
		[Bind] public string BattleLog { get; set; }
		[Bind] public string HerbCount { get; set; }

		[Bind] public bool CanAct { get; set; }
		[Bind] public bool CanUseHerb { get; set; }
		[Bind] public bool IsSettled { get; set; }
		[Bind] public string Spoils { get; set; }

		[Event, Consequence(ConsequenceKind.Irreversible)] public Action OnAttack { get; set; }
		[Event, Consequence(ConsequenceKind.Irreversible)] public Action OnGuard { get; set; }
		[Event, Consequence(ConsequenceKind.Irreversible)] public Action OnUseHerb { get; set; }
		[Event, Consequence(ConsequenceKind.Irreversible)] public Action OnFlee { get; set; }
		[Event, Consequence(ConsequenceKind.Safe)] public Action OnLeaveBattle { get; set; }
	}

	public class ShopViewModel : ViewModel
	{
		[Bind] public UnityEngine.Sprite Scenery { get; set; }
		[Bind] public string Greeting { get; set; }
		[Bind] public string Gold { get; set; }
		[Bind] public string Food { get; set; }
		[Bind] public Collection<ShopItemViewModel> Items { get; private set; }

		[Event, Consequence(ConsequenceKind.Safe)] public Action OnOpenTavern { get; set; }
		[Event, Consequence(ConsequenceKind.Irreversible)] public Action OnLeaveTown { get; set; }
	}

	public class ShopItemViewModel : ViewModel
	{
		[Bind] public string Name { get; set; }
		[Bind] public string Price { get; set; }
		[Bind] public string Effect { get; set; }
		[Bind] public string BlockReason { get; set; }
		[Bind] public bool CanBuy { get; set; }

		[Event, Consequence(ConsequenceKind.Purchase)] public Action<int> OnBuy { get; set; }
	}

	public class TavernViewModel : ViewModel
	{
		[Bind] public UnityEngine.Sprite Scenery { get; set; }
		[Bind] public string Greeting { get; set; }
		[Bind] public string Gold { get; set; }
		[Bind] public string Hp { get; set; }
		[Bind] public string InnPrice { get; set; }
		[Bind] public string RumorPrice { get; set; }
		[Bind] public string Rumor { get; set; }
		[Bind] public bool CanStayInn { get; set; }
		[Bind] public bool CanBuyRumor { get; set; }

		[Event, Consequence(ConsequenceKind.Purchase)] public Action OnStayInn { get; set; }
		[Event, Consequence(ConsequenceKind.Purchase)] public Action OnBuyRumor { get; set; }
		[Event, Consequence(ConsequenceKind.Safe)] public Action OnBackToShop { get; set; }
		[Event, Consequence(ConsequenceKind.Irreversible)] public Action OnLeaveTown { get; set; }
	}

	public class GrowthViewModel : ViewModel
	{
		[Bind] public string Level { get; set; }
		[Bind] public string Exp { get; set; }
		[Bind] public string UnspentPoints { get; set; }

		[Bind] public int Strength { get; set; }
		[Bind] public int Agility { get; set; }
		[Bind] public int Vitality { get; set; }
		[Bind] public int WeaponAttack { get; set; }

		[Bind, DependsOn(nameof(Strength), nameof(WeaponAttack))]
		public string Attack => $"攻撃力 {Strength * 2 + WeaponAttack}";

		[Bind, DependsOn(nameof(Vitality))]
		public string MaxHp => $"最大体力 {Journey.StartHp + (Vitality - 5) * 2}";

		[Bind, DependsOn(nameof(Agility))]
		public string FleeChance => $"逃走 {40 + Agility * 4}%";

		[Bind] public string PendingAllocation { get; set; }

		[Bind] public bool CanIncrease { get; set; }
		[Bind] public bool CanDecreaseStrength { get; set; }
		[Bind] public bool CanDecreaseAgility { get; set; }
		[Bind] public bool CanDecreaseVitality { get; set; }
		[Bind] public bool CanConfirm { get; set; }

		[Event, Consequence(ConsequenceKind.Safe)] public Action<int> OnIncrease { get; set; }
		[Event, Consequence(ConsequenceKind.Safe)] public Action<int> OnDecrease { get; set; }
		[Event, Consequence(ConsequenceKind.Irreversible)] public Action OnConfirm { get; set; }
		[Event, Consequence(ConsequenceKind.Safe)] public Action OnCancelGrowth { get; set; }
	}

	public class EndingViewModel : ViewModel
	{
		[Bind] public UnityEngine.Sprite Scenery { get; set; }
		[Bind] public string Outcome { get; set; }
		[Bind] public string Epitaph { get; set; }
		[Bind] public string TravelledDistance { get; set; }
		[Bind] public string ElapsedDays { get; set; }
		[Bind] public string Level { get; set; }

		[Bind] public string DifficultyLabel { get; set; }

		[Event, Consequence(ConsequenceKind.Safe)] public Action OnRestart { get; set; }
	}
}
