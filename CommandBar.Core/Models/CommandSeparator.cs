namespace CommandBar.Core.Models
{
    public class CommandSeparator : CommandItem
    {
        // NEW: Flags this specific item as a separator
        public override bool IsSeparator => true;

        public override CommandItem Clone()
        {
            return new CommandSeparator
            {
                Id = this.Id,
                Text = this.Text,
                Tooltip = this.Tooltip,
                IconGeometry = this.IconGeometry,
                ActionCallback = this.ActionCallback,
                DisplayMode = this.DisplayMode,
                KeepOriginalColors = this.KeepOriginalColors,
                RawSvgContent = this.RawSvgContent,
                IsVisible = this.IsVisible
            };
        }
    }
}