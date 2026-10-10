using NUnit.Framework;
using UVMBinding.CodeGen;

namespace UVMBinding.EditorTests
{
	public class AssemblyNameMatchTests
	{
		[Test]
		public void 末尾が重なる参照が先にあっても完全一致を返す()
		{
			var references = new[]
			{
				@"Library\Bee\artifacts\dag\UIControl.UVMBinding.dll",
				@"Library\Bee\artifacts\dag\UVMBinding.dll",
			};

			Assert.AreEqual(references[1], AssemblyNameMatch.Find(references, "UVMBinding"));
		}

		[Test]
		public void 末尾が重なる参照が後にあっても完全一致を返す()
		{
			var references = new[]
			{
				@"Library\Bee\artifacts\dag\UVMBinding.dll",
				@"Library\Bee\artifacts\dag\UIControl.UVMBinding.dll",
			};

			Assert.AreEqual(references[0], AssemblyNameMatch.Find(references, "UVMBinding"));
		}

		[Test]
		public void 末尾が重なる参照だけならnullを返す()
		{
			var references = new[]
			{
				@"Library\Bee\artifacts\dag\UIControl.UVMBinding.dll",
			};

			Assert.IsNull(AssemblyNameMatch.Find(references, "UVMBinding"));
		}

		[Test]
		public void スラッシュ区切りの参照でも完全一致を返す()
		{
			var references = new[]
			{
				"Library/Bee/artifacts/dag/UIControl.UVMBinding.dll",
				"Library/Bee/artifacts/dag/UVMBinding.dll",
			};

			Assert.AreEqual(references[1], AssemblyNameMatch.Find(references, "UVMBinding"));
		}
	}
}
