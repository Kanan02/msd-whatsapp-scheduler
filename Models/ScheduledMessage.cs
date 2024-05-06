using System;

namespace msd_whatsapp_scheduler.Models
{
    public class ScheduledMessage
    {
        public string ContactName { get; set; }
        public string Message { get; set; }
        public DateTime ScheduleDateTime { get; set; }
    }

}
