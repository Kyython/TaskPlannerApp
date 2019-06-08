using FluentScheduler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using TaskPlanner.Models;
using TaskPlanner.Services;

namespace TaskPlanner
{
    public partial class MainWindow : Window
    {
        AppService _appService;

        public MainWindow()
        {
            InitializeComponent();

            _appService = new AppService();

            List<PeriodType> periodTypes = Enum.GetValues(typeof(PeriodType)).Cast<PeriodType>().ToList();
            periodTypeComboBox.ItemsSource = periodTypes;
            periodTypeComboBox.SelectedValue = PeriodType.Day;

            startDate.SelectedDate = DateTime.Now;
            endDate.SelectedDate = DateTime.Now.AddDays(1);

            dateToDownload.SelectedDate = DateTime.Now;
            dateToMove.SelectedDate = DateTime.Now;

            startTime.Text = DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString();
            endTime.Text = DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString();

            timeToMove.Text = DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString();
            timeToDownload.Text = DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString();
        }

        private void OpenExecute(object sender, ExecutedRoutedEventArgs e)
        {
            Show();
            taskPlanningIcon.Visibility = Visibility.Collapsed;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnClosing(e);

            taskPlanningIcon.Visibility = Visibility.Visible;
        }

        private async void SendEmailAsync()
        {
            string emailText = new TextRange(richTextBoxEmailText.Document.ContentStart, richTextBoxEmailText.Document.ContentEnd).Text;
            var result = await _appService.SendEmailAsync("theasanali7@gmail.com", "davisdex", toAddresEmail.Text, themeText.Text, emailText);

            MessageBox.Show(result);
        }

        private void SendEmailButtonClick(object sender, RoutedEventArgs e)
        {  
            if (toAddresEmail.Text == string.Empty || themeText.Text == string.Empty)
            {
                MessageBox.Show("Заполните все поля для опрации отправки сообщения!");
                return;
            }

            if (periodTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите период отправки сообщений!");
                return;
            }

            if (!TimeSpan.TryParse(startTime.Text, out TimeSpan timeStart))
            {
                MessageBox.Show("Время старта заполнено не корректно!");
                return;
            }

            if (!TimeSpan.TryParse(startTime.Text, out TimeSpan timeEnd))
            {
                MessageBox.Show("Время конца заполнено не корректно!");
                return;
            }

            if (startDate.SelectedDate.Value.AddHours(timeStart.Hours).AddMinutes(timeStart.Minutes) < DateTime.Now)
            {
                MessageBox.Show("Дата старта операции отправки сообщения не должна быть меньше нынешней даты!");
                return;
            }

            if (endDate.SelectedDate.Value.AddHours(timeEnd.Hours).AddMinutes(timeEnd.Minutes) < startDate.SelectedDate.Value.AddHours(timeStart.Hours).AddMinutes(timeStart.Minutes))
            {
                MessageBox.Show("Дата конца операции отправки сообщения не должна быть меньше даты старта!");
                return;
            }

            const int INTERVAL = 1;
            var periodType = (PeriodType)periodTypeComboBox.SelectedValue;

            string jobName = "";

            if (periodType == PeriodType.Day)
            {
                jobName = "DayJob";
                JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(SendEmailAsync), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunEvery(INTERVAL).Days().At(timeStart.Hours, timeStart.Minutes));
            }
            else if (periodType == PeriodType.Week)
            {
                jobName = "WeekJob";
                JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(SendEmailAsync), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunEvery(INTERVAL).Weeks().On(DayOfWeek.Monday).At(timeStart.Hours, timeStart.Minutes));
            }
            else if (periodType == PeriodType.Month)
            {
                jobName = "MonthJob";
                JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(SendEmailAsync), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunEvery(INTERVAL).Months().OnTheFirst(DayOfWeek.Monday).At(timeStart.Hours, timeStart.Minutes));
            }
            else if (periodType == PeriodType.Year)
            {
                jobName = "YearJob";
                JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(SendEmailAsync), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunEvery(INTERVAL).Years().On(1).At(timeStart.Hours, timeStart.Minutes));
            }

            if (startDate.DisplayDate == DateTime.Now.Date)
            {
                JobManager.Start();
            }

            if (endDate.SelectedDate.Value.AddHours(timeEnd.Hours).AddMinutes(timeEnd.Minutes) == DateTime.Now.Date)
            {
                JobManager.Stop();
                JobManager.RemoveJob(jobName);
            }
        }

        private void AddOnceInJob(DateTime operationStartDate, TimeSpan timeExecute, string jobName, Action action)
        {
            TimeSpan intervalTime = operationStartDate.AddHours(timeExecute.Hours).AddMinutes(timeExecute.Minutes) - DateTime.Now;
            int intervalCountSeconds = (int)intervalTime.TotalSeconds;

            JobManager.AddJob(() => Dispatcher.BeginInvoke(new Action(action), DispatcherPriority.Background), (schedule) => schedule.WithName(jobName).ToRunOnceIn(intervalCountSeconds).Seconds());

            if (operationStartDate.AddHours(timeExecute.Hours).AddMinutes(timeExecute.Minutes) == DateTime.Now)
            {
                JobManager.Stop();
                JobManager.RemoveJob(jobName);
            }
        }

        private async void MoveDirectoryAsync()
        {
            var result = await _appService.MoveCatalog(fromPathDirectory.Text, toPathDirectory.Text);
            MessageBox.Show(result);
        }

        private void MoveDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            if (fromPathDirectory.Text == string.Empty || toPathDirectory.Text == string.Empty)
            {
                MessageBox.Show("Заполните все поля для опрации перемещения директории!");
                return;
            }

            if (dateToMove.SelectedDate.Value == null || !TimeSpan.TryParse(timeToMove.Text, out TimeSpan timeExecute))
            {
                MessageBox.Show("Время выполнения заполнено не корректно!");
                return;
            }

            if (dateToMove.SelectedDate.Value.AddHours(timeExecute.Hours).AddMinutes(timeExecute.Minutes) < DateTime.Now)
            {
                MessageBox.Show("Дата старта операции перемещения директории не должна быть меньше нынешней даты!");
                return;
            }

            AddOnceInJob(dateToMove.SelectedDate.Value, timeExecute, "moveFile", MoveDirectoryAsync);
        }

        private async void DownloadFileAsync()
        {
            var result = await _appService.DownloadFile(fromPathDownload.Text, toPathDownload.Text);
            MessageBox.Show(result);
        }

        private void DownloadFileButtonClick(object sender, RoutedEventArgs e)
        {
            if (fromPathDownload.Text == string.Empty || toPathDownload.Text == string.Empty)
            {
                MessageBox.Show("Заполните все поля для опрации загрузки файла!");
                return;
            }

            if (dateToDownload.SelectedDate.Value == null || !TimeSpan.TryParse(timeToDownload.Text, out TimeSpan timeExecute))
            {
                MessageBox.Show("Время выполнения заполнено не корректно!");
                return;
            }

            if (dateToDownload.SelectedDate.Value.AddHours(timeExecute.Hours).AddMinutes(timeExecute.Minutes) < DateTime.Now)
            {
                MessageBox.Show("Дата старта операции загрузки файла не должна быть меньше нынешней даты!");
                return;
            }

            AddOnceInJob(dateToDownload.SelectedDate.Value, timeExecute, "downloadFile", DownloadFileAsync);
        }

        private void EnableSendMailMenu(object sender, RoutedEventArgs e)
        {
            mailGrid.IsEnabled = true;
            mailDateGrid.IsEnabled = true;
            moveGrid.IsEnabled = false;
            downloadGrid.IsEnabled = false;
        }

        private void EnableMoveDirectoryMenu(object sender, RoutedEventArgs e)
        {
            moveGrid.IsEnabled = true;
            mailGrid.IsEnabled = false;
            mailDateGrid.IsEnabled = false;
            downloadGrid.IsEnabled = false;
        }

        private void EnableDownloadFileMenu(object sender, RoutedEventArgs e)
        {
            downloadGrid.IsEnabled = true;
            moveGrid.IsEnabled = false;
            mailGrid.IsEnabled = false;
            mailDateGrid.IsEnabled = false;
        }
    }
}
