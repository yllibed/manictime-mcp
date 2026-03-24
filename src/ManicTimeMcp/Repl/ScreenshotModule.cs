using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class ScreenshotModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("screenshot", screenshot =>
		{
			MapList(screenshot);
			MapGet(screenshot);
			MapCrop(screenshot);
			MapSave(screenshot);
		});
	}

	private static void MapList(IReplMap screenshot)
	{
		screenshot.Map("list", ManicTimeReplHandlers.ListScreenshotsAsync)
			.WithDescription("List screenshot metadata for a date-time window.")
			.WithDetails("Returns metadata only. Use screenshot get or screenshot crop to retrieve image bytes.")
			.ReadOnly();
	}

	private static void MapGet(IReplMap screenshot)
	{
		screenshot.Map("get", ManicTimeReplHandlers.GetScreenshot)
			.WithDescription("Fetch a screenshot payload by reference.")
			.WithDetails("Returns metadata plus thumbnail/full image payloads encoded as base64 text.")
			.ReadOnly();
	}

	private static void MapCrop(IReplMap screenshot)
	{
		screenshot.Map("crop", ManicTimeReplHandlers.CropScreenshot)
			.WithDescription("Crop a screenshot region of interest.")
			.ReadOnly();
	}

	private static void MapSave(IReplMap screenshot)
	{
		screenshot.Map("save", ManicTimeReplHandlers.SaveScreenshotAsync)
			.WithDescription("Persist a screenshot inside an MCP client root.")
			.WithDetails("Validates output paths against MCP client roots and can optionally save a cropped region.")
			.OpenWorld();
	}
}
