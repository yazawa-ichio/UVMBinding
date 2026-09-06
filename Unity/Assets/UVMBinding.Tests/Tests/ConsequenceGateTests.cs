using NUnit.Framework;

namespace UVMBinding.Tests
{
	public class ConsequenceGateTests
	{
		[TestCase(ConsequenceKind.Unknown, false)]
		[TestCase(ConsequenceKind.Safe, true)]
		[TestCase(ConsequenceKind.Irreversible, false)]
		[TestCase(ConsequenceKind.Purchase, false)]
		[TestCase(ConsequenceKind.Destructive, false)]
		public void 自動で押してよいのは安全と宣言されたものだけ(ConsequenceKind kind, bool allowed)
		{
			Assert.AreEqual(allowed, ConsequenceGate.AllowsAutoPress(kind));
		}

		[Test]
		public void 既定は分かっていない側()
		{
			Assert.AreEqual(ConsequenceKind.Unknown, default(ConsequenceKind));
			Assert.IsFalse(ConsequenceGate.AllowsAutoPress(default));
		}

		[Test]
		public void 属性値は小文字になる()
		{
			Assert.AreEqual("purchase", ConsequenceGate.ToAttributeValue(ConsequenceKind.Purchase));
			Assert.AreEqual("unknown", ConsequenceGate.ToAttributeValue(ConsequenceKind.Unknown));
		}
	}
}
