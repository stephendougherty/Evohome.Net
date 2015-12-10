using Evohome.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Evohome.Console
{
    class Program
    {
        static int REFRESH_SECONDS = 300;
        static void Main(string[] args)
        {
            Controller cn = new Controller();
            Task t = cn.Init("", "");
            t.Wait();

            // Set a temp override and cancel again
            cn.Locations[0].Gateways[0].TemperatureControlSystems[0].Zones[0].SetTemperatureOverride(11, DateTime.Now.AddHours(1)).Wait();
            cn.Locations[0].Gateways[0].TemperatureControlSystems[0].Zones[0].CancelTemperatureOverride().Wait();

            // Copy the schedule down and writeit back again
            var st = cn.Locations[0].Gateways[0].TemperatureControlSystems[0].Zones[0].GetSchedule();
            st.Wait();
            var sched = st.Result;

            cn.Locations[0].Gateways[0].TemperatureControlSystems[0].Zones[0].SetSchedule(sched).Wait();

            // Export data to a csv file
            using (var s = File.AppendText(@"data.csv"))
            {
                foreach (var x in cn.Locations)
                {
                    foreach (var gateway in x.Gateways)
                    {
                        foreach (var tc in gateway.TemperatureControlSystems)
                        {
                            string line = "Time," + string.Join(",", from z in tc.Zones select z.Name) + "," + string.Join(",", from z in tc.Zones select z.Name);
                            System.Console.WriteLine(line);
                            s.WriteLine(line);
                        }
                    }
                }


                while (true)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    try
                    {
                        cn.UpdateStatus().Wait();
                        foreach (var x in cn.Locations)
                        {
                            foreach (var gateway in x.Gateways)
                            {
                                foreach (var tc in gateway.TemperatureControlSystems)
                                {
                                    string line = DateTime.UtcNow.ToString("HH:mm dd MMM yyyy") + "," + string.Join(",", from z in tc.Zones select z.Status.TemperatureStatus.Temperature) + "," + string.Join(",", from z in tc.Zones select z.Status.HeatSetpointStatus.TargetTemperature);
                                    System.Console.WriteLine(line);
                                    s.WriteLine(line);
                                }
                            }
                            s.Flush();
                        }
                    }
                    catch
                    { }
                    sw.Stop();
                    Thread.Sleep((int)Math.Max(0, (REFRESH_SECONDS * 1000) - sw.ElapsedMilliseconds));
                }
            }

            System.Console.ReadLine();
        }
    }
}
