using System.Collections.Generic;
using UnityEngine;

namespace UVMBinding.Sample
{
	public class SampleGame : MonoBehaviour
	{
		[SerializeField] View m_TitleView = default;
		[SerializeField] View m_FieldView = default;
		[SerializeField] View m_BattleView = default;
		[SerializeField] View m_ShopView = default;
		[SerializeField] View m_TavernView = default;
		[SerializeField] View m_GrowthView = default;
		[SerializeField] View m_EndingView = default;
		[SerializeField] SampleArt m_Art = new SampleArt();
		[SerializeField] int m_Seed = 0;

		Journey m_Journey;
		Battle m_Battle;

		TitleViewModel m_Title;
		FieldViewModel m_Field;
		BattleViewModel m_BattleVm;
		ShopViewModel m_Shop;
		TavernViewModel m_Tavern;
		GrowthViewModel m_Growth;
		EndingViewModel m_Ending;

		readonly List<Route> m_Routes = new List<Route>();
		RouteKind m_LastRoute = RouteKind.Road;
		bool m_RumorBought;
		int m_PendingStrength, m_PendingAgility, m_PendingVitality;
		View m_GrowthReturn;

		void Start()
		{
			BindTitle();
			BindField();
			BindBattle();
			BindShop();
			BindTavern();
			BindGrowth();
			BindEnding();
			ShowTitle();
		}

		void ShowTitle()
		{
			RefreshTitle();
			Show(m_TitleView);
		}

		void BeginJourney(Difficulty difficulty)
		{
			m_Journey = new Journey(difficulty, m_Seed != 0 ? m_Seed : System.Environment.TickCount);
			m_Battle = null;
			m_RumorBought = false;
			m_LastRoute = RouteKind.Road;
			ShowField();
		}

		void Show(View view)
		{
			m_TitleView.gameObject.SetActive(view == m_TitleView);
			m_FieldView.gameObject.SetActive(view == m_FieldView);
			m_BattleView.gameObject.SetActive(view == m_BattleView);
			m_ShopView.gameObject.SetActive(view == m_ShopView);
			m_TavernView.gameObject.SetActive(view == m_TavernView);
			m_GrowthView.gameObject.SetActive(view == m_GrowthView);
			m_EndingView.gameObject.SetActive(view == m_EndingView);
		}

		bool GoToEndingIfOver()
		{
			if (!m_Journey.IsOver)
			{
				return false;
			}
			RefreshEnding();
			Show(m_EndingView);
			return true;
		}

		void ShowField()
		{
			if (GoToEndingIfOver()) return;
			RefreshField();
			Show(m_FieldView);
		}

		void BindTitle()
		{
			m_Title = m_TitleView.Attach<TitleViewModel>();
		}

		void RefreshTitle()
		{
			m_Title.Scenery = m_Art.Gate;
			m_Title.Title = "王都への道";
			m_Title.Prologue = "王都まで 100 里。門が閉まる前にたどり着けるか。";
			m_Title.Difficulties.Clear();
			for (int i = 0; i < DifficultyRule.All.Count; i++)
			{
				var rule = DifficultyRule.All[i];
				var row = new DifficultyViewModel
				{
					Label = rule.Label,
					Summary = rule.Summary,
					Terms = $"期限 {rule.Deadline} 日 / 遭遇 {rule.EncounterScale}%",
				};
				row.OnChooseDifficulty = ChooseDifficulty;
				m_Title.Difficulties.Add(row);
			}
		}

		void ChooseDifficulty(int index)
		{
			if (index < 0 || index >= DifficultyRule.All.Count) return;
			BeginJourney(DifficultyRule.All[index].Level);
		}

		void BindField()
		{
			m_Field = m_FieldView.Attach<FieldViewModel>(vm =>
			{
				vm.OnCamp = () =>
				{
					m_Journey.Camp();
					ShowField();
				};
				vm.OnUseHerb = () =>
				{
					m_Journey.UseHerb();
					RefreshField();
				};
				vm.OnOpenGrowth = () => OpenGrowth(m_FieldView);
			});
		}

		void RefreshField()
		{
			var j = m_Journey;
			m_Field.Scenery = m_Art.Scenery(m_LastRoute);
			m_Field.Description = Describe(m_LastRoute);
			m_Field.Day = $"{j.Day} 日目";
			m_Field.Hp = $"体力 {j.Hp} / {j.MaxHp}";
			m_Field.Gold = $"{j.Gold} G";
			m_Field.Food = $"食料 {j.Food}";
			m_Field.RemainingDistance = $"残り {j.RemainingDistance} 里";
			m_Field.RemainingDays = $"残り {j.RemainingDays} 日";
			m_Field.DifficultyLabel = j.Rule.Label;
			m_Field.HerbCount = $"やくそう {j.HerbCount}";
			m_Field.CanUseHerb = j.HerbCount > 0 && j.Hp < j.MaxHp;
			m_Field.CanCamp = j.Food > 0;
			m_Field.CampBlockReason = j.Food > 0 ? string.Empty : "食料がない";
			m_Field.CanGrow = j.UnspentPoints > 0;

			Route.Choices(j.Distance, m_Routes);
			m_Field.Routes.Clear();
			for (int i = 0; i < m_Routes.Count; i++)
			{
				var route = m_Routes[i];
				var reason = route.BlockReason(j);
				var row = new RouteViewModel
				{
					Label = route.Label,
					Cost = m_RumorBought ? route.CostWithRumor(j) : route.Cost,
					BlockReason = reason,
					CanChoose = reason.Length == 0,
				};
				row.OnChooseRoute = ChooseRoute;
				m_Field.Routes.Add(row);
			}
		}

		void ChooseRoute(int index)
		{
			if (index < 0 || index >= m_Routes.Count) return;
			var route = m_Routes[index];
			if (!route.CanChoose(m_Journey)) return;

			m_LastRoute = route.Kind;
			m_RumorBought = false;
			switch (m_Journey.Advance(route))
			{
				case LegOutcome.Encounter:
					StartBattle();
					break;
				case LegOutcome.Town:
					ShowShop();
					break;
				default:
					ShowField();
					break;
			}
		}

		static string Describe(RouteKind kind)
		{
			switch (kind)
			{
				case RouteKind.Pass: return "峠を越えた。眼下に道が続いている。";
				case RouteKind.Cave: return "洞窟を抜けた。外の光がまぶしい。";
				case RouteKind.River: return "川沿いを歩いてきた。水音が遠ざかる。";
				default: return "道は三方に分かれている。どれを選ぶか。";
			}
		}

		void BindBattle()
		{
			m_BattleVm = m_BattleView.Attach<BattleViewModel>(vm =>
			{
				vm.OnAttack = () => { m_Battle?.Attack(); RefreshBattle(); };
				vm.OnGuard = () => { m_Battle?.Guard(); RefreshBattle(); };
				vm.OnUseHerb = () => { m_Battle?.UseHerb(); RefreshBattle(); };
				vm.OnFlee = () => { m_Battle?.Flee(); RefreshBattle(); };
				vm.OnLeaveBattle = LeaveBattle;
			});
		}

		void LeaveBattle()
		{
			if (GoToEndingIfOver()) return;
			if (m_Journey.AtTown)
			{
				ShowShop();
				return;
			}
			ShowField();
		}

		void StartBattle()
		{
			m_Battle = new Battle(m_Journey, Enemy.At(m_Journey.Distance));
			RefreshBattle();
			Show(m_BattleView);
		}

		void RefreshBattle()
		{
			if (m_Battle == null) return;
			var vm = m_BattleVm;
			vm.EnemyPortrait = m_Art.Portrait(m_Battle.Enemy);
			vm.EnemyName = m_Battle.Enemy.Name;
			vm.EnemyHp = $"体力 {m_Battle.EnemyHp} / {m_Battle.Enemy.MaxHp}";
			vm.Hp = $"体力 {m_Journey.Hp} / {m_Journey.MaxHp}";
			vm.BattleLog = m_Battle.Log;
			vm.HerbCount = $"やくそう {m_Journey.HerbCount}";
			vm.CanAct = m_Battle.IsOngoing;
			vm.CanUseHerb = m_Battle.IsOngoing && m_Journey.HerbCount > 0;
			vm.IsSettled = !m_Battle.IsOngoing;
			vm.Spoils = m_Battle.Outcome == BattleOutcome.Won
				? $"経験 {m_Battle.GainedExp} / 戦利品 {m_Battle.GainedGold} G"
				: string.Empty;
			if (m_Battle.Outcome == BattleOutcome.Lost)
			{
				GoToEndingIfOver();
			}
		}

		void BindShop()
		{
			m_Shop = m_ShopView.Attach<ShopViewModel>(vm =>
			{
				vm.OnOpenTavern = ShowTavern;
				vm.OnLeaveTown = LeaveTown;
			});
		}

		void LeaveTown()
		{
			m_Journey.LeaveTown();
			ShowField();
		}

		void ShowShop()
		{
			RefreshShop();
			Show(m_ShopView);
		}

		void RefreshShop()
		{
			m_Shop.Scenery = m_Art.Shop;
			m_Shop.Greeting = "「いらっしゃい。見ていっとくれ」";
			m_Shop.Gold = $"所持金 {m_Journey.Gold} G";
			m_Shop.Food = $"食料 {m_Journey.Food}";
			m_Shop.Items.Clear();
			for (int i = 0; i < Item.Catalog.Count; i++)
			{
				var item = Item.Catalog[i];
				var row = new ShopItemViewModel
				{
					Name = item.Name,
					Price = $"{item.Price} G",
					Effect = item.Effect,
					CanBuy = item.CanBuy(m_Journey),
					BlockReason = BuyBlockReason(item),
				};
				row.OnBuy = Buy;
				m_Shop.Items.Add(row);
			}
		}

		string BuyBlockReason(Item item)
		{
			if (item.IsSoldOut(m_Journey)) return "売り切れ";
			if (!m_Journey.CanAfford(item.Price)) return "所持金が足りない";
			return string.Empty;
		}

		void Buy(int index)
		{
			if (index < 0 || index >= Item.Catalog.Count) return;
			m_Journey.Buy(Item.Catalog[index]);
			RefreshShop();
		}

		void BindTavern()
		{
			m_Tavern = m_TavernView.Attach<TavernViewModel>(vm =>
			{
				vm.OnStayInn = () =>
				{
					if (!m_Journey.Spend(Tavern.InnPrice)) return;
					m_Journey.HealFully();
					m_Journey.PassDays(1);
					if (GoToEndingIfOver()) return;
					RefreshTavern();
				};
				vm.OnBuyRumor = () =>
				{
					if (!m_Journey.Spend(Tavern.RumorPrice)) return;
					m_RumorBought = true;
					RefreshTavern();
				};
				vm.OnBackToShop = ShowShop;
				vm.OnLeaveTown = LeaveTown;
			});
		}

		void ShowTavern()
		{
			RefreshTavern();
			Show(m_TavernView);
		}

		void RefreshTavern()
		{
			m_Tavern.Scenery = m_Art.Tavern;
			m_Tavern.Greeting = "「一杯やってくかい」";
			m_Tavern.Gold = $"所持金 {m_Journey.Gold} G";
			m_Tavern.Hp = $"体力 {m_Journey.Hp} / {m_Journey.MaxHp}";
			m_Tavern.InnPrice = $"宿を取る {Tavern.InnPrice} G";
			m_Tavern.RumorPrice = $"噂を聞く {Tavern.RumorPrice} G";
			m_Tavern.CanStayInn = m_Journey.CanAfford(Tavern.InnPrice) && m_Journey.Hp < m_Journey.MaxHp;
			m_Tavern.CanBuyRumor = m_Journey.CanAfford(Tavern.RumorPrice) && !m_RumorBought;
			m_Tavern.Rumor = m_RumorBought ? "「この先の道の危なさが分かるようになった」" : string.Empty;
		}

		void BindGrowth()
		{
			m_Growth = m_GrowthView.Attach<GrowthViewModel>(vm =>
			{
				vm.OnIncrease = index =>
				{
					if (Spent() >= m_Journey.UnspentPoints) return;
					if (index == 0) m_PendingStrength++;
					else if (index == 1) m_PendingAgility++;
					else m_PendingVitality++;
					RefreshGrowth();
				};
				vm.OnDecrease = index =>
				{
					if (index == 0 && m_PendingStrength > m_Journey.Strength) m_PendingStrength--;
					else if (index == 1 && m_PendingAgility > m_Journey.Agility) m_PendingAgility--;
					else if (index == 2 && m_PendingVitality > m_Journey.Vitality) m_PendingVitality--;
					RefreshGrowth();
				};
				vm.OnConfirm = () =>
				{
					m_Journey.ApplyGrowth(m_PendingStrength, m_PendingAgility, m_PendingVitality);
					CloseGrowth();
				};
				vm.OnCancelGrowth = CloseGrowth;
			});
		}

		int Spent()
		{
			return (m_PendingStrength - m_Journey.Strength)
				+ (m_PendingAgility - m_Journey.Agility)
				+ (m_PendingVitality - m_Journey.Vitality);
		}

		void OpenGrowth(View returnTo)
		{
			m_GrowthReturn = returnTo;
			m_PendingStrength = m_Journey.Strength;
			m_PendingAgility = m_Journey.Agility;
			m_PendingVitality = m_Journey.Vitality;
			RefreshGrowth();
			Show(m_GrowthView);
		}

		void CloseGrowth()
		{
			if (GoToEndingIfOver()) return;
			if (m_GrowthReturn == m_FieldView)
			{
				ShowField();
				return;
			}
			Show(m_GrowthReturn != null ? m_GrowthReturn : m_FieldView);
		}

		void RefreshGrowth()
		{
			var vm = m_Growth;
			int spent = Spent();
			vm.Level = $"Lv {m_Journey.Level}";
			vm.Exp = $"経験 {m_Journey.Exp} / {m_Journey.ExpToNext}";
			vm.UnspentPoints = $"残りポイント {m_Journey.UnspentPoints - spent}";
			vm.Strength = m_PendingStrength;
			vm.Agility = m_PendingAgility;
			vm.Vitality = m_PendingVitality;
			vm.WeaponAttack = m_Journey.WeaponAttack;
			vm.PendingAllocation = DescribePending();
			vm.CanIncrease = spent < m_Journey.UnspentPoints;
			vm.CanDecreaseStrength = m_PendingStrength > m_Journey.Strength;
			vm.CanDecreaseAgility = m_PendingAgility > m_Journey.Agility;
			vm.CanDecreaseVitality = m_PendingVitality > m_Journey.Vitality;
			vm.CanConfirm = spent > 0;
		}

		string DescribePending()
		{
			var text = string.Empty;
			text = AppendDelta(text, "腕力", m_PendingStrength - m_Journey.Strength);
			text = AppendDelta(text, "敏捷", m_PendingAgility - m_Journey.Agility);
			text = AppendDelta(text, "体力", m_PendingVitality - m_Journey.Vitality);
			return text.Length == 0 ? string.Empty : "未確定 " + text;
		}

		static string AppendDelta(string text, string label, int delta)
		{
			if (delta == 0)
			{
				return text;
			}
			var part = $"{label} +{delta}";
			return text.Length == 0 ? part : text + " / " + part;
		}

		void BindEnding()
		{
			m_Ending = m_EndingView.Attach<EndingViewModel>(vm => vm.OnRestart = ShowTitle);
		}

		void RefreshEnding()
		{
			bool reached = m_Journey.Result == JourneyResult.Reached;
			m_Ending.Scenery = reached ? m_Art.Gate : m_Art.Grave;
			m_Ending.Outcome = reached ? "王都に着いた" : "旅は終わった";
			m_Ending.Epitaph = Epitaph(m_Journey.Result);
			m_Ending.TravelledDistance = $"{m_Journey.Distance} 里";
			m_Ending.ElapsedDays = $"{m_Journey.Day} 日";
			m_Ending.Level = $"Lv {m_Journey.Level}";
			m_Ending.DifficultyLabel = m_Journey.Rule.Label;
		}

		static string Epitaph(JourneyResult result)
		{
			switch (result)
			{
				case JourneyResult.Reached: return "門は開いていた。";
				case JourneyResult.Collapsed: return "力尽きて道に倒れた。";
				case JourneyResult.OutOfTime: return "門は閉ざされていた。";
				default: return string.Empty;
			}
		}
	}
}
