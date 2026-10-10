namespace UVMBinding.Sample
{
	public class Enemy
	{
		public readonly string Name;
		public readonly int MaxHp;
		public readonly int Attack;
		public readonly int Agility;
		public readonly int Exp;
		public readonly int Gold;

		Enemy(string name, int maxHp, int attack, int agility, int exp, int gold)
		{
			Name = name;
			MaxHp = maxHp;
			Attack = attack;
			Agility = agility;
			Exp = exp;
			Gold = gold;
		}

		static readonly Enemy Dog = new Enemy("野犬", 12, 4, 6, 15, 10);
		static readonly Enemy Bandit = new Enemy("山賊", 22, 7, 5, 30, 25);
		static readonly Enemy Eater = new Enemy("岩喰い", 34, 11, 3, 55, 40);

		public static Enemy At(int distance)
		{
			if (distance >= 60) return Eater;
			if (distance >= 30) return Bandit;
			return Dog;
		}
	}

	public class Battle
	{
		public readonly Enemy Enemy;
		public int EnemyHp { get; private set; }
		public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;
		public string Log { get; private set; }
		public int GainedExp { get; private set; }
		public int GainedGold { get; private set; }

		readonly Journey m_Journey;
		bool m_Guarding;

		public Battle(Journey journey, Enemy enemy)
		{
			m_Journey = journey;
			Enemy = enemy;
			EnemyHp = enemy.MaxHp;
			Log = $"{enemy.Name} が現れた。";
		}

		public bool IsOngoing => Outcome == BattleOutcome.Ongoing;

		public bool HasInitiative => m_Journey.Agility >= Enemy.Agility;

		public void Attack()
		{
			if (!IsOngoing) return;
			m_Guarding = false;
			if (!HasInitiative && StrikeBack("先を取られた。")) return;

			int damage = Vary(m_Journey.Attack);
			EnemyHp = System.Math.Max(0, EnemyHp - damage);
			Log = $"{damage} の傷を負わせた。";
			if (EnemyHp == 0)
			{
				Win();
				return;
			}
			if (HasInitiative)
			{
				StrikeBack(null);
			}
		}

		public void Guard()
		{
			if (!IsOngoing) return;
			m_Guarding = true;
			Log = "身を守った。";
			StrikeBack(null);
		}

		public void UseHerb()
		{
			if (!IsOngoing || !m_Journey.UseHerb()) return;
			m_Guarding = false;
			Log = $"やくそうを使った。体力が {Item.HerbHeal} 戻った。";
			StrikeBack(null);
		}

		public void Flee()
		{
			if (!IsOngoing) return;
			m_Guarding = false;
			if (m_Journey.Roll(100) < m_Journey.FleeChance)
			{
				Outcome = BattleOutcome.Fled;
				Log = "逃げ切った。1 日を失った。";
				m_Journey.PassDays(1);
				return;
			}
			Log = "逃げ損なった。";
			StrikeBack(null);
		}

		bool StrikeBack(string prefix)
		{
			int damage = Vary(Enemy.Attack);
			if (m_Guarding)
			{
				damage /= 2;
			}
			m_Journey.Damage(damage);
			var text = $"{Enemy.Name} の攻撃。{damage} の傷を受けた。";
			Log = prefix != null ? prefix + text : Log + " " + text;
			if (m_Journey.Hp == 0)
			{
				Outcome = BattleOutcome.Lost;
				return true;
			}
			return false;
		}

		void Win()
		{
			Outcome = BattleOutcome.Won;
			GainedExp = Enemy.Exp;
			GainedGold = Enemy.Gold;
			m_Journey.GainExp(GainedExp);
			m_Journey.GainGold(GainedGold);
			Log = $"{Enemy.Name} を退けた。経験 {GainedExp} / 戦利品 {GainedGold} G。";
		}

		int Vary(int value)
		{
			int span = System.Math.Max(1, value / 5);
			int delta = m_Journey.Roll(span * 2 + 1) - span;
			return System.Math.Max(1, value + delta);
		}
	}

	public enum BattleOutcome
	{
		Ongoing,
		Won,
		Lost,
		Fled,
	}
}
