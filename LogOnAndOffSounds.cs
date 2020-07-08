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
using NAudio.Wave;
using NAudio;
using System.Threading;

namespace LogOnOffSounds
{
    public partial class LogOnAndOffSounds : ServiceBase
    {
        public LogOnAndOffSounds() => InitializeComponent();

        protected System.String
            LogOnFilePath = System.String.Empty,
            LogOffFilePath = System.String.Empty,
            UnlockFilePath = System.String.Empty;


        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
            System.String FileName = System.String.Empty;
            try
            {
                System.String subkeyDir = System.String.Empty, defSoundPath = System.String.Empty;
                Action readReg = new Action(() =>
                {
                    if (System.String.IsNullOrEmpty(subkeyDir)) return;
                    FileName = defSoundPath;
                    ITerminalServicesManager servicesManager = new TerminalServicesManager();
                    using (ITerminalServer server = servicesManager.GetLocalServer())
                    {
                        SecurityIdentifier securityIdentifier = (SecurityIdentifier)server?.GetSessions().DefaultIfEmpty(null).FirstOrDefault(p => p.SessionId == changeDescription.SessionId)?.UserAccount.Translate(typeof(SecurityIdentifier));
                        if (securityIdentifier != null)
                        {
                            using (RegistryKey SecureKey = Registry.Users.OpenSubKey(securityIdentifier.ToString(), RegistryKeyPermissionCheck.ReadSubTree))
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
                });
                Action uptSounds = new Action(() =>
                {
                    subkeyDir = @"AppEvents\Schemes\Apps\.Default\WindowsLogoff\.Current";
                    defSoundPath = "C:\\Windows\\Media\\Windows Logoff Sound.wav";
                    readReg();
                    this.LogOffFilePath = FileName;
                    subkeyDir = @"AppEvents\Schemes\Apps\.Default\WindowsUnlock\.Current";
                    defSoundPath = "C:\\Windows\\Media\\Windows Unlock.wav";
                    readReg();
                    this.UnlockFilePath = FileName;
                    subkeyDir = @"AppEvents\Schemes\Apps\.Default\WindowsLogon\.Current";
                    defSoundPath = "C:\\Windows\\Media\\Windows Logon.wav";
                    readReg();
                    this.LogOnFilePath = FileName;
                });
                switch (changeDescription.Reason)
                {
                    //case SessionChangeReason.SessionLogon:
                    case SessionChangeReason.SessionLogon:
                        uptSounds();
                        FileName = this.LogOnFilePath;
                        break;
                    case SessionChangeReason.SessionLogoff:
                        FileName = this.LogOffFilePath;
                        break;
                    case SessionChangeReason.SessionUnlock:
                        uptSounds();
                        FileName = this.UnlockFilePath;
                        break;
                }
                if (FileName != System.String.Empty)
                {
                    /*using (System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer(FileName))
                    {
                        soundPlayer.Load();
                        soundPlayer.Play();
                    }*/
                    using(WaveOutEvent output=new WaveOutEvent())
                    using (AudioFileReader reader=new  AudioFileReader(FileName))
                    {
                        output.Init(reader);
                        EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
                        output.PlaybackStopped += new EventHandler<StoppedEventArgs>((object sender, StoppedEventArgs args) => waitHandle.Set());
                        output.Play();
                        waitHandle.WaitOne();
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            finally { }
        }
    }
}
