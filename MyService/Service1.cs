using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Management;
using System.IO.Pipes;
using System.Windows.Forms;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Data.OleDb;

namespace MyService
{
    public partial class Service1 : ServiceBase
    {

        string strAccessConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\\Users\\Павел\\Documents\\Visual Studio 2015\\Projects\\lab 6\\MyService\\bin\\Debug\\log.mdb";
        OleDbConnection myAccessConn;
        

        public Service1()
        {
            InitializeComponent();
        }
        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            File.AppendAllText(@"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab 6\MyService\bin\Debug\1.txt", "Insert USB\n");
            CheckUSB();
            //GetUSBInfo();
            //lab_6.DiskInfo DI =
        }

        private void DeviceDeletedEvent(object sender, EventArrivedEventArgs e)
        {
            /*File.AppendAllText(@"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab 6\MyService\bin\Debug\1.txt", "Remove USB\n");
            GetUSBInfo();*/
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                File.AppendAllText(@"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab 6\MyService\bin\Debug\1.txt", property.Name + " = " + property.Value);
            }
            
        }

        void DataBaseConnect()
        {
            try
            {
                myAccessConn = new OleDbConnection(strAccessConn);
              //  myAccessConn.DataSource = @"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab 6\MyService\bin\Debug\log.mdb"
                myAccessConn.Open();
            }
            catch(Exception ex)
            {
                File.AppendAllText(@"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab 6\MyService\bin\Debug\1.txt", ex.Message);
            }
        }

        void USBInsert()
        {
            ManagementEventWatcher watcherRemove = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            watcherRemove.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            watcherRemove.Query = query;
            watcherRemove.Start();
            watcherRemove.WaitForNextEvent();
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
        
        protected override void OnStart(string[] args)
         {
            File.AppendAllText(@"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab 6\MyService\bin\Debug\1.txt", "Start \n");
           
            Task TwatcherInsert= Task.Factory.StartNew(() =>
            {
                
                USBInsert();
            });

            Task TwatcherRemove = Task.Factory.StartNew(() =>
            {
                USBRemove();
            });

        }

        protected override void OnStop()
        {

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

        private void CheckUSB()
        {
            string diskName = string.Empty;
            string Y = "";
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
                        try
                        {
                            DiskInfo MyUSB = new DiskInfo(parseSerialFromDeviceID(drive["PNPDeviceID"].ToString().Trim()));
                            DataBaseConnect();
                            if (File.Exists(disk["Name"].ToString().Trim()+ "\\Licence.key"))
                            {
                                string strAccessInsert = string.Format("INSERT INTO Events(NameEvent, NameUSB, SerialNumberUSB, KeyExist, EventDate) VALUES(\"{0}\",\"{1}\",\"{2}\", \"{3}\", \"{4}\")","Insert USB", drive["Model"], parseSerialFromDeviceID(drive["PNPDeviceID"].ToString().Trim()), "YES",  DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                                OleDbCommand cmd = new OleDbCommand(strAccessInsert, myAccessConn);
                                cmd.ExecuteNonQuery();
                                List<string> MyParam = MyUSB.CheckKey(disk["Name"].ToString().Trim() + "\\\\Licence.key");
                                try
                                {
                                    if (MyParam.Count > 3)
                                    {
                                        NativeMethods.LaunchProcess(@"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab3\lab3\bin\Debug\lab3.exe");
                                         Task.Factory.StartNew(() =>
                                         {
                                             PipeSecurity ps = new PipeSecurity();
                                             System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
                                             PipeAccessRule par = new PipeAccessRule(sid, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
                                             ps.AddAccessRule(par);
                                             using (NamedPipeServerStream pipe = new NamedPipeServerStream("KeyBot", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None, 2048, 2048, ps))
                                             {
                                                 
                                                 pipe.WaitForConnection();

                                                 StreamWriter writer = new StreamWriter(pipe);
                                                 TimeSpan ts = Convert.ToDateTime(MyParam[1]) - Convert.ToDateTime(DateTime.Now.ToShortDateString());
                                                 writer.WriteLine(MyParam[0] + "|" + MyParam[2]+"|"+ts.Days);
                                                 writer.Flush();
                                                
                                             }                                            
                                         });
                                        
                                    }
                                }
                                catch (Exception ex)
                                {
                                      File.AppendAllText(@"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab 6\MyService\bin\Debug\1.txt", ex.Message);
                                }
                            }
                            else
                            {
                                string strAccessInsert = string.Format("INSERT INTO Events(NameEvent, NameUSB, SerialNumberUSB, KeyExist, EventDate) VALUES(\"{0}\",\"{1}\",\"{2}\", \"{3}\", \"{4}\")", "Insert USB", drive["Model"], parseSerialFromDeviceID(drive["PNPDeviceID"].ToString().Trim()), "NO", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                                OleDbCommand cmd = new OleDbCommand(strAccessInsert, myAccessConn);
                                cmd.ExecuteNonQuery();
                            }                       
                        }
                        catch(Exception ex)
                        {
                            File.AppendAllText(@"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab 6\MyService\bin\Debug\1.txt", ex.Message);
                        }
                    }
                }
            }
        }

        private void GetUSBInfo()
        {
            string diskName = string.Empty;
            string Y = "";

           // ManagementObjectCollection tre = new ManagementObjectSearcher(
                   //    "select * from Win32_DiskDrive where InterfaceType='USB'").Get();
           // string = tre.g["DeviceID"];
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
                File.AppendAllText(@"C:\Users\Павел\Documents\Visual Studio 2015\Projects\lab 6\MyService\bin\Debug\1.txt", Y+"\n");
            }
        }


    }
}
