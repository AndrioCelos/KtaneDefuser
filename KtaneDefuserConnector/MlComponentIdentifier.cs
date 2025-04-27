using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.ML;
using Microsoft.ML.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KtaneDefuserConnector;

public class MlComponentIdentifier : IDisposable {
	private const int ImageHeight = 224;
	private const int ImageWidth = 224;
	private const float Mean = 117;
	private const string NullLabel = "_";

	private readonly PredictionEngine<ImagePixelData, ImagePrediction> predictor;

	public MlComponentIdentifier() {
		var mlContext = new MLContext();
		var path = File.Exists("MlComponentIdentifierModel.zip") ? "MlComponentIdentifierModel.zip" : Path.Combine(Path.GetDirectoryName(typeof(MlComponentIdentifier).Assembly.Location)!, "MlComponentIdentifierModel.zip");
		var trainedModel = mlContext.Model.Load(path, out _);
		predictor = mlContext.Model.CreatePredictionEngine<ImagePixelData, ImagePrediction>(trainedModel);
	}
	
	~MlComponentIdentifier() => Dispose();

	public string? Identify(Image<Rgba32> image) {
		// Transform the image into the format expected by the model (224 × 224 × 3 floats).
		var pixelData = new float[ImageHeight * ImageWidth * 3];
		image.ProcessPixelRows(p => {
			var i = 0;
			for (var y = 0; y < ImageHeight; y++) {
				var row = p.GetRowSpan(y + (y + 3) / 7);
				for (var x = 0; x < ImageWidth; x++) {
					var pixel = row[x + (x + 3) / 7];
					pixelData[i++] = pixel.R - Mean;
					pixelData[i++] = pixel.G - Mean;
					pixelData[i++] = pixel.B - Mean;
				}
			}
		});
		
		// Run the model.
		var prediction = predictor.Predict(new(pixelData, null));
		return prediction.PredictedLabelValue == NullLabel ? null : prediction.PredictedLabelValue;
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	private record ImagePixelData([property: ColumnName("input"), VectorType(ImageWidth, ImageHeight, 3)] float[] Input, string? Label);

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	private class ImagePrediction {
		public float[]? Score { get; set; }
		public string? PredictedLabelValue { get; set; }
	}

	public void Dispose() {
		predictor.Dispose();
		GC.SuppressFinalize(this);
	}
}
