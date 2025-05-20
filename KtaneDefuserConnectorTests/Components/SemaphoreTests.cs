using Semaphore = KtaneDefuserConnector.Components.Semaphore;

namespace KtaneDefuserConnectorTests.Components;
public class SemaphoreTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.Semaphore1);
		var result = new Semaphore().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(1, 0), Semaphore.Direction.Down, Semaphore.Direction.DownRight), result);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.Semaphore2);
		var result = new Semaphore().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(new(0, 0), Semaphore.Direction.Up, Semaphore.Direction.Right), result);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.Semaphore3);
		var result = new Semaphore().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(new(2, 0), Semaphore.Direction.DownLeft, Semaphore.Direction.UpRight), result);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.Semaphore4);
		var result = new Semaphore().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(null, Semaphore.Direction.Left, Semaphore.Direction.DownLeft), result);
	}
}
