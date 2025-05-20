namespace KtaneDefuserConnectorTests.Components;
public class MazeTests {
	[Fact]
	public void Sample1_LightsOn() {
		var image = Image.Load<Rgba32>(Resources.Maze1);
		var result = new Maze().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(new(1, 0), new(4, 5), new(3, 2), new(3, 0), new(2, 3)), result);
	}

	[Fact]
	public void Sample2_LightsEmergency() {
		var image = Image.Load<Rgba32>(Resources.Maze2);
		var result = new Maze().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(new(0, 1), new(4, 0), new(1, 4), new(3, 3), new(5, 3)), result);
	}

	[Fact]
	public void Sample3_LightsBuzz() {
		var image = Image.Load<Rgba32>(Resources.Maze3);
		var result = new Maze().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(new(1, 2), new(5, 3), new(0, 0), new(0, 0), new(0, 3)), result);
	}

	[Fact]
	public void Sample4_LightsOff() {
		var image = Image.Load<Rgba32>(Resources.Maze4);
		var result = new Maze().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(null, new(4, 1), new(4, 5), new(2, 1), new(0, 4)), result);
	}
}
