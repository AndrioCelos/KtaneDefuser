using JetBrains.Annotations;

namespace KtaneDefuserConnectorTests;

internal static class Util {
	internal static Image<Rgba32>? DummyImage;

	[AssertionMethod]
	internal static void AssertList<T>(ICollection<T> expected, IEnumerable<T> actual) => Assert.Collection(actual, [.. from e in expected select (Action<T>) (a => Assert.Equal(e, a))]);
}
