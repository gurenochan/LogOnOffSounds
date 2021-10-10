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
        protected int SessionId;

        public System.String ReadReg(System.String subkeyDir, int sessionId)
        {
            System.String FileName = System.String.Empty;
            try
            {
                if (System.String.IsNullOrEmpty(subkeyDir)) return FileName;
                ITerminalServicesManager servicesManager = new TerminalServicesManager();
                using (ITerminalServer server = servicesManager.GetLocalServer())
                {
                    SecurityIdentifier securityIdentifier = (SecurityIdentifier)server?.GetSessions().DefaultIfEmpty(null).FirstOrDefault(p => p.SessionId == sessionId)?.UserAccount.Translate(typeof(SecurityIdentifier));
                    if (securityIdentifier != null)
                    {
                        using (RegistryKey SecureKey = Registry.Users.OpenSubKey(securityIdentifier.ToString(), RegistryKeyPermissionCheck.ReadSubTree))
                        {
                            RegistryKey regKey = SecureKey?.OpenSubKey(subkeyDir, RegistryKeyPermissionCheck.ReadSubTree);
                            if (regKey != null)
                            {
                                FileName = (System.String)regKey.GetValue(null, System.String.Empty);
                                regKey?.Dispose();
                            }
                        }
                    }
                }
            }
            catch { }
            return FileName;
        }

        protected void PlaySound(System.String fileName)
        {
            try
            {
                if ((fileName ?? System.String.Empty) != System.String.Empty)
                {
                    /*using (System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer(FileName))
                    {
                        soundPlayer.Load();
                        soundPlayer.Play();
                    }*/
                    using (WaveOutEvent output = new WaveOutEvent())
                    using (AudioFileReader reader = new AudioFileReader(fileName))
                    {
                        output.Init(reader);
                        EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
                        output.PlaybackStopped += new EventHandler<StoppedEventArgs>((object sender, StoppedEventArgs args) => waitHandle.Set());
                        output.Play();
                        waitHandle.WaitOne();
                    }
                }
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public void WriteIfNotExists(System.String subkeyDir, System.String name, object value, int sessionId)
        {
            try
            {
                ITerminalServicesManager servicesManager = new TerminalServicesManager();
                using (ITerminalServer server = servicesManager.GetLocalServer())
                {
                    SecurityIdentifier securityIdentifier = (SecurityIdentifier)server?.GetSessions().DefaultIfEmpty(null).FirstOrDefault(p => p.SessionId == sessionId)?.UserAccount.Translate(typeof(SecurityIdentifier));
                    if (securityIdentifier != null)
                    {
                        using (RegistryKey SecureKey = Registry.Users.OpenSubKey(securityIdentifier.ToString(), RegistryKeyPermissionCheck.ReadSubTree))
                        {
                            RegistryKey regKey = SecureKey?.OpenSubKey(subkeyDir, RegistryKeyPermissionCheck.ReadWriteSubTree);
                            if (regKey != null)
                            {
                                regKey.SetValue(name, value);
                                regKey?.Dispose();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
            System.String FileName = System.String.Empty;
            try
            {
                Action uptSounds = new Action(() =>
                {
                    WriteIfNotExists(@"AppEvents\EventLabels\WindowsLogoff", "ExcludeFromCPL", 0, changeDescription.SessionId);
                    WriteIfNotExists(@"AppEvents\EventLabels\WindowsLogon", "ExcludeFromCPL", 0, changeDescription.SessionId);
                    WriteIfNotExists(@"AppEvents\EventLabels\WindowsUnlock", "ExcludeFromCPL", 0, changeDescription.SessionId);

                    this.LogOffFilePath = this.ReadReg(@"AppEvents\Schemes\Apps\.Default\WindowsLogoff\.Current", changeDescription.SessionId);
                    this.UnlockFilePath = this.ReadReg(@"AppEvents\Schemes\Apps\.Default\WindowsUnlock\.Current", changeDescription.SessionId);
                    this.LogOnFilePath = this.ReadReg(@"AppEvents\Schemes\Apps\.Default\WindowsLogon\.Current", changeDescription.SessionId);
                    this.SessionId = changeDescription.SessionId;
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
                this.PlaySound(FileName);
            }
            catch { }
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            WriteIfNotExists(@"AppEvents\EventLabels\SystemExit", "ExcludeFromCPL", 0, this.SessionId);
            this.PlaySound(ReadReg(@"AppEvents\Schemes\Apps\.Default\SystemExit\.Current", this.SessionId));
        }
    }
}
