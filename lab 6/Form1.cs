using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using System.Security.Policy;
using System.Management;
//using System.Management.Instrumentation;
using System.Security.Cryptography;
using System.IO;


namespace lab_6
{
    public partial class Form1 : Form
    {

        List<DiskInfo> DI;

        public Form1()
        {
            InitializeComponent();
        }
        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            GetUSBInfo();
            /*ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                Console.WriteLine(property.Name + " = " + property.Value);
            }*/
        }
        private void DeviceDeletedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                string s = (property.Name + " = " + property.Value);
            }
        }
        private string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private string Base64Decode(string base64EncodedData)
        {
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch(Exception ex)
            {
                return "0";
            }
        }

        void USBRemove()
        {
            ManagementEventWatcher watcherInsert = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            watcherInsert.EventArrived += new EventArrivedEventHandler(DeviceDeletedEvent);
            watcherInsert.Query = query;
            watcherInsert.Start();
            watcherInsert.WaitForNextEvent();
        }
        private string parseSerialFromDeviceID(string deviceId)
        {
            string[] splitDeviceId = deviceId.Split('\\');
            string[] serialArray;
            string serial;
            int arrayLen = splitDeviceId.Length - 1;

            serialArray = splitDeviceId[arrayLen].Split('&');
            serial = serialArray[0];

            return serial;
        }

        private string parseVenFromDeviceID(string deviceId)
        {
            string[] splitDeviceId = deviceId.Split('\\');
            string Ven;
            //Разбиваем строку на несколько частей. 
            //Каждая чаcть отделяется по символу &
            string[] splitVen = splitDeviceId[1].Split('&');

            Ven = splitVen[1].Replace("VEN_", "");
            Ven = Ven.Replace("_", " ");
            return Ven;
        }

        private string parseProdFromDeviceID(string deviceId)
        {
            string[] splitDeviceId = deviceId.Split('\\');
            string Prod;
            //Разбиваем строку на несколько частей. 
            //Каждая чаcть отделяется по символу &
            string[] splitProd = splitDeviceId[1].Split('&');

            Prod = splitProd[2].Replace("PROD_", ""); ;
            Prod = Prod.Replace("_", " ");
            return Prod;
        }

        private string parseRevFromDeviceID(string deviceId)
        {
            string[] splitDeviceId = deviceId.Split('\\');
            string Rev;
            //Разбиваем строку на несколько частей. 
            //Каждая чаcть отделяется по символу &
            string[] splitRev = splitDeviceId[1].Split('&');

            Rev = splitRev[3].Replace("REV_", ""); ;
            Rev = Rev.Replace("_", " ");
            return Rev;
        }

        private string garbageAdditor(string Hash)
        {
            Random rnd = new Random();
            for (int i = 0; i < Hash.Length; i += 2)
                Hash = Hash.Insert(i, Convert.ToChar(97 + rnd.Next(25)).ToString());
            return Hash;
        }

        private string garbageCollector(string garbagedHash)
        {
            int hashLength = garbagedHash.Length;
            for (int i = hashLength - 2; i >= 0; i -= 2)
                garbagedHash = garbagedHash.Remove(i, 1);
            return garbagedHash;
        }

        private void button1_Click(object sender, EventArgs e)
        {
           if (DI.Count != 0)
            {
                string[] Properties = new string[5];
                Properties[0] = PermitionsBox.Text;
                Properties[1] = dateTimePicker1.Value.ToShortDateString();

                Properties[2] = Environment.UserName;
                Properties[3] = Environment.MachineName;
                Properties[4] = Environment.OSVersion.VersionString;

                richTextBox1.AppendText(DI[DiskBox.SelectedIndex].CreateKey(Properties) + "\n");
            }
           /*  string g = Base64Encode(Base64Encode("qwerty"));
            richTextBox1.AppendText(g+"\n");
            richTextBox1.AppendText(Base64Decode(Base64Decode(g)));*/

        }
        private void ReadUSBFlashDrivers(Dictionary<string, string> comboSource)
        {
            string diskName = string.Empty;
            
            //Получение списка накопителей подключенных через интерфейс USB
            foreach (System.Management.ManagementObject drive in
                      new System.Management.ManagementObjectSearcher(
                       "select * from Win32_DiskDrive where InterfaceType='USB'").Get())
            {
                //Получаем букву накопителя
                foreach (System.Management.ManagementObject partition in
                new System.Management.ManagementObjectSearcher(
                    "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + drive["DeviceID"]
                      + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get())
                {
                    foreach (System.Management.ManagementObject disk in
                 new System.Management.ManagementObjectSearcher(
                        "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='"
                          + partition["DeviceID"]
                          + "'} WHERE AssocClass = Win32_LogicalDiskToPartition").Get())
                    {
                        //Получение буквы устройства
                        diskName = disk["Name"].ToString().Trim();
                        //listBox1.Items.Add("Буква накопителя=" + diskName);
                    }
                }               
                decimal dSize = Math.Round((Convert.ToDecimal(
              new System.Management.ManagementObject("Win32_LogicalDisk.DeviceID='"
                      + diskName + "'")["Size"]) / 1073741824), 2);
                comboSource.Add(DI.Count.ToString(), diskName + drive["Model"].ToString().Trim() +" (" + dSize.ToString()+") GB");
                DI.Add(new DiskInfo(diskName, drive["Model"].ToString().Trim(), dSize, parseSerialFromDeviceID(drive["PNPDeviceID"].ToString().Trim())));
            }

        }

        private void GetUSBInfo()
        {
            string diskName = string.Empty;
            string Y = "";
            ManagementObjectCollection tre = new ManagementObjectSearcher(
                      "select * from Win32_DiskDrive where InterfaceType='USB'").Get();
            //Получение списка накопителей подключенных через интерфейс USB
            foreach (System.Management.ManagementObject drive in
                      new System.Management.ManagementObjectSearcher(
                       "select * from Win32_DiskDrive where InterfaceType='USB'").Get())
            {
                //Получаем букву накопителя
                foreach (System.Management.ManagementObject partition in
                new System.Management.ManagementObjectSearcher(
                    "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + drive["DeviceID"]
                      + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get())
                {
                    foreach (System.Management.ManagementObject disk in
                 new System.Management.ManagementObjectSearcher(
                        "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='"
                          + partition["DeviceID"]
                          + "'} WHERE AssocClass = Win32_LogicalDiskToPartition").Get())
                    {
                        //Получение буквы устройства
                        diskName = disk["Name"].ToString().Trim();
                        Y += "Буква накопителя=" + diskName;
                        //listBox1.Items.Add("Буква накопителя=" + diskName);
                    }
                }
                //Получение модели устройства
                Y += "Модель=" + drive["Model"];

                //Получение Ven устройства
                Y += "Ven=" +
                 parseVenFromDeviceID(drive["PNPDeviceID"].ToString().Trim());

                //Получение Prod устройства
                Y += "Prod=" +
                 parseProdFromDeviceID(drive["PNPDeviceID"].ToString().Trim());

                //Получение Rev устройства
                Y += "Rev=" +
                 parseRevFromDeviceID(drive["PNPDeviceID"].ToString().Trim());
                Y += "Серийный номер=" + parseSerialFromDeviceID(drive["PNPDeviceID"].ToString().Trim());
                //Получение объема устройства в гигабайтах
                decimal dSize = Math.Round((Convert.ToDecimal(
              new System.Management.ManagementObject("Win32_LogicalDisk.DeviceID='"
                      + diskName + "'")["Size"]) / 1073741824), 2);
                Y += "Полный объем=" + dSize + " gb";

                //Получение свободного места на устройстве в гигабайтах
                decimal dFree = Math.Round((Convert.ToDecimal(
              new System.Management.ManagementObject("Win32_LogicalDisk.DeviceID='"
                      + diskName + "'")["FreeSpace"]) / 1073741824), 2);
                Y += "Свободный объем=" + dFree + " gb";

                //Получение использованного места на устройстве
                decimal dUsed = dSize - dFree;
                Y += "Используемый объем=" + dUsed + " gb";
                Y += "\n";
            }
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            DI = new List<DiskInfo>();
            DiskBox.DisplayMember = "Value";
            DiskBox.ValueMember = "Key";
            Dictionary<string, string> comboSource = new Dictionary<string, string>();
            ReadUSBFlashDrivers(comboSource);
            DiskBox.DataSource = new BindingSource(comboSource, null);


            /*  Task TwatcherRemove = Task.Factory.StartNew(() =>
              {
                  USBRemove();
              });*/
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog1.Filter = "Licence file (*.key)|*.key|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (openFileDialog1.OpenFile() != null)
                    {
                        DiskInfo f = DI.Select(x => { string.Compare(x.Letter, openFileDialog1.FileName.Remove(openFileDialog1.FileName.IndexOf(':'))); return x; }).ToList()[0];
                        string yt = f.CheckKey(openFileDialog1.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }
    }
}
