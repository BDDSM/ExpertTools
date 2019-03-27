using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace ExpertTools
{
    public class StateMessage : MessageBase
    {
        public string State { get; private set; }

        public StateMessage(string state)
        {
            State = state;
        }
    }
}
