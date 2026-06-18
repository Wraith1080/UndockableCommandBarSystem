namespace CommandBar.Core.Models
{
    public class CommandSeparator : CommandItem
    {
        // NEW: Flags this specific item as a separator
        public override bool IsSeparator => true;

        public override CommandItem Clone()
        {
            return new CommandSeparator();
        }
    }
}