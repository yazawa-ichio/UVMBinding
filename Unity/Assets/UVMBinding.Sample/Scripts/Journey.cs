using System;
using System.Collections.Generic;

namespace UVMBinding.Sample
{
	public class Journey
	{
		public const int GoalDistance = 100;
		public const int DeadlineDay = 30;
		public const int TownInterval = 30;
		public const int StartHp = 30;
		public const int StartGold = 72;
		public const int StartFood = 12;
		public const int StarveDamage = 3;
		public const int CampHeal = 8;

		readonly Random m_Random;

		public Journey(Difficulty difficulty = Difficulty.Normal, int seed = 0)
		{
			m_Random = new Random(seed);
			Rule = DifficultyRule.Of(difficulty);
		}

		public DifficultyRule Rule { get; }

		public int Deadline => Rule.Deadline;

		public int EncounterRateOf(Route route)
		{
			return route != null ? route.EncounterRate * Rule.EncounterScale / 100 : 0;
		}

		public int Distance { get; private set; }
		public int Day { get; private set; } = 1;
		public int Hp { get; private set; } = StartHp;
		public int Gold { get; private set; } = StartGold;
		public int Food { get; private set; } = StartFood;

		public int Level { get; private set; } = 1;
		public int Exp { get; private set; }
		public int UnspentPoints { get; private set; }
		public int Strength { get; private set; } = 5;
		public int Agility { get; private set; } = 4;
		public int Vitality { get; private set; } = 5;
		public int WeaponAttack { get; private set; }

		public JourneyResult Result { get; private set; } = JourneyResult.InProgress;

		int m_TownsVisited;

		readonly List<ItemKind> m_Bag = new List<ItemKind>();

		public IReadOnlyList<ItemKind> Bag => m_Bag;

		public int MaxHp => StartHp + (Vitality - 5) * 2;

		public int Attack => Strength * 2 + WeaponAttack;

		public int ExpToNext => Level * 50;

		public int FleeChance => 40 + Agility * 4;

		public bool HasTorch => m_Bag.Contains(ItemKind.Torch);

		public int HerbCount => m_Bag.FindAll(x => x == ItemKind.Herb).Count;

		public bool IsOver => Result != JourneyResult.InProgress;

		public int RemainingDistance => Math.Max(0, GoalDistance - Distance);

		public int RemainingDays => Math.Max(0, Deadline - Day);

		public LegOutcome Advance(Route route)
		{
			if (IsOver || !route.CanChoose(this))
			{
				return LegOutcome.None;
			}
			AtTown = false;
			Distance += route.Distance;
			PassDays(route.Days);
			if (IsOver)
			{
				return LegOutcome.None;
			}
			if (Distance >= GoalDistance)
			{
				Result = JourneyResult.Reached;
				return LegOutcome.None;
			}
			if (Distance >= (m_TownsVisited + 1) * TownInterval)
			{
				m_TownsVisited++;
				AtTown = true;
			}
			if (Roll(100) < EncounterRateOf(route))
			{
				return LegOutcome.Encounter;
			}
			return AtTown ? LegOutcome.Town : LegOutcome.None;
		}

		public bool AtTown { get; private set; }

		public void LeaveTown()
		{
			AtTown = false;
		}

		public bool Camp()
		{
			if (IsOver || Food <= 0)
			{
				return false;
			}
			Heal(CampHeal);
			PassDays(1);
			return true;
		}

		public void PassDays(int days)
		{
			for (int i = 0; i < days && !IsOver; i++)
			{
				Day++;
				if (Food > 0)
				{
					Food--;
				}
				else
				{
					Damage(StarveDamage);
				}
				if (Day > Deadline && !IsOver)
				{
					Result = JourneyResult.OutOfTime;
				}
			}
		}

		public void Heal(int amount)
		{
			Hp = Math.Min(MaxHp, Hp + amount);
		}

		public void Damage(int amount)
		{
			Hp = Math.Max(0, Hp - amount);
			if (Hp == 0)
			{
				Result = JourneyResult.Collapsed;
			}
		}

		public void HealFully()
		{
			Hp = MaxHp;
		}

		public bool CanAfford(int price) => Gold >= price;

		public bool Spend(int price)
		{
			if (!CanAfford(price))
			{
				return false;
			}
			Gold -= price;
			return true;
		}

		public void GainGold(int amount)
		{
			Gold += amount;
		}

		public void GainFood(int amount)
		{
			Food += amount;
		}

		public bool Buy(Item item)
		{
			if (IsOver || !Spend(item.Price))
			{
				return false;
			}
			switch (item.Kind)
			{
				case ItemKind.Dried:
					GainFood(Item.DriedFood);
					break;
				case ItemKind.Sword:
					WeaponAttack += Item.SwordAttack;
					break;
				default:
					m_Bag.Add(item.Kind);
					break;
			}
			return true;
		}

		public bool UseHerb()
		{
			if (IsOver || !m_Bag.Remove(ItemKind.Herb))
			{
				return false;
			}
			Heal(Item.HerbHeal);
			return true;
		}

		public void GainExp(int amount)
		{
			Exp += amount;
			while (Exp >= ExpToNext)
			{
				Exp -= ExpToNext;
				Level++;
				UnspentPoints += 2;
			}
		}

		public bool ApplyGrowth(int strength, int agility, int vitality)
		{
			int spent = (strength - Strength) + (agility - Agility) + (vitality - Vitality);
			if (spent < 0 || spent > UnspentPoints)
			{
				return false;
			}
			Strength = strength;
			Agility = agility;
			Vitality = vitality;
			UnspentPoints -= spent;
			Hp = Math.Min(Hp, MaxHp);
			return true;
		}

		public int Roll(int max) => m_Random.Next(max);
	}

	public enum JourneyResult
	{
		InProgress,
		Reached,
		Collapsed,
		OutOfTime,
	}

	public enum LegOutcome
	{
		None,
		Encounter,
		Town,
	}
}
