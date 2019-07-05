using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EOL_VehicleInfo {
    public partial class Form1 : Form {
        Logger Log;
        Config Cfg;
        Model DB;
        SerialPortClass Serial;
        MainFileVersion FileVer;
        BindingSource bs;

        public Form1() {
            InitializeComponent();
            Log = new Logger("./log", EnumLogLevel.LogLevelAll, true, 100);
            Cfg = new Config(Log);
            DB = new Model(Cfg, Log);
            if (Cfg.Serial.PortName != "") {
                Serial = new SerialPortClass(
                    Cfg.Serial.PortName,
                    Cfg.Serial.BaudRate,
                    (Parity)Cfg.Serial.Parity,
                    Cfg.Serial.DataBits,
                    (StopBits)Cfg.Serial.StopBits
                );
                Serial.DataReceived += new SerialPortClass.SerialPortDataReceiveEventArgs(SerialDataReceived);
                try {
                    Serial.OpenPort();
                } catch (Exception e) {
                    Log.TraceFatal(e.Message);
                    MessageBox.Show(e.Message + "\n请检查串口配置！", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            FileVer = new MainFileVersion();
            this.labelVer.Text = "Ver: " + FileVer.AssemblyVersion.ToString();
            bs = new BindingSource {
                DataSource = Cfg.VehicleTypeList
            };
            this.comboBox1.DataSource = bs;
        }

        void SerialDataReceived(object sender, SerialDataReceivedEventArgs e, byte[] bits) {
            Control con = this.ActiveControl;
            if (con is TextBox txt && con.Name == "textBoxVIN") {
                // 跨UI线程调用UI控件要使用Invoke，更新UI的字符串长度不能大于21
                this.BeginInvoke((EventHandler)delegate {
                    txt.Text = Encoding.Default.GetString(bits).Trim();
                    //if (bits.Contains<byte>(0x0d) || bits.Contains<byte>(0x0a)) {
                    //    this.labelStatus.Text = "等待输入车型代码";
                    //}
                });
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
            if (Serial != null) {
                Serial.ClosePort();
            }
        }

        private void textBoxVIN_TextChanged(object sender, EventArgs e) {
            TextBox tb = sender as TextBox;
            if (tb.Name == "textBoxVIN") {
                this.labelStatus.Text = "等待输入车型代码";
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            string[] args = new string[2];
            string strVIN = this.textBoxVIN.Text;
            string strVehicleType = this.comboBox1.Text;
            if (strVIN.Length > 17) {
                MessageBox.Show("VIN号大于17位！请修改后重试。", "输入出错");
                return;
            } else if (strVIN.Length < 17) {
                MessageBox.Show("VIN号小于17位！请修改后重试。", "输入出错");
                return;
            }
            if (strVehicleType == "") {
                MessageBox.Show("车型代码不能为空！", "输入出错");
                return;
            }
            if (!Cfg.VehicleTypeList.Contains(strVehicleType)) {
                Cfg.VehicleTypeList.Add(strVehicleType);
                Cfg.SaveConfig();
                bs.ResetBindings(false);
                this.comboBox1.SelectedItem = strVehicleType;
            }
            args[0] = strVIN;
            args[1] = strVehicleType;
            Task.Factory.StartNew((Action<object>)DoWork, (object)args);
        }

        private void DoWork(object obj) {
            const string strTableName = "VehicleInfo";
            if (obj is string[] args) {
                int ret = 0;
                string strVIN = args[0];
                string strVehicleType = args[1];
                string[,] rows = DB.GetRecords(strTableName, "VIN", strVIN);
                if (rows == null) {
                    this.BeginInvoke((EventHandler)delegate {
                        this.labelStatus.Text = "连接数据库失败";
                    });
                    return;
                }
                Dictionary<string, string> dicInfo = new Dictionary<string, string> {
                    { "VIN", strVIN },
                    { "VehicleType", strVehicleType}
                };
                if (rows != null && rows.GetLength(0) > 0) {
                    KeyValuePair<string, string> pairWhere = new KeyValuePair<string, string>("VIN", strVIN);
                    ret = DB.UpdateRecord(strTableName, pairWhere, dicInfo);
                } else {
                    ret = DB.InsertRecord(strTableName, dicInfo);
                }
                this.BeginInvoke((EventHandler)delegate {
                    if (ret == 1) {
                        this.labelStatus.Text = "输入成功";
                    } else if (ret == 0) {
                        this.labelStatus.Text = "输入失败";
                    } else {
                        this.labelStatus.Text = "输入结果未知";
                    }
                });
            }
        }
    }

    // 获取文件版本类
    public class MainFileVersion {
        public Version AssemblyVersion {
            get { return ((Assembly.GetEntryAssembly()).GetName()).Version; }
        }

        public Version AssemblyFileVersion {
            get { return new Version(FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).FileVersion); }
        }

        public string AssemblyInformationalVersion {
            get { return FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion; }
        }
    }

}
