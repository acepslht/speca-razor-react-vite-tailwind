using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Speca.Core.Services;

public class ViteService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ViteService> _logger;
    private readonly Dictionary<string, JsonElement> _manifest = [];

    public bool IsDevelopment => _env.IsDevelopment();

    public ViteService(IWebHostEnvironment env, ILogger<ViteService> logger)
    {
        _env = env;
        _logger = logger;

        if (!IsDevelopment)
        {
            var path = Path.Combine(_env.WebRootPath, "dist", ".vite", "manifest.json");

            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    _manifest = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
                    _logger.LogInformation("[Speca.Vite] Manifest loaded successfully with {Count} entries.", _manifest.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Speca.Vite] Failed to load manifest at {Path}", path);
                }
            }
            else
            {
                _logger.LogWarning("[Speca.Vite] Manifest file not found at {Path}. Production assets may fail to load.", path);
            }
        }
    }

    public string GetAssetUrl(string entryKey)
    {
        if (IsDevelopment) return $"/{entryKey}";

        var entry = _manifest.FirstOrDefault(x =>
            x.Key.Equals(entryKey, StringComparison.OrdinalIgnoreCase) ||
            x.Key.EndsWith(entryKey, StringComparison.OrdinalIgnoreCase));

        if (entry.Value.ValueKind != JsonValueKind.Undefined)
        {
            var fileName = entry.Value.GetProperty("file").GetString();
            return $"/dist/{fileName}";
        }

        // Logging untuk mempermudah debug saat produksi
        _logger.LogWarning("[Speca.Vite] Asset not found in manifest: {EntryKey}", entryKey);
        return $"/dist/{entryKey.TrimStart('/')}";
    }
}