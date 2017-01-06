using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace lab_6
{
    class DiskInfo
    {
        string _Letter;
        string _Model;
        decimal _DiskSize;
        string _SerialNumber;
        /// <summary>
        /// Кодирование строки
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns>закодированная информация</returns>
        private string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        /// <summary>
        /// Декодирование Base64
        /// </summary>
        /// <param name="base64EncodedData"></param>
        /// <returns>раскодированная информация</returns>
        private string Base64Decode(string base64EncodedData)
        {
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (Exception ex)
            {
                return "0";
            }
        }
        /// <summary>
        /// Разбавление мусором данных
        /// </summary>
        /// <param name="Hash">Строка для разбавления</param>
        /// <returns></returns>
        private string garbageAdditor(string Hash)
        {
            Random rnd = new Random();
            for (int i = 0; i < Hash.Length; i += 2)
                Hash = Hash.Insert(i, Convert.ToChar(97 + rnd.Next(25)).ToString());
            return Hash;
        }
        /// <summary>
        /// Сбор мусора из строки
        /// </summary>
        /// <param name="garbagedHash">Строка с мусором</param>
        /// <returns></returns>
        private string garbageCollector(string garbagedHash)
        {
            int hashLength = garbagedHash.Length;
            for (int i = hashLength - 2; i >= 0; i -= 2)
                garbagedHash = garbagedHash.Remove(i, 1);
            return garbagedHash;
        }
        public DiskInfo(string Letter, string Model, decimal DiskSize, string DeviceID)
        {
            _Letter = Letter;
            _Model = Model;
            _DiskSize = DiskSize;
            _SerialNumber = DeviceID;
        }
        /// <summary>
        /// Создание файла, ключа-лицензии на флеш накопителе
        /// </summary>
        /// <param name="Properties">Параметры</param>
        /// <returns></returns>
        public bool CreateKey(string[] Properties)
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(_Letter + "\\Licence.key", FileMode.Create)))
                {
                    writer.Write(Base64Encode(Base64Encode(garbageAdditor(_SerialNumber))));
                    foreach (string item in Properties)
                    {
                        writer.Write(Base64Encode(Base64Encode(garbageAdditor(item))));
                    }
                }

                return true;
            }
            finally
            {
                   
            }
            return false;
        }
        /// <summary>
        /// Проверка целостности ключа
        /// </summary>
        /// <param name="Path">Путь к ключу</param>
        /// <returns>Список параметров или информацию, почему ключ не действителен</returns>
        public List<string> CheckKey(string Path)
        {
            if (File.Exists(Path))
            {
                using (BinaryReader reader = new BinaryReader(File.OpenRead(Path)))
                {
                    List<string> Params = new List<string>();
                    if (garbageCollector(Base64Decode(Base64Decode(reader.ReadString()))) == _SerialNumber)
                        while(reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            Params.Add(garbageCollector(Base64Decode(Base64Decode(reader.ReadString()))));
                        }
                    return Params;
                }
            }
            return null;
        }
        public string Letter
        {
            get { return _Letter; }
        }

    }
}
