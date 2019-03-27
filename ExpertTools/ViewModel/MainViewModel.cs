using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace ExpertTools.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private bool started;
        public bool Started
        {
            get { return started; }
            set { Set(() => Started, ref started, value); }
        }

        private string state;
        public string State
        {
            get { return state; }
            set { Set(() => State, ref state, value); }
        }

        private string timer;
        public string Timer
        {
            get { return timer; }
            set { Set(() => Timer, ref timer, value); }
        }

        public MainViewModel()
        {
            Logger.EnableFileLog();

            MessengerInstance.Register<StartMessage>(this, StartedMsgHandler);
            MessengerInstance.Register<StateMessage>(this, StateMsgHandler);

            Started = false;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            MessengerInstance.Unregister<StartMessage>(this, StartedMsgHandler);
            MessengerInstance.Unregister<StateMessage>(this, StateMsgHandler);
        }

        private void StartedMsgHandler(StartMessage msg)
        {
            Started = msg.Started;

            if (Started)
            {
                State = "Drink a cup of coffee, the processing of data is going...";
            }

            if (!Started && App.Current.MainWindow.WindowState == System.Windows.WindowState.Minimized)
            {
                App.Current.MainWindow.WindowState = System.Windows.WindowState.Normal;
            }

            if (!Started && msg.SuccessfullyCompleted)
            {
                State = "Successfully completed";
            }

            if (!Started && !msg.SuccessfullyCompleted)
            {
                State = "Completed with errors (see details in the log file)";
            }
        }

        private void StateMsgHandler(StateMessage msg)
        {
            State = msg.State;
        }
    }
}
