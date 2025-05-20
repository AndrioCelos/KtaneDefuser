namespace KtaneDefuserConnectorTests.Components;
public class WireSequenceTests {
	[Fact]
	public void Sample1() {
		var image = Image.Load<Rgba32>(Resources.WireSequence1);
		var result = new WireSequence().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(0, 0), result.Selection);
		Assert.Equal(0, result.StagesCleared);
		Assert.Equal(1, result.CurrentPageFirstWireNum);
		AssertList([WireSequence.WireColour.Red, WireSequence.WireColour.Blue, WireSequence.WireColour.Blue], result.WireColours);
		Assert.Null(result.HighlightedWire);
	}

	[Fact]
	public void Sample2() {
		var image = Image.Load<Rgba32>(Resources.WireSequence2);
		var result = new WireSequence().Process(image, LightsState.On, ref DummyImage);
		Assert.Equal(new(0, 3), result.Selection);
		Assert.Equal(2, result.StagesCleared);
		Assert.Equal(7, result.CurrentPageFirstWireNum);
		AssertList([WireSequence.WireColour.Blue, WireSequence.WireColour.Red, WireSequence.WireColour.Black], result.WireColours);
		Assert.Equal(new(2, 'A'), result.HighlightedWire);
	}

	[Fact]
	public void Sample3() {
		var image = Image.Load<Rgba32>(Resources.WireSequence3);
		var result = new WireSequence().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Equal(new(0, 0), result.Selection);
		Assert.Equal(1, result.StagesCleared);
		Assert.Equal(4, result.CurrentPageFirstWireNum);
		AssertList([null, null, null], result.WireColours);
		Assert.Null(result.HighlightedWire);
	}

	[Fact]
	public void Sample4() {
		var image = Image.Load<Rgba32>(Resources.WireSequence4);
		var result = new WireSequence().Process(image, LightsState.Emergency, ref DummyImage);
		Assert.Null(result.Selection);
		Assert.Equal(1, result.StagesCleared);
		Assert.Equal(1, result.CurrentPageFirstWireNum);
		AssertList([WireSequence.WireColour.Blue, WireSequence.WireColour.Red, WireSequence.WireColour.Red], result.WireColours);
		Assert.Null(result.HighlightedWire);  // Highlight isn't clear enough in this sample.
	}

	[Fact]
	public void Sample5() {
		var image = Image.Load<Rgba32>(Resources.WireSequence5);
		var result = new WireSequence().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(0, 4), result.Selection);
		Assert.Equal(2, result.StagesCleared);
		Assert.Equal(7, result.CurrentPageFirstWireNum);
		AssertList([WireSequence.WireColour.Red, null, WireSequence.WireColour.Black], result.WireColours);
		Assert.Null(result.HighlightedWire);
	}

	[Fact]
	public void Sample6() {
		var image = Image.Load<Rgba32>(Resources.WireSequence6);
		var result = new WireSequence().Process(image, LightsState.Buzz, ref DummyImage);
		Assert.Equal(new(0, 2), result.Selection);
		Assert.Equal(3, result.StagesCleared);
		Assert.Equal(10, result.CurrentPageFirstWireNum);
		AssertList([WireSequence.WireColour.Blue, WireSequence.WireColour.Blue, WireSequence.WireColour.Black], result.WireColours);
		Assert.Equal(new(1, 'C'), result.HighlightedWire);
	}

	[Fact]
	public void Sample7() {
		var image = Image.Load<Rgba32>(Resources.WireSequence7);
		var result = new WireSequence().Process(image, LightsState.Off, ref DummyImage);
		Assert.Equal(new(0, 1), result.Selection);
		Assert.Equal(0, result.StagesCleared);
		Assert.Equal(1, result.CurrentPageFirstWireNum);
		AssertList([WireSequence.WireColour.Blue, WireSequence.WireColour.Blue, null], result.WireColours);
		Assert.Equal(new(0, 'C'), result.HighlightedWire);
	}

	[Fact]
	public void Sample8() {
		var image = Image.Load<Rgba32>(Resources.WireSequence8);
		var result = new WireSequence().Process(image, LightsState.Off, ref DummyImage);
		Assert.Null(result.Selection);
		Assert.Equal(1, result.StagesCleared);
		Assert.Equal(1, result.CurrentPageFirstWireNum);
		AssertList([null, WireSequence.WireColour.Blue, WireSequence.WireColour.Red], result.WireColours);
		Assert.Null(result.HighlightedWire);
	}
}
