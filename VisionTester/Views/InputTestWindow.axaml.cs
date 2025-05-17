using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using KtaneDefuserConnector;
using KtaneDefuserConnectorApi;
using Microsoft.Extensions.Logging.Abstractions;
using Button = Avalonia.Controls.Button;

namespace VisionTester.Views;

public partial class InputTestWindow : Window {
	private readonly DefuserConnector _connector = new();
	
	public InputTestWindow() {
		InitializeComponent();

		LTButton.Click += async (_, _) => {
			LTSlider.Value = 1;
			await Task.Delay(250);
			LTSlider.Value = 0;
		};
		LXButton.Click += (_, _) => LXSlider.Value = 0;
		LYButton.Click += (_, _) => LYSlider.Value = 0;
		RTButton.Click += async (_, _) => {
			RTSlider.Value = 1;
			await Task.Delay(250);
			RTSlider.Value = 0;
		};
		RXButton.Click += (_, _) => RXSlider.Value = 0;
		RYButton.Click += (_, _) => RYSlider.Value = 0;

		LTSlider.ValueChanged += (_, _) => SendAxisAction(Axis.LT, (float) LTSlider.Value);
		LXSlider.ValueChanged += (_, _) => SendAxisAction(Axis.LeftStickX, (float) LXSlider.Value);
		LYSlider.ValueChanged += (_, _) => SendAxisAction(Axis.LeftStickY, (float) LYSlider.Value);
		RTSlider.ValueChanged += (_, _) => SendAxisAction(Axis.RT, (float) RTSlider.Value);
		RXSlider.ValueChanged += (_, _) => SendAxisAction(Axis.RightStickX, (float) RXSlider.Value);
		RYSlider.ValueChanged += (_, _) => RYBox.Value = (decimal) RYSlider.Value;
		RYBox.ValueChanged += (_, _) => SendAxisAction(Axis.RightStickY, (float) RYBox.Value!);
		
		foreach (var button in ButtonsGrid.GetLogicalDescendants().OfType<Button>()) {
			button.HorizontalAlignment = HorizontalAlignment.Stretch;
			button.HorizontalContentAlignment = HorizontalAlignment.Center;
			button.Click += Button_Click;
		}

		ZoomSlider.ValueChanged += (_, _) => ZoomBox.Value = (decimal) ZoomSlider.Value;
		ZoomButton.Click += (_, _) => _connector.SendInputs(new ZoomAction((float) ZoomBox.Value!));
	}

	private async void OnLoaded(object? sender, RoutedEventArgs e) {
		try {
			await _connector.ConnectAsync(NullLoggerFactory.Instance);
			StatusLabel.Content = "Connected";
		} catch (Exception ex) {
			StatusLabel.Content = ex.Message;
		}
	}

	private static KtaneDefuserConnectorApi.Button GetButton(object? button) {
		var text = ((Button) button!).Content!.ToString()!;
		return text switch {
			"\u2190" => KtaneDefuserConnectorApi.Button.Left,
			"\u2192" => KtaneDefuserConnectorApi.Button.Right,
			"\u2191" => KtaneDefuserConnectorApi.Button.Up,
			"\u2193" => KtaneDefuserConnectorApi.Button.Down,
			_ => Enum.Parse<KtaneDefuserConnectorApi.Button>(text)
		};
	}

	private void Button_Click(object? sender, RoutedEventArgs e) {
		var button = GetButton(sender);
		_connector.SendInputs(new ButtonAction(button));
	}

	private void SendAxisAction(Axis axis, float value) {
		_connector.SendInputs(new AxisAction(axis, value));
	}
}

