using System.Windows;

namespace ORS_ER.windows
{
    /// <summary>
    /// Interaction logic for LogicWindow.xaml
    /// </summary>
    public partial class LogicWindow : Window
    {
        public string op = "==";
        public string name = "";
        public LogicWindow(string Name, string operation, string type)
        {
            InitializeComponent();
            this.name = Name;
            NameTextBox.Text = Name;
            op = operation;
            if (type == "Gate")
            {
                LogicTypeComboBox.ItemsSource = new List<string>
                {
                    "AND",
                    "OR",
                    "NOT",
                    "XOR",
                    "NOR",
                    "XNOR",
                    "NAND"
                };
            }
            else if (type == "Operator")
            {
                LogicTypeComboBox.ItemsSource = new List<string>
                {
                "+",
                "-",
                "*",
                "/",
                "%",
                "^"
                };
            }
            else
            {
                LogicTypeComboBox.ItemsSource = new List<string>
                {
                "==",
                "!=",
                "<",
                "<=",
                ">",
                ">="
                };
            }
            LogicTypeComboBox.SelectedIndex = LogicTypeComboBox.Items.IndexOf(operation);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (LogicTypeComboBox.SelectedItem != null && NameTextBox.Text != "")
            {
                op = (string)LogicTypeComboBox.SelectedItem;
                name = NameTextBox.Text;
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LogicTypeComboBox.SelectedItem = null;
            op = "";
            DialogResult = false;
        }
    }
}
