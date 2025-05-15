using Tesseract;

namespace KtaneDefuserConnector;

internal class Tesseract {
	internal static readonly TesseractEngine TesseractEngine = new("tessdata", "eng", EngineMode.Default);
}
