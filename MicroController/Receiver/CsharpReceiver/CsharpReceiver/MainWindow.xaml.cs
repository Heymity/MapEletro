using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CsharpReceiver;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel vm;
    
    public MainWindow()
    {
        this.InitializeComponent();

        vm = new MainViewModel();
        this.DataContext = this.vm;
    }
    
    private void CommandInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        
        var command = CommandInput.Text;
        TerminalOutput.Text += $"$ {command}\n";
        CommandInput.Clear();

        // TODO send command
            
        // Scroll automático para o fim
        TerminalScrollView.ScrollToEnd();
    }

    private void Conectar_Click(object sender, RoutedEventArgs e)
    {
        var port = ComboBoxPorts.SelectedItem.ToString();
        if (port is null) return;
        vm.ConnectPort(port);
    }

    private readonly string[] _dropdownErrMsg = ["Não há portas COM disponíveis"];
    private void ComboBoxPorts_OnDropDownOpened(object? sender, EventArgs e)
    {
        var ports = COMPort.GetPortNames();
        if (ports.Length <= 0)
        {
            ComboBoxPorts.ItemsSource = _dropdownErrMsg;
            return;
        }
        ComboBoxPorts.ItemsSource = ports;
    }
}