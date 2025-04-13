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

namespace VisionTester.Views;

public partial class TransformationWindow : Window {
	public TransformationWindow() {
		InitializeComponent();
		WeakReferenceMessenger.Default.Register<TransformationViewModel.LoadImageFileMessage>(this, (_, m) => m.Reply(LoadImageFileAsync()));
		WeakReferenceMessenger.Default.Register<TransformationViewModel.PasteImageMessage>(this, (_, m) => m.Reply(PasteImageAsync()));
		WeakReferenceMessenger.Default.Register<TransformationViewModel.SaveImageFileMessage>(this, (_, m) => m.Reply(SaveImageFileAsync()));
		WeakReferenceMessenger.Default.Register<TransformationViewModel.CopyTextMessage>(this, (_, m) => CopyText(m.Text));
		WeakReferenceMessenger.Default.Register<TransformationViewModel.CopyImageMessage>(this, (_, m) => CopyImage(m.Image));
	}
	
	protected override void OnClosed(EventArgs e) {
		base.OnClosed(e);
		WeakReferenceMessenger.Default.UnregisterAll(this);
	}

	private async Task<IStorageFile?> LoadImageFileAsync() {
		var result = await GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(new() { FileTypeFilter = [FilePickerFileTypes.ImageAll]});
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
		return SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
	}

	private async Task<IStorageFile?> SaveImageFileAsync() {
		var result = await GetTopLevel(this)!.StorageProvider.SaveFilePickerAsync(new() { DefaultExtension = ".png", FileTypeChoices = [FilePickerFileTypes.ImagePng, FilePickerFileTypes.ImageJpg, FilePickerFileTypes.ImageWebp] });
		return result;
	}
	
	private void CopyText(string text) {
		var clipboard = GetTopLevel(this)!.Clipboard!;
		clipboard.SetTextAsync(text);
	}

	private void CopyImage(SixLabors.ImageSharp.Image image) {
		var clipboard = GetTopLevel(this)!.Clipboard!;
		var dataObject = new DataObject();
		using var ms = new MemoryStream();
		image.SaveAsPng(ms);
		dataObject.Set("PNG", ms.ToArray());
		clipboard.SetDataObjectAsync(dataObject);
	}
}
