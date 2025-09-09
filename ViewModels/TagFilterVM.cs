namespace LifeCare.ViewModels
{
    public class TagFilterVM
    {
        public IEnumerable<TagVM>? Tags { get; set; }
        public IEnumerable<int>? SelectedIds { get; set; }
        public string? QueryKey { get; set; }
        public string? Placeholder { get; set; }
        public string? SubmitMode { get; set; }
        public string? FormAction { get; set; }
        public string? Section { get; set; }
    }
}