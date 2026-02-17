# ADR 0004 — Image Crop Dependency (SkiaSharp)

- Status: Accepted
- Date: 2026-02-16
- Deciders: Project maintainers
- Technical Story: WS-05, WS-06

## Context

The progressive resolution screenshot workflow (ADR-0003) includes a `crop_screenshot` tool that extracts a region of interest from a full-size screenshot. The public contract is defined as percentage-first (`0..100`) with optional normalized support (`0.0..1.0`) so selection can be derived from thumbnails while extracting pixels from the full-size source. This requires server-side JPEG decoding, coordinate transform, pixel-region extraction, clamping to image bounds, and re-encoding. The .NET base class library does not include robust production image processing for this use case, so an external dependency is needed.

## Decision

Add SkiaSharp as the image processing dependency for `crop_screenshot` and use server-side ROI-to-pixel mapping (percentage-first with optional normalized input) before crop execution, with bounds clamping.

## Decision Drivers

- Immediate target platform: Windows reliability first.
- Future portability: dependency should not block later Linux/macOS expansion.
- License compatibility: must be compatible with the project's open-source license.
- Binary size: should not bloat the self-contained deployment significantly.
- Maintenance health: must be actively maintained with a stable API.
- Capability fit: needs JPEG decode, ROI mapping (percentage + normalized), bounds clamping, pixel crop, JPEG re-encode.

## Considered Options

1. SkiaSharp
2. System.Drawing.Common
3. ImageSharp (SixLabors)
4. Defer crop to a future version

## Pros and Cons of the Options

### Option 1: SkiaSharp

- Pros:
  - Cross-platform (Windows, macOS, Linux) via Skia native binaries.
  - Google-backed, actively maintained, stable API.
  - ~5 MB native binary addition per platform.
  - MIT-like license (BSD 3-clause for Skia).
  - Well-proven in .NET ecosystem (used by Avalonia, Uno Platform, MAUI).
  - Efficient JPEG decode/encode with hardware acceleration where available.
- Cons:
  - Native binary dependency (platform-specific).
  - Adds ~5 MB to self-contained deployment per target runtime.

### Option 2: System.Drawing.Common

- Pros:
  - Ships with .NET (no extra dependency).
  - Familiar GDI+ API.
- Cons:
  - Windows-only since .NET 6 (CA1416 platform analyzer warning on non-Windows).
  - Contradicts the project's cross-platform ambition.
  - Deprecated for cross-platform use by Microsoft.

### Option 3: ImageSharp (SixLabors)

- Pros:
  - Pure managed .NET (no native binaries).
  - Cross-platform by design.
  - Feature-rich image processing API.
- Cons:
  - Dual license: open-source for non-commercial, commercial license required for SaaS/commercial use.
  - Larger memory footprint for JPEG operations compared to native-backed libraries.
  - Larger package size than SkiaSharp for the narrow use case needed.

### Option 4: Defer crop to future version

- Pros:
  - No new dependency in v1.
  - Simpler initial deployment.
- Cons:
  - Delays a high-value feature that enables progressive resolution.
  - Forces AI models to process full screenshots instead of focused regions.
  - Increases token cost for screenshot-heavy workflows.

## Consequences

### Positive

- `crop_screenshot` becomes available with percentage-first ROI inputs that are practical for thumbnail-driven workflows.
- Progressive resolution workflow is fully operational.
- AI models can request focused regions, reducing unnecessary context.
- SkiaSharp's native performance keeps crop latency low.

### Negative

- ~5 MB native binary added to Windows self-contained packages (initial target).
- Platform-specific native binaries require correct RID selection when expanding beyond Windows.

### Neutral

- SkiaSharp is only loaded when `crop_screenshot` is invoked; no startup cost for other tools.
- The dependency is isolated to the screenshot pipeline; no coupling to database or MCP layers.

## Implementation Notes

- Impacted projects/files: `ManicTimeMcp.csproj` (package reference), screenshot crop handler, packaging configuration.
- Migration/backward-compatibility considerations: none; this is a new capability.
- Test/verification requirements: crop correctness tests with known input/output images, percentage/normalized transform tests, clamping tests, Windows packaging/runtime validation.

## References

- `spec/05-screenshot-pipeline.md`
- `spec/06-mcp-contract-tools-resources-prompts.md`
- `spec/adr/0003-screenshot-content-block-strategy.md`
- SkiaSharp: https://github.com/mono/SkiaSharp
