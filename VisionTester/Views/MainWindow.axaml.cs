using Avalonia.Controls;
using Avalonia.Interactivity;
using VisionTester.ViewModels;

namespace VisionTester.Views;

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();
	}

	private void Button_OnClick(object? sender, RoutedEventArgs e) {
		var vm = new TransformationViewModel() { mainViewModel = (MainWindowViewModel) DataContext! };
		var window = new TransformationWindow() { DataContext = vm };
		window.Show(this);
	}

	private void AnalysisButton_OnClick(object? sender, RoutedEventArgs e) {
		var vm = new AnalysisViewModel();
		((MainWindowViewModel) DataContext!).AnalysisViewModel = vm;
		var window = new AnalysisWindow() { DataContext = vm };
		window.Show(this);
	}
	
	private void ColourRangeButton_OnClick(object? sender, RoutedEventArgs e) {
		var vm = new ColourRangeViewModel();
		var window = new ColourRangeWindow() { DataContext = vm };
		window.Show(this);
	}

	private void InputTestButton_OnClick(object? sender, RoutedEventArgs e) {
		new InputTestWindow().Show(this);
	}
}
