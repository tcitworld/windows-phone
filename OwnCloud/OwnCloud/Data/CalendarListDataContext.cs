﻿using OwnCloud.Data.Calendar.ParsedCalendar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace OwnCloud.Data
{
    /// <summary>
    /// Provide a list of calendars (uncompleted)
    /// </summary>
    class CalendarListDataContext
    {
        #region ctor
        
        /// <summary>
        /// Create the calendar list source for a account
        /// </summary>
        /// <param name="accountID">ID of the account for the calendars</param>
        public CalendarListDataContext(int accountID)
        {
            _accountId = accountID;
        }

        #endregion

        #region private Fields

        private int? _accountId;

        #endregion


        private ObservableCollection<ServerCalendarDisplayInfo> _serverCalendars;
        /// <summary>
        /// A list of all calendars, that exists on the server for a account
        /// </summary>
        public ObservableCollection<ServerCalendarDisplayInfo> ServerCalendars
        {
            get
            {
                if (_serverCalendars == null)
                {
                    _serverCalendars = new ObservableCollection<ServerCalendarDisplayInfo>();
                    LoadServerCalendars();
                }

                return _serverCalendars;
            }
        }

        #region Private Events

        /// <summary>
        /// Loads all server calendars for the account into ServerCalendars
        /// </summary>
        private void LoadServerCalendars()
        {

            using (Data.OwnCloudDataContext context = new OwnCloudDataContext())
            {
                //Get the account, to get the Url, where we can get all calendars
                var account = context.Accounts.Where(o => o.GUID == _accountId).Single();

                //Get clear credentials
                if (account.IsEncrypted)
                    account.RestoreCredentials();

                //Create caldav client to get all calendars
                Net.OcCalendarClient ocClient = new Net.OcCalendarClient(account.GetUri().AbsoluteUri ,
                    new Net.OwncloudCredentials { Username = account.Username, Password = account.Password }, account.CalDAVPath);

                //Load calendars
                ocClient.LoadCalendarInfoComplete += LoadCalendarInfoComplete;
                ocClient.LoadCalendarInfo();

            }
        }

        /// <summary>
        /// Callback for LoadServerCalendars
        /// </summary>
        void LoadCalendarInfoComplete(object sender, Net.LoadCalendarInfoCompleteArgs e)
        {
            if (!e.Success)
                return;

            //Run things in main thread
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    using (Data.OwnCloudDataContext context = new OwnCloudDataContext())
                    {
                        //Get the account, to get the Url, where we can get all calendars
                        var account = context.Accounts.Where(o => o.GUID == _accountId).Single();

                        //enumerate all calendars and add it to list
                        foreach (var calendar in e.CalendarInfo)
                        {
                            ServerCalendars.Add(
                                new ServerCalendarDisplayInfo
                                {
                                    CalendarInfo = calendar,
                                    //If the calendar is in the database, the calendar is "enabled"
                                    IsClientEnabled = account.Calendars.Count(o => o.Url == calendar.Url) > 0
                                });
                        }
                    }
                });

        }

        #endregion

    }
}
