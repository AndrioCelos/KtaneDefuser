using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BombDefuserConnector;
public abstract class ComponentProcessor {
	public abstract string Name { get; }
	protected internal abstract bool UsesNeedyFrame { get; }

	protected internal abstract float IsModulePresent(Image<Rgb24> image);

	protected internal abstract object ProcessNonGeneric(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap);
}

public abstract class ComponentProcessor<T> : ComponentProcessor where T : notnull {
	protected internal abstract T Process(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap);

	protected internal override object ProcessNonGeneric(Image<Rgb24> image, ref Image<Rgb24>? debugBitmap)
		=> this.Process(image, ref debugBitmap);
}