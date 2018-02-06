using Microsoft.Azure.WebJobs;
using PushoverClient;
using Quartz;
using Quartz.Impl;
using System;
using System.Configuration;
using System.Threading;

namespace IMTrackerWebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var host = new JobHost();

            string status = ConfigurationManager.AppSettings["status"];
            if (status.Equals("active", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.Write("WebJob active");
                

                string pushoverAPIkey = ConfigurationManager.AppSettings["pushoverAPIkey"];
                string pushoverAdminKey = ConfigurationManager.AppSettings["pushoverAdminKey"];

                // The following code will invoke a function called ManualTrigger and 
                // pass in data (value in this case) to the function

                int refreshfrequencySecs = 5;
                int.TryParse(ConfigurationManager.AppSettings["refreshfrequencySecs"], out refreshfrequencySecs);

                
                /*
                Console.Write("Last daily heartbeat: " + lastDailyPing);

                if (DateTime.Today.CompareTo(dtPing) > 0)
                {
                    ConfigurationManager.AppSettings["lastDailyPing"] = DateTime.Today.ToString("dd/MM/yyyy");
                    string allReferences = ConfigurationManager.AppSettings["athleteRefs"];
                    Pushover pclient = new Pushover(pushoverAPIkey);
                    PushResponse response = pclient.Push(
                              "Started Athlete Alerting",
                              "reporting on: " + allReferences,
                              pushoverAdminKey
                          );
                }
                */
                while (true)
                {
                    host.Call(typeof(Functions).GetMethod("GetAthleteUpdates"));
                    Thread.Sleep(refreshfrequencySecs * 1000);
                }
            }
            else
            {
                int dormantsleepsecs = 3600;
                int.TryParse(ConfigurationManager.AppSettings["dormantsleepsecs"], out dormantsleepsecs);

                Console.Write("WebJob not active. Waiting for " + dormantsleepsecs + " seconds");
                Thread.Sleep(dormantsleepsecs * 1000);
            }
        }

    }
}
