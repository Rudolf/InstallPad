using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CodeProject.Downloader;
using InstallPad;

namespace InstallPad
{

    public partial class ApplicationListItem
    {
        public enum InstallState
        {
            None, Downloading, Downloaded, Installing, Installed
        }
        InstallState state = InstallState.None;

        string installError = null;

        public string InstallError
        {
            get { return installError; }
            set { installError = value; }
        }

        public InstallState State
        {
            get { return state; }
            set { state = value; }
        }
        #region Properties
        /*public bool Downloading
        {
            get { return downloading; }
            set { downloading = value; }
        }

        public bool DownloadComplete
        {
            get { return downloadComplete; }
            set { downloadComplete = value; }
        }

        public bool Installing
        {
            get { return this.labelStatus.Text.Contains("Installing"); }
        }
        private bool installed = false;

        public bool Installed
        {
            get { return installed; }
        }*/
        #endregion

        #region Event we publish
        public event EventHandler FinishedDownloading;
        private void OnFinishedDownloading()
        {            
            SetState(InstallState.Downloaded);

            if (this.FinishedDownloading != null)
                FinishedDownloading(this, new EventArgs());

            if (installAfterDownload)
                InstallApplication();
        }
        public event EventHandler FinishedInstalling;
        private void OnFinishedInstalling()
        {            
            // Could be an error condition
            if (installError != null || (this.installProcess != null && this.installProcess.ExitCode != 0))
            {
                //this.labelStatus.Text = "Downloaded";
                SetState(InstallState.Downloaded);
            }
            else
            {
                SetState(InstallState.Installed);
                //this.labelStatus.Text = "Install finished";
                RunPostInstallationScript();
                
            }

            /*this.Invoke(new EventHandler(delegate
            {
                //if (this.installProcess != null && this.installProcess.ExitCode == 0)
                //{
                if (installError==null){
                    SetState(InstallState.Installed);

                    //this.labelStatus.Text = "Install finished";
                    RunPostInstallationScript();
                }
                else
                {
                    //this.labelStatus.Text = "Downloaded";
                    SetState(InstallState.Downloaded);
                }

                //MoveControlToTheLeftOf(labelStatus, this.Right);
                //SetInstalLinkText("Install");
            }));*/

            if (this.FinishedInstalling != null)
                FinishedInstalling(this, new EventArgs());
        }
        private void OnFinishedUnzipping(bool status)
        {
            //this.Invoke(new EventHandler(delegate
            //{
                if (status)
                {
                    //this.state = InstallState.Installed;
                    SetState(InstallState.Installed);
                    //this.labelStatus.Text = "Install finished";
                    RunPostInstallationScript();
                }
                else
                    SetState(InstallState.Downloaded);
                    //this.labelStatus.Text = "Downloaded";

                //MoveControlToTheLeftOf(labelStatus, this.Right);
                //SetInstalLinkText("Install");
            //}));
        }
        #endregion

        private void RunPostInstallationScript()
        {
            if (this.application.Options.PostInstallScript == null)
                return;
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = application.Options.PostInstallScript;
                Process p = Process.Start(info);
            }
            catch (Exception e)
            {
                this.installErrorBox.DetailsText =
                    String.Format("Error running the post install script {0} : {1}",
                        application.Options.PostInstallScript, e.Message);
                this.installErrorBox.Show();
            }
        }
        void downloader_DownloadComplete(object sender, EventArgs e)
        {
            // Update our appearance, let others know we're finished downloading.
            Debug.WriteLine("download complete.");
            if (this.state!=InstallState.Downloading)
                return;

            SetState(InstallState.Downloaded);
            
            this.Invoke(new EventHandler(delegate
            {
                /*this.progressBar.Visible = false;
                this.labelStatus.Visible = true;
                this.labelStatus.Text = "Downloaded";
                MoveControlToTheLeftOf(labelStatus, this.Right);*/
                
            }));

            this.OnFinishedDownloading();
        }

        // Determines whether installation should begin after the download
        bool installAfterDownload = false;

        public void Download(bool installAfterDownload)
        {
            this.installAfterDownload = installAfterDownload;
            // If we're already downloading this file, ignore this click
            if (this.state == InstallState.Downloaded || this.state==InstallState.Downloading)
                return;
            SetState(InstallState.Downloading);

           /* this.Invoke(new EventHandler(delegate
            {
                // Hide any previous errors we might have had
                this.downloadErrorBox.Visible = false;
                this.installErrorBox.Visible = false;

                // Update the progress bar with our progress
                this.progressBar.Visible = true;
            }));

            // Change "install" to "cancel"
            SetInstalLinkText("Cancel");*/

            ThreadPool.QueueUserWorkItem(new WaitCallback(this.AsyncDownload), null);
        }

        /// <summary>
        /// Begins an async download
        /// </summary>
        /// <param name="data"></param>
        private void AsyncDownload(object data)
        {
            Debug.WriteLine("starting an async download");
            Exception ex = null;
            try
            {
                // process alternate download locations, build a download list ordered by best location
                List<string> downloadUrlOrderedList = this.application.CreateOrderedUrlList();

                // download from one of the sources
                downloader.Download(downloadUrlOrderedList, InstallPadApp.Preferences.DownloadTo);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                ex = e;
            }
            catch (Exception e)
            {
                ex = e;
            }
            if (ex != null)
            {
                Debug.WriteLine("async download had error: " + ex.Message);
                this.Invoke(new EventHandler(delegate
                {
                    // Show an error
                    this.downloadErrorBox.Visible = true;
                    this.downloadErrorBox.DetailsText = ex.Message;
                    //this.progressBar.Visible = false;
                    //this.labelStatus.Visible = false;
                }));
                //this.state = InstallState.None;
                //SetInstalLinkText("Install");
                SetState(InstallState.None);
            }
            /*else
            {
                //this.state = InstallState.Downloaded;
                SetState(InstallState.Downloaded);
                if (installAfterDownload)
                    InstallApplication();
            }*/
            // Downloader_Complete event will trigger the state change to downloaded
        }

        /// <summary>
        /// Installs the application referred to by this app list item. 
        /// </summary>
        public void InstallApplication()
        {
            Debug.Assert(this.state == InstallState.Downloaded);

           /* this.Invoke(new EventHandler(delegate
            {
                this.labelStatus.Text = "Installing...";
                this.downloadErrorBox.Visible = false;
                this.installErrorBox.Visible = false;
                this.labelProgress.Hide();
            }));*/
            SetState(InstallState.Installing);

            if (downloader.DownloadingTo.ToLower().EndsWith(".zip"))
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.AsyncZipInstall), null);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.AsyncInstall), null);
            }
        }

        private static string ArgumentsForSilentInstall(ApplicationItem application)
        {
            // Coding in special rules here for apps. Each of these special rules should be moved
            // to another class, or a seperate file. Make sure there is a space before the command line arguments.

            // TODO: externalize this information. Put it in a .ini or .config file accessible by users?
            // Each rule should be on its own line; first, a regex to match the application name, and next to it,
            // the installer arguments that pertain to that app

            if (application.FileName.ToLower().Contains("firefox"))
                return "-ms";
            else if (application.FileName.ToLower().Contains("itunessetup"))
            {
                // Args are: /s /v"SILENT_INSTALL=1 ALLUSERS=1 /qb"
                return "/s /v\"SILENT_INSTALL=1 ALLUSERS=1 /qb\"";
            }
            else if (application.FileName.ToLower().Contains("spybot"))
            {
                return "/VERYSILENT";
            }
            else if (application.FileName.ToLower().Contains("adberdr"))
            {
                // Adobe Acrobat Reader. Argh adobe...
                return "/S /w /v\"/qb-! /norestart EULA_ACCEPT=YES\"";
            }
            else
                // This will work for most installers - /S for nullsoft installers, and -s for InstallShield
                return "/S -s";
        
        }

        private void AsyncZipInstall(object data)
        {
            Exception ex = null;
            try
            {
                Zip.Instance.ExtractZip(downloader.DownloadingTo, InstallPadApp.AppList.InstallationOptions.InstallationRoot);
            }
            catch (Exception e)
            {
                ex = e;
            }
            if (ex != null)
            {                
                // There was an error
                this.installProcess = null;
                //SetState(InstallState.Downloaded);
                this.installError = String.Format("Couldn't unzip {0} : {1}",
                        downloader.DownloadingTo, ex.Message);

                this.Invoke(new EventHandler(delegate
                {
                    // Show an error
                    installErrorBox.DetailsText = this.installError;
                    installErrorBox.Visible = true;
                }));
            }
            //else
                //SetState(InstallState.Installed);

            // Done unzipping/installing
            OnFinishedInstalling();
        }

        /// <summary>
        /// Begin an async install
        /// </summary>
        private void AsyncInstall(object data)
        {
            Debug.WriteLine("starting async install: ");
            ProcessStartInfo psi = new ProcessStartInfo(downloader.DownloadingTo);
            psi.Arguments = application.Options.InstallerArguments;
            if (application.Options.SilentInstall || InstallPadApp.AppList.InstallationOptions.SilentInstall)
            {
                psi.Arguments = String.Format("{0} {1}",psi.Arguments,ArgumentsForSilentInstall(application));
            }

            installProcess = new Process();

            installProcess.StartInfo = psi;
            installProcess.EnableRaisingEvents = true;
            installProcess.Exited += new EventHandler(process_Exited);

            Exception ex = null;
            try
            {
                installProcess.Start();
            }
            catch (Exception e)
            {
                ex = e;
            }
            if (ex != null)
            {
                // There was an error.
                this.installProcess = null;                
                this.installError = String.Format("Couldn't install {0} : {1}",downloader.DownloadingTo, ex.Message);

                this.Invoke(new EventHandler(delegate
                {
                    // Show an error
                    installErrorBox.DetailsText = this.installError;
                    installErrorBox.Visible = true;
                }));

                //SetInstalLinkText("Install");
                //SetState(InstallState.Downloaded);
                // We're done running the installer..
                OnFinishedInstalling();
            }

            // OnFinishedInstalling will get called by the process when it exits.
            


        }
        void downloader_ProgressChanged(object sender, CodeProject.Downloader.DownloadEventArgs e)
        {
            // Sometimes we may get events fired even after we're done downloading.
            // Don't react to them.
            if (this.state != InstallState.Downloading)
                return;
            this.Invoke(new EventHandler(delegate
            {
                if (e.PercentDone <= 100)
                {
                    this.progressBar.Value = e.PercentDone;
                    UpdateProgressLabel(e);
                }
            }));

        }


        void process_Exited(object sender, EventArgs e)
        {
            OnFinishedInstalling();
        }

    }
}
