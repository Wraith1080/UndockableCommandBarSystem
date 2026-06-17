namespace CommandBar.Core.Models
{
    public class CommandSeparator : CommandItem
    {
        public override CommandItem Clone()
        {
            return new CommandSeparator
            {
                Id = this.Id
            };
        }
    }
}