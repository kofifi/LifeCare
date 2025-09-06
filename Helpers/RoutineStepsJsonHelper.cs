using System.Text.Json;
using LifeCare.ViewModels;

namespace LifeCare.Helpers;

internal static class RoutineStepsJsonHelper
{
    public static List<RoutineStepVM> Parse(string? json)
    {
        var result = new List<RoutineStepVM>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;

        int order = 0;
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var vm = new RoutineStepVM
            {
                Id = el.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var idVal) ? idVal : 0,
                Name = el.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                EstimatedMinutes = el.TryGetProperty("minutes", out var minutes) && minutes.TryGetInt32(out var m) ? m : (int?)null,
                Description = el.TryGetProperty("desc", out var desc) ? desc.GetString() : null,
                Order = order++
            };

            if (el.TryGetProperty("rotation", out var rot) && rot.ValueKind == JsonValueKind.Object)
            {
                vm.RotationEnabled = rot.TryGetProperty("enabled", out var en) && en.GetBoolean();
                vm.RotationMode = rot.TryGetProperty("mode", out var md) ? md.GetString()?.ToUpperInvariant() : null;
            }

            if (el.TryGetProperty("products", out var prods) && prods.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in prods.EnumerateArray())
                {
                    var nameP = p.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
                    if (string.IsNullOrWhiteSpace(nameP)) continue;

                    var pid =
                        (p.TryGetProperty("id", out var pidEl) && pidEl.TryGetInt32(out var pidVal)) ? pidVal :
                        (p.TryGetProperty("productId", out var pid2) && pid2.TryGetInt32(out var pid2Val)) ? pid2Val : 0;

                    vm.Products.Add(new RoutineStepProductVM
                    {
                        Id = pid,
                        Name = nameP!,
                        Note = p.TryGetProperty("note", out var note) ? note.GetString() : null,
                        Url  = p.TryGetProperty("url",  out var url)  ? url.GetString()  : null,
                        ImageUrl = p.TryGetProperty("imageUrl", out var img) ? img.GetString() : null
                    });
                }
            }

            if (vm.Products.Count < 2)
            {
                vm.RotationEnabled = false;
                vm.RotationMode = null;
            }

            result.Add(vm);
        }

        return result;
    }
}
