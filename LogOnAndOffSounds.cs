using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Cassia;
using System.Security.Principal;

namespace LogOnOffSounds
{
    public partial class LogOnAndOffSounds : ServiceBase
    {
        public LogOnAndOffSounds() => InitializeComponent();

        protected System.String
            LogOnFilePath = System.String.Empty,
            LogOffFilePath = System.String.Empty;


        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {

            base.OnSessionChange(changeDescription);
            System.String FileName = System.String.Empty;
            try
            {
                System.String subkeyDir = System.String.Empty, defSoundPath = System.String.Empty;
                switch (changeDescription.Reason)
                {
                    //case SessionChangeReason.SessionLogon:
                    case SessionChangeReason.SessionUnlock:
                        subkeyDir = @"AppEvents\Schemes\Apps\.Default\WindowsLogon\.Current";
                        defSoundPath = "C:\\Windows\\Media\\Windows Logon.wav";
                        break;
                    case SessionChangeReason.SessionLogoff:
                        subkeyDir = @"AppEvents\Schemes\Apps\.Default\WindowsLogoff\.Current";
                        defSoundPath = "C:\\Windows\\Media\\Windows Logoff Sound.wav";
                        break;
                }
                if(subkeyDir!=System.String.Empty)
                {
                    FileName = defSoundPath;
                    ITerminalServicesManager servicesManager = new TerminalServicesManager();
                    using (ITerminalServer server = servicesManager.GetLocalServer())
                    {
                        SecurityIdentifier securityIdentifier = (SecurityIdentifier)server?.GetSessions().DefaultIfEmpty(null).FirstOrDefault(p => p.SessionId == changeDescription.SessionId)?.UserAccount.Translate(typeof(SecurityIdentifier));
                        if (securityIdentifier != null)
                        {
                            using (RegistryKey SecureKey = Registry.Users.OpenSubKey(securityIdentifier.ToString(), RegistryKeyPermissionCheck.ReadSubTree))
                            //using (RegistryKey SecureKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default).OpenSubKey(securityIdentifier.ToString(), RegistryKeyPermissionCheck.ReadSubTree))
                            {
                                RegistryKey regKey = SecureKey?.OpenSubKey(subkeyDir, RegistryKeyPermissionCheck.ReadSubTree);
                                if (regKey != null)
                                {
                                    FileName = (System.String)regKey.GetValue(null, defSoundPath);
                                    regKey?.Dispose();
                                    
                                }
                            }
                        }
                    }
                }

                if (FileName != System.String.Empty)
                {
                    using (System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer(FileName))
                        soundPlayer.Play();
                }
            }
            catch(Exception ex) { }
        }

        protected override void OnStart(string[] args) => SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        protected override void OnStop() => SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            Action action = new Action(() =>
            { 

            });
        }
    }
}
