using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector;
public abstract class ModuleProcessor {
	public abstract string Name { get; }
	public abstract bool UsesNeedyFrame { get; }

	public abstract float IsModulePresent(Image<Rgb24> image);

	public abstract object ProcessNonGeneric(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap);
}

public abstract class ComponentProcessor<T> : ModuleProcessor where T : notnull {
	public abstract T Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap);

	public override object ProcessNonGeneric(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap)
		=> this.Process(image, ref debugBitmap);
}