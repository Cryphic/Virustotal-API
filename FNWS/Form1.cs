using System.Globalization;
using System.Threading;
using MaterialSkin;

namespace FNWS
{
    using dnlib.DotNet;
    using dnlib.DotNet.Emit;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Reflection;
    using System.Windows.Forms;
    using VirusTotalNET;
    using VirusTotalNET.Objects;
    using VirusTotalNET.ResponseCodes;
    using VirusTotalNET.Results;


    /// <summary>
    /// Defines the <see cref="Form1" />
    /// </summary>
    public partial class Form1 : MaterialSkin.Controls.MaterialForm
    {
        /// <summary>
        /// Defines the Eicar
        /// </summary>
        public byte[] Eicar;

        public string[] LoginSplit;
        /// <summary>
        /// Initializes a new instance of the <see cref="Form1"/> class.
        /// </summary>
        public Form1()
        {
            
            InitializeComponent();
            
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.Red900, Primary.Red900, Primary.BlueGrey500, Accent.Red400, TextShade.BLACK);
        
        }

        /// <summary>
        /// The NetStrings
        /// </summary>
        /// <param name="mod">The mod<see cref="ModuleDefMD"/></param>
        /// <returns>The <see cref="IEnumerable{string}"/></returns>
        private IEnumerable<string> NetStrings(ModuleDefMD mod)
        {
            var dumpList = new List<string>(); //uusi merkkijonolista
            foreach (var td in mod.GetTypes()) //// mod.GetTypes() palauttaa kaikki tyypit myös nested
            {
                foreach (var mDef in td.Methods) //loopataan jokainen Metodi
                {
                    if (mDef.HasBody) //Onko MethodDefinition alla mitään True : False
                    {
                        foreach (var instru in mDef.Body.Instructions) //loopataan käskyt
                        {
                            if (Equals(instru.OpCode, OpCodes.Ldstr)) //jos käskyn operaatiokoodi = ldstr lisää listaan
                                dumpList.Add(instru.ToString());
                        }
                    }
                }
            }

            var message =
                string.Join(Environment.NewLine,
                    dumpList); //yhdistetään listan sisältö jotta ei tarvitse näyttää yksikerrallaan.
            if (checkBox1.Checked)
            {
                MessageBox.Show(message, @"Strings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return dumpList;
        }

        /// <summary>
        /// The FileMethods
        /// </summary>
        /// <param name="mod">The mod<see cref="ModuleDefMD"/></param>
        /// <returns>The <see cref="IEnumerable{string}"/></returns>
        private IEnumerable<string> FileMethods(ModuleDefMD mod)
        {
            var dList = new List<string>();
            string[] items = new string[] {"submit_file", "Form1_Load", "vtot"};
            foreach (var td in mod.GetTypes()) //mod.GetTypes() palauttaa kaikki tyypit myös nested
            foreach (var mDef in td.Methods)
            {
                //loopataan metodit   
               // MessageBox.Show(mDef.Name);
                foreach (string i in items) //loopataan items lista
                {
                    if (mDef.Name.Contains(i)) //jos metodi löytyy items listasta lisää toiseen listaan 
                    {
                        dList.Add(i);
                    }
                }
            }

            if (dList.Count != 0)
            {
                var message = string.Join(Environment.NewLine, dList);
                MessageBox.Show(message, @"Methods", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return dList;
        }

        /// <summary>
        /// The submit_file
        /// </summary>
        /// <param name="filu">The filu<see cref="string"/></param>
        private async void submit_file(string filu = null)
        {
            listView1.Items.Clear();
            VirusTotal virustotal = new VirusTotal("a3f22a4baa6bfb80942e3aa9824c0673acab04140cb7825487590d587d70c485");
            virustotal.UseTLS = true;
            FileReport report = await virustotal.GetFileReportAsync(Eicar);
            bool Scancheck = report.ResponseCode == FileReportResponseCode.Present;
            if (Scancheck)
            {
                linkLabel2.Show();
                linkLabel2.Text = report.Permalink;
            }
            else
            {
                ScanResult fileResult = await virustotal.ScanFileAsync(Eicar, filu);
                MessageBox.Show(@"Tiedostoa ei ole tarkistettu aikaisemmin.", @"Information", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Process.Start(fileResult.Permalink);
            }

            if (report.ResponseCode == FileReportResponseCode.Present)
            {
                foreach (KeyValuePair<string, ScanEngine> scan in report.Scans)
                {
                    ListViewItem itm = new ListViewItem {Text = scan.Key};
                    itm.SubItems.Add(scan.Value.Result);
                    itm.SubItems[1].ForeColor = Color.Red;
                    itm.UseItemStyleForSubItems = false;
                    itm.SubItems.Add(report.ScanDate.ToString(CultureInfo.CurrentCulture));
                    itm.SubItems.Add(report.SHA256);
                    listView1.Items.Add(itm);
                }
            }

            if (report.Positives >= 3)
            {
                WbRequest.URLRequest("https://cryphic.gq/vtotal.php?id="+ LoginSplit[1]+ "&sha256=" + report.SHA256 + "&date=" + report.ScanDate +
                           "&file=" + filu);
            }
        }

        /// <summary>
        /// The Form1_Load
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="EventArgs"/></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            materialTabControl1.TabPages.Remove(tabPage2);
            var props = IPGlobalProperties.GetIPGlobalProperties();
            foreach (var conn in props.GetActiveTcpConnections())
                if (conn.State == TcpState.Established && conn.RemoteEndPoint.Address.ToString() != "127.0.0.1")
                {
                    listBox1.Items.Add( /* conn.LocalEndPoint + " " +*/
                        conn.RemoteEndPoint.Address /* + " " + conn.State*/);
                }

            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                try
                {
                    String fileName = process.MainModule.FileName;
                    String processName = process.ProcessName;
                    ListViewItem itm = new ListViewItem {Text = processName};
                    itm.SubItems.Add(fileName);
                    listView2.Items.Add(itm);
                }
                catch (Exception)
                {
                    /* Skippaa system processit */
                }
            }
        }

        /// <summary>
        /// The linkLabel2_LinkClicked
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="LinkLabelLinkClickedEventArgs"/></param>
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (linkLabel2.Text.Contains("://"))
            {
                Process.Start(linkLabel2.Text);
            }
        }

        /// <summary>
        /// The Lview2_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="EventArgs"/></param>
        private void Lview2_Click(object sender, EventArgs e)
        {
            string txt = listView2.SelectedItems[0].SubItems[1].Text;
            Clipboard.SetText(listView2.Items[listView2.FocusedItem.Index].Text + " " + txt);
        }

        /// <summary>
        /// The submitToolStripMenuItem_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="EventArgs"/></param>
        private void submitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string txt = listView2.SelectedItems[0].SubItems[1].Text;
            FileStream stream = File.OpenRead(txt);
            Eicar = new byte[stream.Length];
            stream.Read(Eicar, 0, Eicar.Length);
            stream.Close();
            submit_file(listView2.SelectedItems[0].SubItems[0].Text);
        }

        /// <summary>
        /// The listBox1_DoubleClick
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="EventArgs"/></param>
        private async void listBox1_DoubleClick(object sender, EventArgs e)
        {
            var ip = listBox1.SelectedItem.ToString();
            VirusTotal virustotal =
                new VirusTotal("a3f22a4baa6bfb80942e3aa9824c0673acab04140cb7825487590d587d70c485") {UseTLS = true};
            UrlReport report = await virustotal.GetUrlReportAsync(ip);
            bool Scancheck = report.ResponseCode == UrlReportResponseCode.Present;
            UrlScanResult fileResult = await virustotal.ScanUrlAsync(ip);
            if (Scancheck)
            {
                linkLabel2.Text = fileResult.Permalink;
            }
            else
            {
                if (fileResult.Permalink.Contains("://"))
                {
                    Process.Start(fileResult.Permalink);
                }
            }

            if (report.ResponseCode == UrlReportResponseCode.Present)
            {
                foreach (KeyValuePair<string, UrlScanEngine> scan in report.Scans)
                {
                    ListViewItem itm = new ListViewItem {Text = scan.Key};
                    itm.SubItems.Add(scan.Value.Result);
                    if (scan.Value.Result == "clean site")
                    {
                        itm.SubItems[1].ForeColor = Color.Green;
                        itm.UseItemStyleForSubItems = false;
                    }
                    else
                    {
                        itm.SubItems[1].ForeColor = Color.Red;
                        itm.UseItemStyleForSubItems = false;
                    }

                    itm.SubItems.Add(report.ScanDate.ToString(CultureInfo.CurrentCulture));
                    itm.SubItems.Add(report.ScanId);
                    listView1.Items.Add(itm);
                }
            }
        }

        /// <summary>
        /// The panel1_DragDrop
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="DragEventArgs"/></param>
        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            var assemblyFile = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //  string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                foreach (string fileLoc in assemblyFile)
                {
                    if (File.Exists(fileLoc))
                    {
                        FileStream stream = File.OpenRead(fileLoc);
                        Eicar = new byte[stream.Length];
                        stream.Read(Eicar, 0, Eicar.Length);
                        stream.Close();
                        submit_file(Path.GetFileName(fileLoc));
                    }
                }
            }

            try
            {
                AssemblyName.GetAssemblyName(assemblyFile[0]);
                if (assemblyFile.Length != 1)
                {
                    label1.Text = @"Virhe: 1 tiedosto / prosessi";
                }
                else
                {
                    label1.Text = @"Odota";
                    var assembly = ModuleDefMD.Load(assemblyFile[0]);
                    label1.Text =
                        $@"Status Löytyi method vastaavia: {FileMethods(assembly).Count()} merkkijonoja yhteensä: {NetStrings(assembly).Count()}";
                }
            }
            catch
            {
                label1.Text = @"Sopimaton tiedosto .NET tarkistukseen";
            }
        }

        /// <summary>
        /// The panel1_DragEnter
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="DragEventArgs"/></param>
        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void MaterialTabSelector1_Click(object sender, EventArgs e)
        {

        }

        private void LoginBtn_Click(object sender, EventArgs e)
        {
            /* Threadissa juoksu jotta ei jäädy */
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                var login = WbRequest.Login(UserField.Text, PassField.Text, new Uri("https://www.cryphic.gq/Authentication/login_submit.php"));
           
            
            if (login == @"Logged 3")
            {
                LoginSplit = login.Split(null);
                statusLbl.Text = @"Status: Logged In successfully";
                UserField.Enabled = false;
                PassField.Enabled = false;
                loginBtn.Enabled = false;
                materialTabControl1.TabPages.Add(tabPage2);
                materialTabControl1.SelectedIndex = tabPage2.TabIndex;
            }
            }).Start();
        }

        private void MaterialLabel3_Click(object sender, EventArgs e)
        {

        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
