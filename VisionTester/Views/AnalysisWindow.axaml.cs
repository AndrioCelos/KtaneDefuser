using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VisionTester.ViewModels;
using Image = SixLabors.ImageSharp.Image;

namespace VisionTester.Views;

public partial class AnalysisWindow : Window
{
    public AnalysisWindow()
    {
        InitializeComponent();
		WeakReferenceMessenger.Default.Register<AnalysisViewModel.LoadImageFileMessage>(this, (_, m) => m.Reply(LoadImageFileAsync()));
		WeakReferenceMessenger.Default.Register<AnalysisViewModel.PasteImageMessage>(this, (_, m) => m.Reply(PasteImageAsync()));
		WeakReferenceMessenger.Default.Register<AnalysisViewModel.CopyImageMessage>(this, (_, m) => CopyImage(m.Image));
    }

	protected override void OnClosed(EventArgs e) {
		base.OnClosed(e);
		WeakReferenceMessenger.Default.UnregisterAll(this);
	}

	private async Task<IStorageFile?> LoadImageFileAsync() {
		var result = await GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(new() { FileTypeFilter = [FilePickerFileTypes.ImageAll] });
		return result.Count == 0 ? null : result[0];
	}

	private async Task<Image<Rgba32>?> PasteImageAsync() {
		var clipboard = GetTopLevel(this)!.Clipboard!;
		var data = await clipboard.GetDataAsync("PNG");
		if (data is null) {
			Debug.WriteLine($"Could not get image data; formats are {string.Join(", ", await clipboard.GetFormatsAsync())}");
			return null;
		}
		using var ms = new MemoryStream((byte[]) data);
		return Image.Load<Rgba32>(ms);
	}

	private void CopyImage(Image image) {
		var clipboard = GetTopLevel(this)!.Clipboard!;
		var dataObject = new DataObject();
		using var ms = new MemoryStream();
		image.SaveAsPng(ms);
		dataObject.Set("PNG", ms.ToArray());
		clipboard.SetDataObjectAsync(dataObject);
	}
}
