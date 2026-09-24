using System.IO.Compression;
using System.Text.Json;
using Formify.Api.Dtos;
using Formify.Api.Models;
using Fluid;
using interfaces;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Formify.Api.Services;

public class PdfExportService : IPdfExportService, IAsyncDisposable
{
    private static readonly SemaphoreSlim BrowserFetchLock = new(1, 1);
    private static bool _browserFetched;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private static readonly FluidParser Parser = new();
    private readonly IFileContentService _fileContentService;

    public PdfExportService(IFileContentService fileContentService)
    {
        _fileContentService = fileContentService;
    }

    public async Task<string> RenderedContentAsync(string content, AppFile file, SchemaEntry schema)
    {
        // 1. Guard check equivalent to your early return
        if (string.IsNullOrEmpty(content) || file == null)
        {
            return string.Empty;
        }

        // 2. Prepare the mobile responsive base styles
        const string mobileResetCss = @"
            * { box-sizing: border-box; }
            body, html { margin: 0; padding: 0; }
            img { max-width: 100%; height: auto; }
            table { width: 100%; border-collapse: collapse; }
            p, li, td, th, span, div {
                font-size: clamp(14px, 4vw, 16px);
                line-height: 1.6;
                word-break: break-word;
                overflow-wrap: break-word;
            }
            h1 { font-size: clamp(20px, 5vw, 32px); }
            h2 { font-size: clamp(17px, 4.5vw, 26px); }
            h3 { font-size: clamp(15px, 4vw, 22px); }
            h4, h5, h6 { font-size: clamp(14px, 3.5vw, 18px); }
        ";

        // 3. Build the combined CSS block
        string customCss = schema?.TemplateCss ?? "";
        string combinedCss = string.IsNullOrWhiteSpace(customCss)
            ? mobileResetCss
            : $"{mobileResetCss}\n{customCss}";

        string styleTag = !string.IsNullOrWhiteSpace(combinedCss)
            ? $"<style>{combinedCss.Trim()}</style>\n"
            : string.Empty;

        // 4. Build the dynamic context dictionary
        var contextDict = new Dictionary<string, object>
        {
            { "name", file.Name ?? "" },
            { "createdAt", file.CreatedAt.ToString() ?? "" },
            { "updatedAt", file.UpdatedAt.ToString() ?? "" }
        };

        // Parse metadata array equivalent to Object.fromEntries + map logic
        if (file.Metadata != null)
        {
            foreach (var meta in file.Metadata)
            {
                if (string.IsNullOrEmpty(meta.Key)) continue;

                object? processedValue = meta.Value;

                // Safely handle string values that might be encoded JSON strings
                if (meta.Value is string stringValue)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(stringValue);
                        var element = doc.RootElement.Clone();

                        // Match JS mapping rules: evaluate actual JSON objects/primitives
                        processedValue = ConvertJsonElement(element);
                    }
                    catch (JsonException)
                    {
                        // Fallback if it's just a standard unparseable string
                        processedValue = stringValue;
                    }
                }

                // Unwrap helper logic representation
                if (processedValue != null)
                {
                    contextDict[meta.Key] = UnwrapMetadataValue(processedValue);
                }
            }
        }

        // 5. Initialize Fluid and map data safely
        var context = new TemplateContext(contextDict);

        try
        {
            if (Parser.TryParse(content, out var template, out var error))
            {
                string html = await template.RenderAsync(context);
                return $"{styleTag}{html}";
            }

            return $"{styleTag}{content}";
        }
        catch (Exception)
        {
            return $"{styleTag}{content}";
        }
    }

    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element
        };
    }
    private object UnwrapMetadataValue(object val) => val;
    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is { IsClosed: false }) return _browser;

        await _browserLock.WaitAsync();
        try
        {
            if (_browser is { IsClosed: false }) return _browser;
            await EnsureBrowserAsync();
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" },
            });
            return _browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    public async Task<string> RenderFolderAsZipAsync(
        List<AppFile> Files,
        SchemaEntry Schema,
        string zipFileNameWithoutExtension = "documents",
        CancellationToken ct = default)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string tempDir = Path.Combine(Path.GetTempPath(), $"export_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string zipPath = Path.Combine(Path.GetTempPath(), $"{zipFileNameWithoutExtension}_{Guid.NewGuid():N}.zip");

        try
        {
            if (Schema != null)
            {
                foreach (AppFile file in Files)
                {
                    string? fileContent = await _fileContentService.DownloadFileContentAsync(file.ContentId, ct);
                    if (string.IsNullOrWhiteSpace(fileContent))
                    {
                        continue;
                    }

                    string html = await this.RenderedContentAsync(fileContent, file, Schema);
                    byte[] pdfBytes = await this.RenderAsync(html, null);
                    string entryName = this.GetUniquePdfName(file?.Name, usedNames);
                    string filePath = Path.Combine(tempDir, entryName);

                    await File.WriteAllBytesAsync(filePath, pdfBytes);
                }
            }

            ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            return zipPath;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    public void CleanupExportedZip(string zipPath)
    {
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }
    }

    private string GetUniquePdfName(string? rawName, HashSet<string> usedNames)
    {
        string baseName = string.IsNullOrWhiteSpace(rawName) ? "document" : rawName;
        foreach (char c in Path.GetInvalidFileNameChars())
            baseName = baseName.Replace(c, '_');

        string candidate = $"{baseName}.pdf";
        int suffix = 1;
        while (!usedNames.Add(candidate))
        {
            candidate = $"{baseName} ({suffix}).pdf";
            suffix++;
        }

        return candidate;
    }

    public async Task<byte[]> RenderAsync(string html, int? marginPx, CancellationToken ct = default)
    {
        var browser = await GetBrowserAsync();
        await using var page = await browser.NewPageAsync();

        await page.SetViewportAsync(new ViewPortOptions { Width = 794, Height = 1123 });
        await page.EmulateMediaTypeAsync(MediaType.Print);

        await page.SetContentAsync(html, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
        });

        PdfOptions pdfOptions;
        if (marginPx != null)
        {
            var margin = $"{marginPx}px";
            pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions { Top = margin, Bottom = margin, Left = margin, Right = margin }
            };
        }
        else
        {
            pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
            };
        }

        byte[] pdfData = await page.PdfDataAsync(pdfOptions);
        return pdfData;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
    }

    private static async Task EnsureBrowserAsync()
    {
        if (_browserFetched) return;
        await BrowserFetchLock.WaitAsync();
        try
        {
            if (_browserFetched) return;
            await new BrowserFetcher().DownloadAsync();
            _browserFetched = true;
        }
        finally
        {
            BrowserFetchLock.Release();
        }
    }
}