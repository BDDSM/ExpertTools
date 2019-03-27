using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace ExpertTools
{
    public class InfoMessage : MessageBase
    {
        public string Message { get; set; }

        public InfoMessage(string message)
        {
            Message = message;
        }
    }
}
