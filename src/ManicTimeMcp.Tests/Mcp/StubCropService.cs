using ManicTimeMcp.Screenshots;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubCropService(byte[]? result = null) : ICropService
{
	public byte[]? Crop(byte[] jpeg, CropRegion region) => result ?? jpeg;
}
