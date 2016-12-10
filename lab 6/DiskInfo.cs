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
        string _PID;
        string _VID;

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
            catch (Exception ex)
            {
                return "0";
            }
        }

        private string GetMd5Hash(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
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
        public DiskInfo(string Letter, string Model, decimal DiskSize, string DeviceID)
        {
            _Letter = Letter;
            _Model = Model;
            _DiskSize = DiskSize;
            _SerialNumber = DeviceID;
        }
        public string CreateKey(string[] Properties)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(_Letter + "\\Licence.key", FileMode.Create)))
            {
                using (MD5 md5Hash = MD5.Create())
                {
                    //writer.Write(GetMd5Hash(md5Hash, _SerialNumber));
                    writer.Write(Base64Encode(Base64Encode(garbageAdditor(_SerialNumber))));
                    foreach (string item in Properties)
                    {
                        writer.Write(Base64Encode(Base64Encode(garbageAdditor(item))));
                    }
                   // writer.Write("\n");
                   // writer.Write(GetMd5Hash(md5Hash, _SerialNumber));
                    //File.WriteAllText(_Letter + "\\Mylicense.key", GetMd5Hash(md5Hash, _SerialNumber));
                    //return GetMd5Hash(md5Hash, _SerialNumber);
                }
            }
            return "OK";
        }
        public string CheckKey(string Path)
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

                }
            }
            return "NO";
        }
        public string Letter
        {
            get { return _Letter; }
        }

    }
}
