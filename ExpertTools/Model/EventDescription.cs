using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.IO;

namespace ExpertTools.Model
{
    public class EventDescription
    {
        public string Name { get; private set; }
        public ITargetBlock<string> TargetBlock { get; private set; }

        public EventDescription(string name, ITargetBlock<string> targetBlock)
        {
            Name = name;
            TargetBlock = targetBlock;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj is EventDescription @event &&
                   Name == @event.Name;
        }

        public override int GetHashCode()
        {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
        }
    }
}
