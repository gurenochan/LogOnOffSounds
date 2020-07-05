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

namespace LogOnOffSounds
{
    public partial class LogOnAndOffSounds : ServiceBase
    {
        public LogOnAndOffSounds() => InitializeComponent();

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
            System.String FileName = System.String.Empty;
            switch (changeDescription.Reason)
            {
                //case SessionChangeReason.SessionLogon:
                case SessionChangeReason.SessionUnlock:
                    FileName = "C:\\Windows\\Media\\Windows Logon.wav";
                    break;
                case SessionChangeReason.SessionLogoff:
                    FileName = "C:\\Windows\\Media\\Windows Logoff Sound.wav";
                    break;
            }
            if (FileName != System.String.Empty)
            {
                try
                {
                    using (System.Media.SoundPlayer soundPlayer = new System.Media.SoundPlayer(FileName))
                        soundPlayer.Play();
                }
                catch (Exception ex) { }
            }
        }
    }
}
