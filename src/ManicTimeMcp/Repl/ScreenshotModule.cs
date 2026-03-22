using ManicTimeMcp.Screenshots;
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
		screenshot.Map(
			"list",
			(
				ReplDateTimeRange window,
				ScreenshotListOptions options,
				IScreenshotService screenshotService,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.ListScreenshotsAsync(
						window,
						options,
						screenshotService,
						cancellationToken))
			.WithDescription("List screenshot metadata for a date-time window.")
			.WithDetails("Returns metadata only. Use screenshot get or screenshot crop to retrieve image bytes.")
			.ReadOnly();
	}

	private static void MapGet(IReplMap screenshot)
	{
		screenshot.Map(
			"get",
			(
				string screenshotRef,
				IScreenshotRegistry registry,
				IScreenshotService screenshotService) =>
					ManicTimeReplHandlers.GetScreenshot(
						screenshotRef,
						registry,
						screenshotService))
			.WithDescription("Fetch a screenshot payload by reference.")
			.WithDetails("Returns metadata plus thumbnail/full image payloads encoded as base64 text.")
			.ReadOnly();
	}

	private static void MapCrop(IReplMap screenshot)
	{
		screenshot.Map(
			"crop",
			(
				string screenshotRef,
				double x,
				double y,
				double width,
				double height,
				string? coordinateUnits,
				IScreenshotRegistry registry,
				IScreenshotService screenshotService,
				ICropService cropService) =>
					ManicTimeReplHandlers.CropScreenshot(
						screenshotRef,
						x,
						y,
						width,
						height,
						coordinateUnits,
						registry,
						screenshotService,
						cropService))
			.WithDescription("Crop a screenshot region of interest.")
			.ReadOnly();
	}

	private static void MapSave(IReplMap screenshot)
	{
		screenshot.Map(
			"save",
			(
				string screenshotRef,
				ScreenshotSaveOptions saveOptions,
				ScreenshotCropOptions cropOptions,
				IScreenshotRegistry registry,
				IScreenshotService screenshotService,
				ICropService cropService,
				IServiceProvider services,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.SaveScreenshotAsync(
						screenshotRef,
						saveOptions,
						cropOptions,
						registry,
						screenshotService,
						cropService,
						services,
						cancellationToken))
			.WithDescription("Persist a screenshot inside an MCP client root.")
			.WithDetails("Validates output paths against MCP client roots and can optionally save a cropped region.")
			.OpenWorld();
	}
}
