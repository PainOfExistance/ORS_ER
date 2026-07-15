using System.Windows;

namespace ORS_ER.windows
{
    public partial class LabelWindow : Window
    {
        public string? LabelText = "";

        public LabelWindow(string initial)
        {
            InitializeComponent();
            LabelTextBox.Text = initial ?? "";
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            LabelText = LabelTextBox.Text ?? "";
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
