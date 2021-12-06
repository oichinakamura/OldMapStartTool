using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace OldMapStarter
{
    public class StartupConfig : XmlDocument
    {
        public bool BUpdate { get; set; } = false;

        public StartupConfig(string sDirectory)
        {
            localPath = sDirectory;
        }

        public void LocalLoad() => Load(System.IO.Path.Combine(localPath, "StartupConfig.xml"));
        public void ServerLoad() => Load(System.IO.Path.Combine(ServerPath, "StartupConfig.xml"));

        public bool LocalConfigExists() => System.IO.File.Exists(System.IO.Path.Combine(localPath, "StartupConfig.xml"));
        public bool ServerConfigExists() => System.IO.File.Exists(System.IO.Path.Combine(ServerPath, "StartupConfig.xml"));

        private DateTime? localConfigLastWrite;
        public DateTime LocalConfigLastWrite
        {
            get
            {
                if (!localConfigLastWrite.HasValue)
                    localConfigLastWrite = new System.IO.FileInfo(System.IO.Path.Combine(Localpath, "StartupConfig.xml")).LastWriteTime;

                return localConfigLastWrite.Value;
            }
        }
        private DateTime? serverConfigLastWrite;
        public DateTime ServerConfigLastWrite
        {
            get
            {
                if (!serverConfigLastWrite.HasValue)
                    serverConfigLastWrite = new System.IO.FileInfo(System.IO.Path.Combine(ServerPath, "StartupConfig.xml")).LastWriteTime;

                return serverConfigLastWrite.Value;
            }
        }

        public ProgressView ProgBar { get; set; }

        public int CompareConfigFile()
        {
            if (ServerConfigLastWrite > LocalConfigLastWrite)
            {
                return -1;
            }
            return 0;
        }

        public void LocalSave() => Save(System.IO.Path.Combine(localPath, "StartupConfig.xml"));

        public void UpdateFiles(ListBox list)
        {
            foreach (XmlElement child in DocumentElement.ChildNodes)
            {
                if (child is XmlFileElement fileElement)
                {
                    fileElement.Compare(localPath, ServerPath, list);
                }
            }
        }

        public void Execute()
        {
            var sFilename = DocumentElement.GetAttribute("Exec");
            if (System.IO.File.Exists(System.IO.Path.Combine(Localpath, sFilename)))
            {
                Process.Start(System.IO.Path.Combine(Localpath, sFilename));
            }
        }

        private string localPath;
        public string Localpath => localPath;
        public string ServerPath
        {
            get
            {
                if (DocumentElement is XmlElement root)
                    return root.GetAttribute("ServerPath");
                else
                    return "X:\\";
            }
        }

        public override XmlElement CreateElement(string prefix, string localName, string namespaceURI)
        {
            switch (localName)
            {
                case "Directory": return new XmlDirectory(prefix, localName, namespaceURI, this);
                case "File": return new XmlFile(prefix, localName, namespaceURI, this);
            }
            return base.CreateElement(prefix, localName, namespaceURI);
        }
    }

    public abstract class XmlFileElement : XmlElement
    {
        protected internal XmlFileElement(string prefix, string localName, string namespaceURI, XmlDocument doc) : base(prefix, localName, namespaceURI, doc)
        {
        }
        public abstract void Compare(string localPath, string serverPath, ListBox list);

        public ProgressView ProgBar => (OwnerDocument as StartupConfig).ProgBar;
    }

    public class XmlDirectory : XmlFileElement
    {
        protected internal XmlDirectory(string prefix, string localName, string namespaceURI, XmlDocument doc) : base(prefix, localName, namespaceURI, doc)
        {
        }

        public override void Compare(string localPath, string serverPath, ListBox list)
        {
            var thisName = System.IO.Path.Combine(localPath, GetAttribute("Name"));
            var serverThisName = System.IO.Path.Combine(serverPath, GetAttribute("Name"));
            if (!System.IO.Directory.Exists(serverThisName))
            {
                System.Windows.MessageBox.Show($"サーバーに{serverThisName}がありません。", "サーバーの確認", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Environment.Exit(0);
            }
            else if (!System.IO.Directory.Exists(thisName))
            {
                System.IO.Directory.CreateDirectory(thisName);
                list.Items.Add($"{localPath}を作成しました。");
            }

            foreach (XmlElement child in ChildNodes)
            {
                if (child is XmlFileElement fileElement)
                {
                    fileElement.Compare(thisName, serverThisName, list);
                }
            }

        }

    }

    public class XmlFile : XmlFileElement
    {
        protected internal XmlFile(string prefix, string localName, string namespaceURI, XmlDocument doc) : base(prefix, localName, namespaceURI, doc)
        {
        }
        private string thisName;
        public override void Compare(string localPath, string serverPath, ListBox list)
        {
            thisName = GetAttribute("Name");
            var localName = System.IO.Path.Combine(localPath, thisName);
            var serverThisName = System.IO.Path.Combine(serverPath, thisName);
            if (!System.IO.File.Exists(serverThisName))
            {
                System.Windows.MessageBox.Show($"サーバーに{serverThisName}がありません。", "サーバーの確認", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Environment.Exit(0);
            }
            else if (!System.IO.File.Exists(localName))
            {
                try
                {
                    ProgBar.FileName = thisName;
                    var copy = new CopyFileProgress();
                    copy.ProgressChanged += Copy_ProgressChanged;
                    var result = copy.CopyStart(serverThisName, localName, true);

                    list.Items.Add($"{thisName}をコピーしました。");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Environment.Exit(-1);
                }
            }
            else if (new System.IO.FileInfo(serverThisName).LastWriteTime > new System.IO.FileInfo(localName).LastWriteTime)
            {
                try
                {
                    ProgBar.FileName = thisName;
                    var copy = new CopyFileProgress();
                    copy.ProgressChanged += Copy_ProgressChanged;
                    var result = copy.CopyStart(serverThisName, localName, true);
                    list.Items.Add($"{thisName}をコピーしました。");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Environment.Exit(-1);
                }
            }
        }

        private void Copy_ProgressChanged(object s, CopyFileProgress.CopyProgressEventArgs e)
        {
            var progresspar = e.TotalFileSize > 0 ? ((decimal)e.TotalBytesTransferred / (decimal)e.TotalFileSize) * (decimal)100 : (decimal)0;
            this.ProgBar.FileName = $"{thisName}({(int)progresspar}%)";
            this.ProgBar.Value = (int)progresspar;
        }
    }
}
