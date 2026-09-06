using System.Collections.Generic;

namespace UVMBinding.Sample
{
	public class Route
	{
		public readonly RouteKind Kind;
		public readonly string Label;
		public readonly int Distance;
		public readonly int Days;
		public readonly int EncounterRate;
		public readonly int RequiredHp;
		public readonly bool RequiresTorch;

		Route(RouteKind kind, string label, int distance, int days, int encounterRate,
			int requiredHp = 0, bool requiresTorch = false)
		{
			Kind = kind;
			Label = label;
			Distance = distance;
			Days = days;
			EncounterRate = encounterRate;
			RequiredHp = requiredHp;
			RequiresTorch = requiresTorch;
		}

		public string BlockReason(Journey journey)
		{
			if (RequiresTorch && !journey.HasTorch)
			{
				return "松明がない";
			}
			if (RequiredHp > 0 && journey.Hp < RequiredHp)
			{
				return $"体力が {RequiredHp} 必要";
			}
			return string.Empty;
		}

		public bool CanChoose(Journey journey) => BlockReason(journey).Length == 0;

		public string Cost => $"{Distance} 里 / {Days} 日";

		public string CostWithRumor(Journey journey)
		{
			return $"{Distance} 里 / {Days} 日 / 遭遇 {journey.EncounterRateOf(this)}%";
		}

		static readonly Route Road = new Route(RouteKind.Road, "街道をゆく", 12, 2, 15);
		static readonly Route Byway = new Route(RouteKind.Byway, "間道を抜ける", 20, 2, 35);
		static readonly Route Pass = new Route(RouteKind.Pass, "峠を越える", 30, 3, 45, requiredHp: 15);
		static readonly Route Cave = new Route(RouteKind.Cave, "洞窟を抜ける", 26, 1, 50, requiresTorch: true);
		static readonly Route River = new Route(RouteKind.River, "川沿いを行く", 15, 2, 25);

		static readonly Route[] s_Third = { Pass, Cave, River };

		public static void Choices(int distance, List<Route> list)
		{
			list.Clear();
			list.Add(Road);
			list.Add(Byway);
			list.Add(s_Third[(distance / 10) % s_Third.Length]);
		}
	}

	public enum RouteKind
	{
		Road,
		Byway,
		Pass,
		Cave,
		River,
	}
}
