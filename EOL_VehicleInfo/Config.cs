using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace EOL_VehicleInfo {
    public class Config {
        public struct SerialPortConfig {
            public string PortName { get; set; }
            public int BaudRate { get; set; }
            public int Parity { get; set; }
            public int DataBits { get; set; }
            public int StopBits { get; set; }
        }

        public struct DBConfig {
            public string IP { get; set; }
            public string Port { get; set; }
            public string UserID { get; set; }
            public string Pwd { get; set; }
            public string DBName { get; set; }
        }

        public SerialPortConfig Serial;
        public DBConfig DB;
        public List<string> VehicleTypeList;
        readonly Logger Log;
        string ConfigFile { get; set; }

        public Config(Logger Log, string strConfigFile = "./config/config.xml") {
            this.Log = Log;
            this.ConfigFile = strConfigFile;
            LoadConfig();
        }

        ~Config() {
            SaveConfig();
        }

        void LoadConfig() {
            try {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigFile);
                XmlNode xnRoot = xmlDoc.SelectSingleNode("Config");
                XmlNodeList xnl = xnRoot.ChildNodes;

                foreach (XmlNode node in xnl) {
                    XmlNodeList xnlChildren = node.ChildNodes;
                    if (node.Name == "SerialPort") {
                        foreach (XmlNode item in xnlChildren) {
                            if (item.Name == "PortName") {
                                Serial.PortName = item.InnerText;
                            } else if (item.Name == "BaudRate") {
                                int.TryParse(item.InnerText, out int result);
                                Serial.BaudRate = result;
                            } else if (item.Name == "Parity") {
                                int.TryParse(item.InnerText, out int result);
                                Serial.Parity = result;
                            } else if (item.Name == "DataBits") {
                                int.TryParse(item.InnerText, out int result);
                                Serial.DataBits = result;
                            } else if (item.Name == "StopBits") {
                                int.TryParse(item.InnerText, out int result);
                                Serial.StopBits = result;
                            }
                        }
                    } else if (node.Name == "DB") {
                        foreach (XmlNode item in xnlChildren) {
                            if (item.Name == "IP") {
                                DB.IP = item.InnerText;
                            } else if (item.Name == "Port") {
                                DB.Port = item.InnerText;
                            } else if (item.Name == "UserID") {
                                DB.UserID = item.InnerText;
                            } else if (item.Name == "Pwd") {
                                DB.Pwd = item.InnerText;
                            } else if (item.Name == "DBName") {
                                DB.DBName = item.InnerText;
                            }
                        }
                    } else if (node.Name == "VehicleTypeList") {
                        VehicleTypeList = new List<string>(node.InnerText.Split(','));
                    }
                }
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
        }

        public void SaveConfig() {
            try {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ConfigFile);
                XmlNode xnRoot = xmlDoc.SelectSingleNode("Config");
                XmlNodeList xnl = xnRoot.ChildNodes;

                foreach (XmlNode node in xnl) {
                    XmlNodeList xnlChildren = node.ChildNodes;
                    // 只操作了需要被修改的配置项
                    if (node.Name == "VehicleTypeList") {
                        node.InnerText = string.Join(",", VehicleTypeList.ToArray());
                    }
                }

                xmlDoc.Save(ConfigFile);
            } catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: " + e.Message);
                Console.ResetColor();
                Log.TraceError(e.Message);
            }
        }

    }
}
