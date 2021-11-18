using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Gengar.Handlers
{
	public class DateTimeHandler
	{
		private static readonly Timer timer;

		static DateTimeHandler()
		{
			timer = new Timer(GetSleepTime());
			timer.Elapsed += (o, e) =>
			{
				OnDayChanged(DateTime.Now);
				timer.Interval = GetSleepTime();
			};
			timer.Start();

			SystemEvents.TimeChanged += new EventHandler(SystemEvents_TimeChanged);
		}

		private static void SystemEvents_TimeChanged(object sender, EventArgs e)
		{
			timer.Interval = GetSleepTime();
		}

		private static double GetSleepTime()
		{
			var midnightTonight = DateTime.Now.AddDays(1);
			var differenceInMilliseconds = (midnightTonight - DateTime.Now).TotalMilliseconds + 10000;
			return differenceInMilliseconds;
		}

		public static void OnDayChanged(DateTime day)
		{
			DayChanged?.Invoke(null, new DayChangedEventArgs(day));
		}

		public static event EventHandler<DayChangedEventArgs> DayChanged;
	}

	public class DayChangedEventArgs : EventArgs
	{
		public DayChangedEventArgs(DateTime day)
		{
			this.Date = day;
		}

		public DateTime Date { get; private set; }
	}
}
