﻿// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-03
// 
// Licensed under the MIT License.

using GoalSetting;
using GoalSetting.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.Data;
using Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using WindowsActivityTracker.Data;

namespace GoalSettingTests.Visualization
{
    [TestClass()]
    public class TestVisualization
    {

        private static readonly string _testDBName = "pa-test.dat";

        [TestMethod()]
        public void TestRetrospectionGoalSetting()
        {
            //Set database path to test database
            bool switchedToTestDB = false;
            try
            {
                var field = typeof(Database).GetField("_databaseFileName", BindingFlags.Static | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(null, _testDBName);
                    switchedToTestDB = field.GetValue(null).Equals(_testDBName);
                }
            }
            catch (Exception e)
            {
                switchedToTestDB = false;
            }

            //Execute the rest of the test case only if we have sucessfully switched to the test DB!
            if (switchedToTestDB)
            {
                //Connect to database
                Database.GetInstance().Connect();

                //Delete tables so that data from the last test run is deleted
                Database.GetInstance().ExecuteDefaultQuery("DROP table windows_activity;");
                Database.GetInstance().ExecuteDefaultQuery("DROP table goals;");
                Database.GetInstance().ExecuteDefaultQuery("DROP table emails;");

                //Create tables
                MethodInfo createGoalMethod = typeof(DatabaseConnector).GetMethod("CreateRulesTableIfNotExists", BindingFlags.NonPublic | BindingFlags.Static);
                if (createGoalMethod != null)
                {
                    createGoalMethod.Invoke(null, new object[] { });
                }

                MethodInfo createActivitiesMethod = typeof(Queries).GetMethod("CreateWindowsActivityTable", BindingFlags.NonPublic | BindingFlags.Static);
                if (createActivitiesMethod != null)
                {
                    createActivitiesMethod.Invoke(null, new object[] { });
                }

                MethodInfo createEmailsMethod = typeof(MsOfficeTracker.Data.Queries).GetMethod("CreateMsTrackerTables", BindingFlags.NonPublic | BindingFlags.Static);
                if (createEmailsMethod != null)
                {
                    createEmailsMethod.Invoke(null, new object[] { });
                }

                //Add activity data
                List<Tuple<string, string, DateTime>> activites = new List<Tuple<string, string, DateTime>>();
                DateTime today = DateTime.Now;

                //10 minutes on Dev
                activites.Add(Tuple.Create("IDLE", "IDLE", new DateTime(today.Year, today.Month, today.Day, 1, 59, 59)));
                activites.Add(Tuple.Create("Microsoft Visual Studio", "devenv", new DateTime(today.Year, today.Month, today.Day, 2, 0, 0)));
                activites.Add(Tuple.Create("Microsoft Visual Studio", "devenv", new DateTime(today.Year, today.Month, today.Day, 2, 10, 0)));
                activites.Add(Tuple.Create("IDLE", "IDLE", new DateTime(today.Year, today.Month, today.Day, 2, 10, 1)));

                //20 minutes on Dev 
                activites.Add(Tuple.Create("Microsoft Visual Studio", "devenv", new DateTime(today.Year, today.Month, today.Day, 2, 59, 59)));
                activites.Add(Tuple.Create("Microsoft Visual Studio", "devenv", new DateTime(today.Year, today.Month, today.Day, 3, 19, 59)));
                activites.Add(Tuple.Create("IDLE", "IDLE", new DateTime(today.Year, today.Month, today.Day, 3, 20, 0)));

                //5 minutes on work unrelated browsing
                activites.Add(Tuple.Create("www.20min.ch - News von Jetzt", "chrome", new DateTime(today.Year, today.Month, today.Day, 3, 21, 0)));
                activites.Add(Tuple.Create("IDLE", "IDLE", new DateTime(today.Year, today.Month, today.Day, 3, 26, 0)));

                //another 5 minutes on work unrelated browsing
                activites.Add(Tuple.Create("www.20min.ch - News von Jetzt", "chrome", new DateTime(today.Year, today.Month, today.Day, 4, 21, 0)));
                activites.Add(Tuple.Create("IDLE", "IDLE", new DateTime(today.Year, today.Month, today.Day, 4, 26, 0)));

                //30 minutes on work related browsing
                activites.Add(Tuple.Create("stackoverflow.com", "chrome", new DateTime(today.Year, today.Month, today.Day, 5, 21, 0)));
                activites.Add(Tuple.Create("IDLE", "IDLE", new DateTime(today.Year, today.Month, today.Day, 5, 51, 0)));

                //35 minutes on dev
                activites.Add(Tuple.Create("Microsoft Visual Studio", "devenv", new DateTime(today.Year, today.Month, today.Day, 6, 0, 0)));
                activites.Add(Tuple.Create("Microsoft Visual Studio", "devenv", new DateTime(today.Year, today.Month, today.Day, 6, 1, 0)));
                activites.Add(Tuple.Create("stackoverflow.com", "chrome", new DateTime(today.Year, today.Month, today.Day, 6, 36, 1)));
                activites.Add(Tuple.Create("stackoverflow.com", "chrome", new DateTime(today.Year, today.Month, today.Day, 6, 42, 1)));

                //30 minutes on dev
                activites.Add(Tuple.Create("Microsoft Visual Studio", "devenv", new DateTime(today.Year, today.Month, today.Day, 7, 0, 0)));
                activites.Add(Tuple.Create("Microsoft Visual Studio", "devenv", new DateTime(today.Year, today.Month, today.Day, 7, 1, 0)));
                activites.Add(Tuple.Create("stackoverflow.com", "chrome", new DateTime(today.Year, today.Month, today.Day, 7, 31, 1)));
                activites.Add(Tuple.Create("stackoverflow.com", "chrome", new DateTime(today.Year, today.Month, today.Day, 7, 42, 1)));

                foreach (Tuple<string, string, DateTime> activity in activites)
                {
                    Database.GetInstance().ExecuteDefaultQuery("INSERT INTO windows_activity (time, window, process) VALUES (strftime('%Y-%m-%d %H:%M:%f', '" + activity.Item3.ToString("yyyy-MM-dd HH:mm:ss.FFF") + "', 'localtime'), " + Database.GetInstance().Q(activity.Item1) + ", " + Database.GetInstance().Q(activity.Item2) + ")");
                }

                //Add goals
                ObservableCollection<PARule> rules = new ObservableCollection<PARule>();
                rules.Add(new PARuleActivity { Title = "Test Rule 1", IsVisualizationEnabled = true, Activity = ContextCategory.DevCode, TimeSpan = RuleTimeSpan.EveryDay, Rule = new Rule { Operator = GoalSetting.Model.Operator.LessThan, Goal = GoalSetting.Model.Goal.TimeSpentOn, TargetValue = "" + TimeSpan.FromHours(1).TotalMilliseconds } });
                rules.Add(new PARuleEmail { Title = "Test Rule 2", IsVisualizationEnabled = true, TimePoint = RuleTimePoint.End, Rule = new Rule { Operator = GoalSetting.Model.Operator.LessThan, Goal = GoalSetting.Model.Goal.NumberOfEmailsInInbox, TargetValue = "1" } });
                rules.Add(new PARuleActivity { Title = "Test Rule 3", IsVisualizationEnabled = true, Activity = ContextCategory.WorkUnrelatedBrowsing, TimeSpan = RuleTimeSpan.EveryDay, Rule = new Rule { Operator = GoalSetting.Model.Operator.LessThan, Goal = GoalSetting.Model.Goal.NumberOfSwitchesTo, TargetValue = "10" } });

                MethodInfo saveRulesMethod = typeof(DatabaseConnector).GetMethod("SaveRules", BindingFlags.NonPublic | BindingFlags.Static);
                if (saveRulesMethod != null)
                {
                    saveRulesMethod.Invoke(null, new object[] { rules });
                }

                //Add email data
                List<Tuple<DateTime, int, int, int, int, int>> emails = new List<Tuple<DateTime, int, int, int, int, int>>();
                DateTimeOffset tonight = DateTimeHelper.GetEndOfDay(DateTime.Now);

                emails.Add(Tuple.Create(tonight.Date, 6, 7, 14, 21, 28));
                emails.Add(Tuple.Create(tonight.Date.Subtract(TimeSpan.FromDays(1)), 5, 8, 15, 22, 29));
                emails.Add(Tuple.Create(tonight.Date.Subtract(TimeSpan.FromDays(2)), 4, 9, 16, 23, 30));
                emails.Add(Tuple.Create(tonight.Date.Subtract(TimeSpan.FromDays(3)), 4, 10, 17, 24, 31));
                emails.Add(Tuple.Create(tonight.Date.Subtract(TimeSpan.FromDays(4)), 2, 11, 18, 25, 32));
                emails.Add(Tuple.Create(tonight.Date.Subtract(TimeSpan.FromDays(5)), 1, 12, 19, 26, 33));
                emails.Add(Tuple.Create(tonight.Date.Subtract(TimeSpan.FromDays(6)), 0, 13, 20, 27, 34));

                MethodInfo saveEmails = typeof(MsOfficeTracker.Data.Queries).GetMethod("SaveEmailsSnapshot", BindingFlags.NonPublic | BindingFlags.Static);
                if (saveEmails != null)
                {
                    foreach (Tuple<DateTime, int, int, int, int, int> email in emails)
                    {
                        saveEmails.Invoke(null, new object[] { email.Item1, (long)email.Item2, (long)email.Item3, email.Item4, email.Item5, email.Item6, false });
                    }
                }

                Retrospection.Handler.GetInstance().Start(new List<Shared.ITracker>() { new GoalSetting.Visualizers.GoalVisualizer() }, "test version");
                GoalSettingManager.Instance.Start();
            }
        }

    }
    
}