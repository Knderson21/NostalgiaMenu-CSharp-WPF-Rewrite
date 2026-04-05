namespace NostalgiaMenu.Models
{
    public class GameEntry
    {
        public string SectionName  { get; set; }
        public string DisplayName  { get; set; }
        public string LauncherPath { get; set; }
        public string ImagePath    { get; set; }
        public string Color        { get; set; }
        public bool   IsDefault    { get; set; }
    }
}
