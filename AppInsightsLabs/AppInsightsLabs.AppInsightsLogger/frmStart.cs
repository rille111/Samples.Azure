using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace AppInsightsLabs.AppInsightsLogger
{
    public partial class frmStart : Form
    {
        private Infrastructure.Logging.AppInsightsLogger _logger;

        public frmStart()
        {
            InitializeComponent();
        }

        private void frmStart_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            var config = JObject.Parse(File.ReadAllText("./log.config.json"));
            var instrKey = (string)config["AppInsightsInstrumentationKey"];
            _logger = new Infrastructure.Logging.AppInsightsLogger("instrKey");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss" + "> ");
            if (textBox1.Text == string.Empty)
            {
                textBox2.AppendText(timestamp  + "Nothing to trace!");
                return;
            }

            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    _logger.Debug(textBox1.Text);
                    textBox2.AppendText(timestamp + "Sent Debug trace!");
                    break;
                case 1:
                    _logger.Info(textBox1.Text);
                    textBox2.AppendText(timestamp + "Sent Info trace!");
                    break;
                case 2:
                    _logger.Warn(textBox1.Text);
                    textBox2.AppendText(timestamp + "Sent Warn trace!");
                    break;
                case 3:
                    _logger.Error(textBox1.Text);
                    textBox2.AppendText(timestamp + "Sent Error trace!");
                    break;
                case 4:
                    _logger.Fatal(textBox1.Text);
                    textBox2.AppendText(timestamp + "Sent Fatal trace!");
                    break;
                case 5:
                    _logger.CustomEvent(textBox1.Text);
                    textBox2.AppendText(timestamp + "Sent Custom Event!");
                    break;

                default:
                    break;
            }
            
        }
    }
}
