using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using System.Windows.Threading;

namespace ExpertTools.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private DispatcherTimer _timer;

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
                StartTimer();
            }
            else
            {
                StopTimer();
            }

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

        private void StartTimer()
        {
            var startTime = DateTime.Now;

            if (_timer == null)
            {
                _timer = new DispatcherTimer();
            }

            _timer.Interval = new TimeSpan(0, 0, 1);

            _timer.Tick += (sender, args) =>
            {
                var diff = DateTime.Now.Subtract(startTime).TotalSeconds;
                var min = (int)(diff / 60);
                var sec = (int)(diff - (min * 60));
                var value = $"{min} m. {sec} s.";
                Timer = value;
            };

            _timer.Start();
        }

        private void StopTimer()
        {
            _timer.Stop();
        }
    }
}
