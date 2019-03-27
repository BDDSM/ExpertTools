using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace ExpertTools
{
    public class StartMessage : MessageBase
    {
        public bool Started { get; private set; }
        public bool SuccessfullyCompleted { get; private set; }

        public static StartMessage AnalyzeStarted()
        {
            var msg = new StartMessage
            {
                Started = true
            };

            return msg;
        }

        public static StartMessage SuccessfullyCompeled()
        {
            var msg = new StartMessage
            {
                Started = false,
                SuccessfullyCompleted = true
            };

            return msg;
        }

        public static StartMessage UnsuccessfullyCompeled()
        {
            var msg = new StartMessage
            {
                Started = false,
                SuccessfullyCompleted = false
            };

            return msg;
        }
    }
}
