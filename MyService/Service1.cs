using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.IO;
using System.Management;
using System.IO.Pipes;
using Microsoft.Win32;

namespace MyService
{
    /// <summary>
    /// 
    /// </summary>
    public partial class Service1 : ServiceBase
    {
        /// <summary>
        /// Флаг наличия ключа в системе
        /// </summary>
        bool noUSBkey = true;
        /// <summary>
        /// Действующий ключ
        /// </summary>
        string LastUSBFlashKey;
        /// <summary>
        /// Параметры лицензии
        /// </summary>
        List<string> MyParam;

        public Service1()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Вызваем процедуру проверки флеш накопителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            CheckUSB();
        }
        /// <summary>
        ///  Если изъят накопитель с ключем,
        ///  изменяем флаг наличие ключа в системе
        ///  и удаляем всю информацию о нем
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceDeletedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                if (property.Name == "DeviceID")
                {
                    if (parseSerialFromDeviceID(property.Value.ToString()) == LastUSBFlashKey)
                    {
                        noUSBkey = true;
                        LastUSBFlashKey = "";
                        MyParam.Clear();
                    }
                    break;
                }
            }

        }
        /// <summary>
        /// Отслеживаем подключение накопителей
        /// </summary>
        void USBInsert()
        {
            ManagementEventWatcher watcherRemove = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            watcherRemove.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            watcherRemove.Query = query;
            watcherRemove.Start();
            watcherRemove.WaitForNextEvent();
        }
        /// <summary>
        /// Отслеживаем изъятие накопителей
        /// </summary>
        void USBRemove()
        {
            ManagementEventWatcher watcherInsert = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            watcherInsert.EventArrived += new EventArrivedEventHandler(DeviceDeletedEvent);
            watcherInsert.Query = query;
            watcherInsert.Start();
            watcherInsert.WaitForNextEvent();
        }
        /// <summary>
        /// Запуск Тасков для отслеживания событий,
        /// связанных с подключением и изъятием флеш накопителей
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {

            Task TwatcherInsert = Task.Factory.StartNew(() =>
             {
                 USBInsert();
             });

            Task TwatcherRemove = Task.Factory.StartNew(() =>
            {
                USBRemove();
            });

        }
        /// <summary>
        /// 
        /// </summary>
        protected override void OnStop()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Проверяем Флеш накопитель на наличие ключа,
        /// запускаем программу по пути указанному в реестре,
        /// передаем программе по сокету данные лицензии
        /// </summary>
        private void CheckUSB()
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
                        try
                        {
                            DiskInfo MyUSB = new DiskInfo(parseSerialFromDeviceID(drive["PNPDeviceID"].ToString().Trim()));
                            if (File.Exists(disk["Name"].ToString().Trim() + "\\Licence.key"))
                            {
                                noUSBkey = false;
                                LastUSBFlashKey = parseSerialFromDeviceID(drive["PNPDeviceID"].ToString().Trim());

                                MyParam = MyUSB.CheckKey(disk["Name"].ToString().Trim() + "\\\\Licence.key");
                                try
                                {
                                    RegistryKey myKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\SNMP_Worker", false);
                                    String pathToProgram = (String)myKey.GetValue("PrgPath");
                                    // string pathToProgram = (string)Registry.GetValue(keyName, "PrgPath", null);
                                    if (pathToProgram == null)
                                        return;
                                    NativeMethods.LaunchProcess(@pathToProgram);
                                    Task.Factory.StartNew(() =>
                                    {
                                        PipeSecurity ps = new PipeSecurity();
                                        System.Security.Principal.SecurityIdentifier sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
                                        PipeAccessRule par = new PipeAccessRule(sid, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow);
                                        ps.AddAccessRule(par);
                                        while (!noUSBkey && MyParam != null)
                                        {
                                            using (NamedPipeServerStream pipe = new NamedPipeServerStream("KeyGuard", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None, 2048, 2048, ps))
                                            {

                                                pipe.WaitForConnection();
                                                StreamWriter writer = new StreamWriter(pipe);
                                                if (MyParam.Count > 1)
                                                {
                                                    TimeSpan ts = Convert.ToDateTime(MyParam[1]) - Convert.ToDateTime(DateTime.Now.ToShortDateString());
                                                    if(ts.Days < 1)
                                                        writer.WriteLine("Истекло время действия ключа!");
                                                    //                0type              1 User              2days               3 MAil           4 Pass          5  id                 6 secret  7 date
                                                    writer.WriteLine(MyParam[0] + "|" + MyParam[2] + "|" + ts.Days + "|" + MyParam[5] + "|" + MyParam[6] + "|" + MyParam[7] + "|" + MyParam[8] + "|" + MyParam[1]);
                                                }
                                                else
                                                    writer.WriteLine(MyParam[0]);
                                                writer.Flush();
                                            }
                                        }
                                    });
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
        }
    }
}
