namespace DMS_Active_Timeouts_1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Core.DataMinerSystem.Automation;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Net.Messages;

    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        /// <summary>
        /// The script entry point.
        /// </summary>
        /// <param name="engine">Link with SLAutomation process.</param>
        public void Run(IEngine engine)
        {
            try
            {
                RunSafe(engine);
            }
            catch (ScriptAbortException)
            {
                // Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
                throw; // Comment if it should be treated as a normal exit of the script.
            }
            catch (ScriptForceAbortException)
            {
                // Catch forced abort exceptions, caused via external maintenance messages.
                throw;
            }
            catch (ScriptTimeoutException)
            {
                // Catch timeout exceptions for when a script has been running for too long.
                throw;
            }
            catch (InteractiveUserDetachedException)
            {
                // Catch a user detaching from the interactive script by closing the window.
                // Only applicable for interactive scripts, can be removed for non-interactive scripts.
                throw;
            }
            catch (Exception e)
            {
                engine.ExitFail("Run|Something went wrong: " + e);
            }
        }

        private void RunSafe(IEngine engine)
        {
            List<TestResult> results = new List<TestResult>();

            IDms dms = engine.GetDms();
            var agentNames = dms.GetAgents().Select(x => new Tuple<string, int>(x.Name, x.Id));

            DMSMessage[] responses = engine.SendSLNetMessage(new GetActiveAlarmsMessage());
            var alarmMessage = (ActiveAlarmsResponseMessage)responses.FirstOrDefault();

            if (alarmMessage != null)
            {
                var activeAlarms = alarmMessage.ActiveAlarms;

                foreach (var agent in agentNames)
                {
                    TestResult testResult = new TestResult
                    {
                        ParameterName = "Active Timeouts",
                        DmaName = agent.Item1,
                        ReceivedValue = Convert.ToString(activeAlarms.Count(x => x.Severity == "Timeout" && agent.Item2.Equals(x.HostingAgentID) && x.ParameterName == "DataMiner run-time")),
                    };

                    results.Add(testResult);
                }
            }

            engine.AddScriptOutput("result", JsonConvert.SerializeObject(results));
        }
    }

    public class TestResult
    {
        public string ParameterName { get; set; }

        public string DisplayName { get; set; }

        public string ElementName { get; set; }

        public string DmaName { get; set; }

        public string ReceivedValue { get; set; }

        public string ExpectedValue { get; set; }

        public bool Success { get; set; }
    }
}
