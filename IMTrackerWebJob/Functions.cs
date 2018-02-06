using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using PushoverClient;
using System;
using System.Configuration;
using System.IO;
using System.Net;

namespace IMTrackerWebJob
{
    public class Functions
    {
        // This function will be triggered based on the schedule you have set for this WebJob
        // This function will enqueue a message on an Azure Queue called queue
        [NoAutomaticTrigger]
        public static void ManualTrigger(TextWriter log, int value, [Queue("queue")] out string message)
        {
            log.WriteLine("Function is invoked with value={0}", value);
            message = value.ToString();
            log.WriteLine("Following message will be written on the Queue={0}", message);
        }


        [NoAutomaticTrigger]
        public static void GetAthleteUpdates(TextWriter log)
        {
            Console.WriteLine("Checking the Ironman Athlete Tracker!");

            string urlpattern = ConfigurationManager.AppSettings["trackURL"];
            string allReferences = ConfigurationManager.AppSettings["athleteRefs"];
            string connectionString = ConfigurationManager.ConnectionStrings["AzureCloudStorage"].ConnectionString;

            // In format raceid-name:bib-name;bib-name/
            string pushoverAPIkey = ConfigurationManager.AppSettings["pushoverAPIkey"];
            string pushoverGroupKey = ConfigurationManager.AppSettings["pushoverGroupKey"];


            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create a reference to the file client.
            CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference("tracking");


            // Create a reference to the Azure path
            CloudFileDirectory cloudFileDirectory = share.GetRootDirectoryReference();


            foreach (string trackinfo in allReferences.Split('/'))
            {
                string raceinfo = trackinfo.Split(':')[0];
                string raceid = raceinfo.Split('-')[0];
                string racename = raceinfo.Split('-')[1];

                string[] athletes = trackinfo.Split(':')[1].Split(';');

                foreach (string athlete in athletes)
                {
                    string bib = athlete.Split('-')[0];
                    string athleteName = athlete.Split('-')[1];

                    string url = urlpattern.Replace("$rid$", raceid).Replace("$bib$", bib);
                    try
                    {
                        Console.WriteLine("About to download athelete url: " + url);

                        WebClient client = new WebClient();
                        string newFile = client.DownloadString(url);

                        Console.WriteLine(newFile);

                        int pFrom = newFile.IndexOf(@"<!-- Begin: main content area -->");
                        int pTo = newFile.IndexOf(@"<!-- End: main content area -->");

                        string latestTrackingData = newFile.Substring(pFrom, pTo - pFrom);


                        string filename = raceid + "-" + bib + ".html";

                        try
                        {
                            CloudFile cloudFile = cloudFileDirectory.GetFileReference(filename);

                            bool isUpdated = false;

                            if (cloudFile.Exists())
                            {
                                StreamReader sr = new StreamReader(cloudFile.OpenRead());
                                string previousTrackingData = sr.ReadToEnd();

                                if (!latestTrackingData.Equals(previousTrackingData, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    isUpdated = true;
                                }
                                sr.Close();
                            }

                            cloudFile.UploadTextAsync(latestTrackingData);

                            if (isUpdated)
                            {
                                Pushover pclient = new Pushover(pushoverAPIkey);
                                PushResponse response = pclient.Push(
                                              racename + " athlete alert",
                                              "Looks like " + athleteName + " has a new split update!",
                                              pushoverGroupKey
                                          );
                            }
                        }
                        catch (Exception ex1)
                        {
                            Console.WriteLine("Error!");
                            Console.WriteLine(ex1.Message);
                            Console.WriteLine(ex1.StackTrace);
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine("Error!");
                        Console.WriteLine(ex2.Message);
                        Console.WriteLine(ex2.StackTrace);
                    }

                }
            }
        }
    }
}
