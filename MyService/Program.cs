using System;
using System.Collections;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
//using System.Collections.Hashtable;
using System.Text;

namespace MyService
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        /// 
        [STAThread]
        static void Main(string[] args = null)
        {
            ServiceBase[] ServicesToRun;
            /// <summary>
            /// Проверка на входные параметры, для инсталяции или деинсталяции
            /// </summary>
            /// 
            if (args.Length != 0)
            {
                if (System.Environment.UserInteractive)
                {
                    try
                    {
                        if (args.Length > 0)
                            switch (args[0])
                            {
                                case "-install":
                                    {
                                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                                        break;
                                    }
                                case "-uninstall":
                                    {
                                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                                        break;
                                    }
                                default: break;
                            }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            else
            {
                ServicesToRun = new ServiceBase[] { new Service1() };
                ServiceBase.Run(ServicesToRun);
            }
            
        }
    }
}
