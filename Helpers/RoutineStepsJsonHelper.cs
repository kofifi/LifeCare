using System.Text.Json;
using System.Text.Json.Serialization;
using LifeCare.ViewModels;

namespace LifeCare.Helpers
{
    public static class RoutineStepsJsonHelper
    {
        private sealed class StepDto
        {
            [JsonPropertyName("id")] public int? Id { get; set; }
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("minutes")] public int? Minutes { get; set; }
            [JsonPropertyName("desc")] public string? Desc { get; set; }

            public sealed class RotationDto
            {
                [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
                [JsonPropertyName("mode")] public string? Mode { get; set; }
            }

            [JsonPropertyName("rotation")] public RotationDto? Rotation { get; set; }

            [JsonPropertyName("rrule")] public string? RRule { get; set; }

            public sealed class ProductDto
            {
                [JsonPropertyName("id")] public int? Id { get; set; }
                [JsonPropertyName("name")] public string? Name { get; set; }
                [JsonPropertyName("note")] public string? Note { get; set; }
                [JsonPropertyName("url")] public string? Url { get; set; }
                [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
            }

            [JsonPropertyName("products")] public List<ProductDto>? Products { get; set; }
        }

        public static List<RoutineStepVM> Parse(string? stepsJson)
        {
            var result = new List<RoutineStepVM>();
            if (string.IsNullOrWhiteSpace(stepsJson))
                return result;

            StepDto[]? items;
            try
            {
                items = JsonSerializer.Deserialize<StepDto[]>(stepsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
            }
            catch
            {
                return result;
            }

            if (items == null || items.Length == 0)
                return result;

            int order = 0;
            foreach (var s in items)
            {
                var name = (s.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var vm = new RoutineStepVM
                {
                    Id = s.Id ?? 0,
                    Name = name,
                    EstimatedMinutes = Math.Max(0, s.Minutes ?? 0),
                    Description = string.IsNullOrWhiteSpace(s.Desc) ? null : s.Desc!.Trim(),
                    Order = order++,

                    RotationEnabled = s.Rotation?.Enabled ?? false,
                    RotationMode = string.IsNullOrWhiteSpace(s.Rotation?.Mode) ? null : s.Rotation!.Mode!.Trim(),

                    RRule = string.IsNullOrWhiteSpace(s.RRule) ? null : s.RRule!.Trim()
                };

                if (s.Products != null && s.Products.Count > 0)
                {
                    vm.Products = s.Products
                        .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                        .Select(p => new RoutineStepProductVM
                        {
                            Id = p.Id ?? 0,
                            Name = p.Name!.Trim(),
                            Note = string.IsNullOrWhiteSpace(p.Note) ? null : p.Note!.Trim(),
                            Url = string.IsNullOrWhiteSpace(p.Url) ? null : p.Url!.Trim(),
                            ImageUrl = string.IsNullOrWhiteSpace(p.ImageUrl) ? null : p.ImageUrl!.Trim()
                        })
                        .ToList();
                }
                else
                {
                    vm.Products = new List<RoutineStepProductVM>();
                }

                result.Add(vm);
            }

            return result;
        }
    }
}