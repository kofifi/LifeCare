namespace LifeCare.ViewModels
{
    public class StatsPartialsVM
    {
        public string Prefix { get; set; } = "stats";
        public string WeekTitle { get; set; } = "Wpisy (tydzień)";
        public string PieTitle { get; set; } = "Pełne vs częściowe vs pominięte";
        public string CalendarTitle { get; set; } = "Kalendarz";

        public string LegendFull { get; set; } = "pełne";
        public string LegendPartial { get; set; } = "częściowe";
        public string LegendSkipped { get; set; } = "pominięte";
        public string LegendFuture { get; set; } = "przyszłe";
        public string LegendOff { get; set; } = "poza zakresem";

        public bool ShowSkips { get; set; } = false;
        public string SkipsTitle { get; set; } = "Najczęściej pomijane kroki";
    }
}