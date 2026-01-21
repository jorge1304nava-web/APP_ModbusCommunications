using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace Comunicaciones_Modbus_TCP
{
    public partial class MainWindow : Window
    {
        private Cliente cliente = null;
        private Boolean Conectado = false;

        private Int16 Nfuncion = 0;
        private Int16 Nodo = 0;
        private int primerBit;
        private int numeroBits;
        private Int16 Tam_Trama_Recibida = 0;
        private Boolean esRegistro;
        private String mode;

        private Byte[] tramaEnviar;
        private Byte[] tramaRecibir;

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            tb_Dir_nodo.Text = "1";
            Circle_EstadoConexion.Fill = new SolidColorBrush(Colors.Red);

            Lectura_Escritura(false);
            CargarComboBox();
        }

        #endregion

        #region Eventos

        private void btn_Salir_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btn_Conectar_Click(object sender, RoutedEventArgs e)
        {
            Conectado = !Conectado;

            if (Conectado)
            {
                cliente = new Cliente(tb_DirIP.Text, Convert.ToInt32(tb_Puerto.Text));

                if (cliente != null && cliente.EsClienteValido)
                {
                    btn_Conectar.Content = "Desconectar";
                    Circle_EstadoConexion.Fill = new SolidColorBrush(Colors.Green);
                    Wnd_main.Title = "Comunicacion Modbus (Conectado)";
                }
                else
                    cliente = null;
            }
            else
            {
                cliente.cierraCliente();
                btn_Conectar.Content = "Conectar";
                Circle_EstadoConexion.Fill = new SolidColorBrush(Colors.Red);
                Wnd_main.Title = "Comunicacion Modbus (Desconectado)";
            }

            //ARRANCAMOS HILO QUE SE ENCARGA DE ESCUCHAR AL PLC

            //Hilo_Recibe_Mensajes = new Thread(new ThreadStart(Analiza_Datos_Recibidos));
            //Hilo_Recibe_Mensajes.Start();
        }

        private void comboBox_Opciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Cargamos el modo correcto
            String titulo = comboBox_Opciones.SelectedItem.ToString();
            label_titulo.Content = titulo;

            //Cargamos valores en los text box superiores
            Cargar_textboxs_trama();

            //limpiar
            dataGrid_Datos.Items.Clear();
            tb_primeraSalida.Text = "";
            tb_NumeroSalidas.Text = "";

        }

        private void btn_Enviar_Click(object sender, RoutedEventArgs e)
        {
            Boolean funciones_comunes = (Nfuncion == 1 || Nfuncion == 2 || Nfuncion == 3 || Nfuncion == 4 );
            Boolean funciones_escritura = (Nfuncion == 5 || Nfuncion == 6);
            Boolean funciones_cortas = (Nfuncion == 7);
            Boolean funciones_largas = (Nfuncion == 15 || Nfuncion == 16);
            Boolean any_option_checked = (cb_ASCII.IsChecked == true || cb_Decimal.IsChecked == true || cb_Hexadecimal.IsChecked == true || cb_bits.IsChecked == true || funciones_escritura);

            Cargar_textboxs_trama();

            if (funciones_cortas) // funcion 7
            {
                if (cb_bits.IsChecked == true)
                    OrganizarTramaCorta_y_Enviar();
                else
                    MessageBox.Show("Por favor, marque el check box  de bits para este modo", "Error de envio");
            }
            else if (funciones_largas) // Funcion 15, 16
            {
                if (tb_primeraSalida.Text != "" || tb_NumeroSalidas.Text != "")
                {
                    if(Modificar_Valores())
                        OrganizarTramaLarga_y_Enviar();
                }
            }
            else //Funciones principales (1, 2, 3, 4, 5, 6)
            {
                if (Nfuncion != 0 && tb_primeraSalida.Text != "" && (tb_NumeroSalidas.Text != "" || funciones_escritura))
                {
                    if (any_option_checked)
                    {
                        OrganizarTrama_y_Enviar();
                    }
                    else
                    {
                        cb_ASCII.BorderBrush = new SolidColorBrush(Colors.OrangeRed);
                        cb_Decimal.BorderBrush = new SolidColorBrush(Colors.OrangeRed);
                        cb_Hexadecimal.BorderBrush = new SolidColorBrush(Colors.OrangeRed);
                        cb_bits.BorderBrush = new SolidColorBrush(Colors.OrangeRed);

                        MessageBox.Show("Por favor, marque una de las opciones en las que quiere que los datos sean representados." +
                         " Ya sea ASCII, Hexadecimal, Decimal o Bytes", "Error de envio");
                    }
                }
                else
                {
                    String campos_incompletos = "";
                    if (tb_primeraSalida.Text == "")
                    {
                        tb_primeraSalida.Background = new SolidColorBrush(Colors.OrangeRed);
                        campos_incompletos = campos_incompletos + "-Primera Salida a leer\n";
                    }
                    if (tb_NumeroSalidas.Text == "")
                    {
                        tb_NumeroSalidas.Background = new SolidColorBrush(Colors.OrangeRed);
                        campos_incompletos = campos_incompletos + "-Numero de salidas a leer\n";
                    }

                    MessageBox.Show("Por favor, rellene los datos necesarios para iniciar el envio de la trama.\n" +
                    "Uno o mas campos, están vacios. \n\n" + campos_incompletos, "Error de envio");
                }
            }
        }

        private void cb_bits_Checked(object sender, RoutedEventArgs e)
        {
            cb_Decimal.IsChecked = false;
            cb_Hexadecimal.IsChecked = false;
            cb_ASCII.IsChecked = false;

            CargarDataGrid(1);

            mostrarPorPantalla();
        }

        private void cb_ASCII_Checked(object sender, RoutedEventArgs e)
        {
            cb_Decimal.IsChecked = false;
            cb_Hexadecimal.IsChecked = false;
            cb_bits.IsChecked = false;

            CargarDataGrid(0);

            mostrarPorPantalla();
        }

        private void cb_Decimal_Checked(object sender, RoutedEventArgs e)
        {
            cb_ASCII.IsChecked = false;
            cb_Hexadecimal.IsChecked = false;
            cb_bits.IsChecked = false;

            CargarDataGrid(0);

            mostrarPorPantalla();
        }

        private void cb_Hexadecimal_Checked(object sender, RoutedEventArgs e)
        {
            cb_ASCII.IsChecked = false;
            cb_Decimal.IsChecked = false;
            cb_bits.IsChecked = false;

            CargarDataGrid(0);

            mostrarPorPantalla();
        }

        private void btn_up_dir_nodo_Click(object sender, RoutedEventArgs e)
        {
            Nodo++;
            tb_Dir_nodo.Text = Nodo.ToString();
        }

        private void btn_down_dir_nodo_Click(object sender, RoutedEventArgs e)
        {
            Nodo--;
            tb_Dir_nodo.Text = Nodo.ToString();
        }

        private void btn_up_primer_bit_Click(object sender, RoutedEventArgs e)
        {
            primerBit++;
            tb_primeraSalida.Text = primerBit.ToString();
        }

        private void btn_down_primer_bit_Click(object sender, RoutedEventArgs e)
        {
            primerBit--;
            tb_primeraSalida.Text = primerBit.ToString();
        }

        private void btn_up_num_bits_Click(object sender, RoutedEventArgs e)
        {
            numeroBits++;
            tb_NumeroSalidas.Text = numeroBits.ToString();
        }

        private void btn_down_num_bits_Click(object sender, RoutedEventArgs e)
        {
            numeroBits--
;
            tb_NumeroSalidas.Text = numeroBits.ToString();
        }

        private void tb_primeraSalida_MouseEnter(object sender, MouseEventArgs e)
        {
            tb_primeraSalida.Background = new SolidColorBrush(Colors.White);
        }

        private void tb_NumeroSalidas_MouseEnter(object sender, MouseEventArgs e)
        {
            tb_NumeroSalidas.Background = new SolidColorBrush(Colors.White);
        }

        private void check_box_Enter(object sender, MouseEventArgs e)
        {
            cb_bits.BorderBrush = new SolidColorBrush(Colors.White);
            cb_ASCII.BorderBrush = new SolidColorBrush(Colors.White);
            cb_Decimal.BorderBrush = new SolidColorBrush(Colors.White);
            cb_Hexadecimal.BorderBrush = new SolidColorBrush(Colors.White);
        }

        private void tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            Cargar_textboxs_trama();
        }

        #endregion

        #region Metodo

        public void CargarComboBox()
        {
            comboBox_Opciones.Items.Add("1-  Lectura de salidas discretas");
            comboBox_Opciones.Items.Add("2-  Lectura de entradas discretas");
            comboBox_Opciones.Items.Add("3-  Lectura de regisros internos");
            comboBox_Opciones.Items.Add("4-  Lectura de regisros de entrada");
            comboBox_Opciones.Items.Add("5-  Modificacion del estado de una salida discreta");
            comboBox_Opciones.Items.Add("6-  Modificacion del valor de un registro interno");
            comboBox_Opciones.Items.Add("7-  Leer estados de error");
            comboBox_Opciones.Items.Add("15- Modificar el estado de salidas discretas");
            comboBox_Opciones.Items.Add("16- Modificar el valor de registros internos");
        }

        public void CargarDataGrid(int mode)
        {
            //0 - modo dos columnas
            //1 - modo nueve columnas

            if (mode == 0)
            {
                //limpiar
                dataGrid_Datos.Columns.Clear();

                DataGridTextColumn ColumnaByte = new DataGridTextColumn();
                DataGridTextColumn ColumnaDato = new DataGridTextColumn();

                ColumnaByte.Header = "Nº Byte";
                ColumnaByte.Width = (dataGrid_Datos.Width -2) / 2;
                ColumnaByte.Binding = new Binding("Byte");
                dataGrid_Datos.Columns.Add(ColumnaByte);

                ColumnaDato.Header = "Value";
                ColumnaDato.Width = (dataGrid_Datos.Width -2 )/ 2;
                ColumnaDato.Binding = new Binding("Value");
                dataGrid_Datos.Columns.Add(ColumnaDato);

            }

            if (mode == 1)
            {
                //limpiar
                dataGrid_Datos.Columns.Clear();

                double tam_byte = 88;
                double tam_bit = (dataGrid_Datos.Width - tam_byte - 2) / 8;

                DataGridTextColumn ColumnaByte = new DataGridTextColumn();
                DataGridTextColumn ColumnaBit0 = new DataGridTextColumn();
                DataGridTextColumn ColumnaBit1 = new DataGridTextColumn();
                DataGridTextColumn ColumnaBit2 = new DataGridTextColumn();
                DataGridTextColumn ColumnaBit3 = new DataGridTextColumn();
                DataGridTextColumn ColumnaBit4 = new DataGridTextColumn();
                DataGridTextColumn ColumnaBit5 = new DataGridTextColumn();
                DataGridTextColumn ColumnaBit6 = new DataGridTextColumn();
                DataGridTextColumn ColumnaBit7 = new DataGridTextColumn();

                ColumnaByte.Header = "Nº Byte";
                ColumnaByte.Width = tam_byte;
                ColumnaByte.Binding = new Binding("Byte");
                dataGrid_Datos.Columns.Add(ColumnaByte);

                ColumnaBit0.Header = "Bit 0";
                ColumnaBit0.Width = tam_bit;
                ColumnaBit0.Binding = new Binding("Bit0");
                dataGrid_Datos.Columns.Add(ColumnaBit0);

                ColumnaBit1.Header = "Bit 1";
                ColumnaBit1.Width = tam_bit;
                ColumnaBit1.Binding = new Binding("Bit1");
                dataGrid_Datos.Columns.Add(ColumnaBit1);

                ColumnaBit2.Header = "Bit 2";
                ColumnaBit2.Width = tam_bit;
                ColumnaBit2.Binding = new Binding("Bit2");
                dataGrid_Datos.Columns.Add(ColumnaBit2);

                ColumnaBit3.Header = "Bit 3";
                ColumnaBit3.Width = tam_bit;
                ColumnaBit3.Binding = new Binding("Bit3");
                dataGrid_Datos.Columns.Add(ColumnaBit3);

                ColumnaBit4.Header = "Bit 4";
                ColumnaBit4.Width = tam_bit;
                ColumnaBit4.Binding = new Binding("Bit4");
                dataGrid_Datos.Columns.Add(ColumnaBit4);

                ColumnaBit5.Header = "Bit 5";
                ColumnaBit5.Width = tam_bit;
                ColumnaBit5.Binding = new Binding("Bit5");
                dataGrid_Datos.Columns.Add(ColumnaBit5);

                ColumnaBit6.Header = "Bit 6";
                ColumnaBit6.Width = tam_bit;
                ColumnaBit6.Binding = new Binding("Bit6");
                dataGrid_Datos.Columns.Add(ColumnaBit6);

                ColumnaBit7.Header = "Bit 7";
                ColumnaBit7.Width = tam_bit;
                ColumnaBit7.Binding = new Binding("Bit7");
                dataGrid_Datos.Columns.Add(ColumnaBit7);

            }
        }

        public void Lectura_Escritura(Boolean value)
        {
            //value 0 = Lectura
            //value 1 = Escritura

            if(value)
            {
                btn_up_num_bits.Visibility = Visibility.Hidden;
                btn_down_num_bits.Visibility = Visibility.Hidden;
                tb_NumeroSalidas.Visibility = Visibility.Hidden;

                label_Off.Visibility = Visibility.Visible;
                label_On.Visibility = Visibility.Visible;
                slider_On_Off.Visibility = Visibility.Visible;
            }
            else
            {
                btn_up_num_bits.Visibility = Visibility.Visible;
                btn_down_num_bits.Visibility = Visibility.Visible;
                tb_NumeroSalidas.Visibility = Visibility.Visible;

                label_Off.Visibility = Visibility.Hidden;
                label_On.Visibility = Visibility.Hidden;
                slider_On_Off.Visibility = Visibility.Hidden;
            }

        }

        public void Bloquear_Checkboxs(Boolean value)
        {
            //value = 1 => Bloquear
            if (value)
            {
                cb_ASCII.IsEnabled = false;
                cb_Decimal.IsEnabled = false;
                cb_Hexadecimal.IsEnabled = false;
                cb_bits.IsEnabled = false;

                cb_ASCII.IsChecked = false;
                cb_Decimal.IsChecked = false;
                cb_Hexadecimal.IsChecked = false;
                cb_bits.IsChecked = false;

                dataGrid_Datos.Items.Clear();
            }
            //value = 0 => Desbloquear
            else
            {
                cb_ASCII.IsEnabled = true;
                cb_Decimal.IsEnabled = true;
                cb_Hexadecimal.IsEnabled = true;
                cb_bits.IsEnabled = true;
            }
        }

        private void Invisibilzar_Texboxs(Boolean value)
        {
            if (value)
            {
                tb_NumeroSalidas.Visibility = Visibility.Hidden;
                tb_primeraSalida.Visibility = Visibility.Hidden;

                btn_up_num_bits.Visibility = Visibility.Hidden;
                btn_down_num_bits.Visibility = Visibility.Hidden;

                btn_up_primer_bit.Visibility = Visibility.Hidden;
                btn_down_primer_bit.Visibility = Visibility.Hidden;

                label_num_bits.Visibility = Visibility.Hidden;
                label_Primera_salida.Visibility = Visibility.Hidden;

                tb_celda_9.Visibility = Visibility.Hidden;
                tb_celda_10.Visibility = Visibility.Hidden;
                tb_celda_11.Visibility = Visibility.Hidden;
                tb_celda_12.Visibility = Visibility.Hidden;

                label_numero_de_registros_a_leer.Visibility = Visibility.Hidden;
                label_Primer_registro_a_leer.Visibility = Visibility.Hidden;

                tb_NumeroDatos.Visibility = Visibility.Hidden;
                tb_Numero_Bytes.Visibility = Visibility.Hidden;
                label3_Copy1.Visibility = Visibility.Hidden;
                label3_Copy4.Visibility = Visibility.Hidden;

                slider_On_Off.Visibility = Visibility.Hidden;
            }
            else
            {
                tb_NumeroSalidas.Visibility = Visibility.Visible;
                tb_primeraSalida.Visibility = Visibility.Visible;

                btn_up_num_bits.Visibility = Visibility.Visible;
                btn_down_num_bits.Visibility = Visibility.Visible;

                btn_up_primer_bit.Visibility = Visibility.Visible;
                btn_down_primer_bit.Visibility = Visibility.Visible;

                label_num_bits.Visibility = Visibility.Visible;
                label_Primera_salida.Visibility = Visibility.Visible;

                tb_celda_9.Visibility = Visibility.Visible;
                tb_celda_10.Visibility = Visibility.Visible;
                tb_celda_11.Visibility = Visibility.Visible;
                tb_celda_12.Visibility = Visibility.Visible;

                label_numero_de_registros_a_leer.Visibility = Visibility.Visible;
                label_Primer_registro_a_leer.Visibility = Visibility.Visible;

                tb_NumeroDatos.Visibility = Visibility.Visible;
                tb_Numero_Bytes.Visibility = Visibility.Visible;
                label3_Copy1.Visibility = Visibility.Visible;
                label3_Copy4.Visibility = Visibility.Visible;
            }
        }

        private void Cargar_textboxs_trama()
        {
            //Leemos de los text box primer bit y numero de bits
            //Cargamos  num Funcion y Nodo

            Nfuncion = Convert.ToInt16(comboBox_Opciones.SelectedIndex + 1);
            Nodo = Convert.ToInt16(tb_Dir_nodo.Text);

            //Comprobamos que los text box no esten vacios y que no se introduzcan letras
            if (tb_primeraSalida.Text != "" && int.TryParse(tb_primeraSalida.Text, out int i))
                primerBit = Convert.ToInt32(tb_primeraSalida.Text) - 1;
            else tb_primeraSalida.Text = "";

            if (tb_NumeroSalidas.Text != "" && int.TryParse(tb_NumeroSalidas.Text, out i))
                numeroBits = Convert.ToInt32(tb_NumeroSalidas.Text);
            else tb_NumeroSalidas.Text = "";

            //Switch para escribir las direcciones de memoria de cada funcion
            switch (Nfuncion)
            {
                case 1:
                    tb_Direcciones_Memoria.Text = "Direcciones de memoria:\n\n 00001  --  00320";
                    mode = "Salidas_Digitales";
                    Bloquear_Checkboxs(false);
                    Lectura_Escritura(false);
                    Invisibilzar_Texboxs(false);
                    label_num_bits.Content = "Numero de salidas a leer";
                    break;
                case 2:
                    tb_Direcciones_Memoria.Text = "Direcciones de memoria:\n\n 10001  --  10032";
                    mode = "Entradas_Digitales";
                    Bloquear_Checkboxs(false);
                    Lectura_Escritura(false);
                    Invisibilzar_Texboxs(false);
                    label_num_bits.Content = "Numero de salidas a leer";
                    break;
                case 3:
                    tb_Direcciones_Memoria.Text = "Direcciones de memoria:\n\n 40001  --  40004";
                    mode = "Registros_Internos";
                    Bloquear_Checkboxs(false);
                    Lectura_Escritura(false);
                    Invisibilzar_Texboxs(false);
                    label_num_bits.Content = "Numero de salidas a leer";
                    break;
                case 4:
                    tb_Direcciones_Memoria.Text = "Direcciones de memoria:\n\n 30001  --  30004";
                    mode = "Registros_Entrada";
                    Bloquear_Checkboxs(false);
                    Lectura_Escritura(false);
                    Invisibilzar_Texboxs(false);
                    label_num_bits.Content = "Numero de salidas a leer";
                    break;
                case 5:
                    tb_Direcciones_Memoria.Text = "Direcciones de memoria:\n\n 00001  --  00320";
                    mode = "Salidas_Digitales";
                    Bloquear_Checkboxs(true);
                    Invisibilzar_Texboxs(false);
                    Lectura_Escritura(true);
                    label_num_bits.Content = "Estado (On/Off)";
                    break;
                case 6:
                    tb_Direcciones_Memoria.Text = "Direcciones de memoria:\n\n 40001  --  40004";
                    mode = "Registros_Internos";
                    Bloquear_Checkboxs(true);
                    Lectura_Escritura(false);
                    Invisibilzar_Texboxs(false);
                    label_num_bits.Content = "Valor deseado";
                    break;
                case 7:
                    mode = "";
                    Bloquear_Checkboxs(false);
                    Invisibilzar_Texboxs(true);
                    break;
                case 8:
                    tb_Direcciones_Memoria.Text = "Direcciones de memoria:\n\n 00001  --  00320";
                    mode = "Salidas_Digitales";
                    Nfuncion = 15;
                    Bloquear_Checkboxs(true);
                    Invisibilzar_Texboxs(false);
                    label_num_bits.Content = "Numero de salidas a leer";
                    break;
                case 9:
                    tb_Direcciones_Memoria.Text = "Direcciones de memoria:\n\n 40001  --  40004";
                    mode = "Registros_Internos";
                    Nfuncion = 16;
                    Bloquear_Checkboxs(true);
                    Invisibilzar_Texboxs(false);
                    label_num_bits.Content = "Numero de salidas a leer";
                    break;

            }

            //////Vaciamos textbox de arriba del datagridview//////
            tb_NumeroDatos.Text = "";
            tb_Funcion.Text = "";
            tb_Nodo.Text = "";
            tb_Numero_Bytes.Text = "";
            //////////////////////////////////////////////


            //Cargamos el mensaje de la parte superior
            tb_celda_1.Text = "11";
            tb_celda_2.Text = "11";
            tb_celda_3.Text = "00";
            tb_celda_4.Text = "00";
            tb_celda_5.Text = "00";
            tb_celda_6.Text = "06";

            String str_Num;
            String Less_Weight;
            String Most_Weight;

            tb_celda_7.Text = Nodo.ToString();
            tb_celda_8.Text = Nfuncion.ToString();

            //Primer Bits

            switch (mode)
            {
                case "Salidas_Digitales":
                    break;
                case "Entradas_Digitales":
                    primerBit = primerBit - 10000;
                    break;
                case "Registros_Entrada":
                    primerBit = primerBit - 30000;
                    break;
                case "Registros_Internos":
                    primerBit = primerBit - 40000;
                    break;
            }

            str_Num = primerBit.ToString("X"); //Convertir a hexadecimal

            if (str_Num.Length > 2)
            {
                Most_Weight = str_Num.Substring(0, str_Num.Length - 2);
                Less_Weight = str_Num.Substring(str_Num.Length - 2);

                if (Most_Weight.Length == 1)  //Añado un 0
                    tb_celda_9.Text = "0" + Most_Weight;
                else
                    tb_celda_9.Text = Most_Weight;

                tb_celda_10.Text = Less_Weight;
            }
            else
            {
                tb_celda_9.Text = "00";

                if(str_Num.Length == 1)
                    tb_celda_10.Text = "0" + str_Num;
                else
                    tb_celda_10.Text = str_Num;


            }

            //Numero de bits
            str_Num = numeroBits.ToString("X"); //Convertir a hexadecimal
            if (str_Num.Length > 2)
            {
                Most_Weight = str_Num.Substring(0, str_Num.Length - 2);
                Less_Weight = str_Num.Substring(str_Num.Length - 2);

                if (Most_Weight.Length == 1) //Añado un 0
                    tb_celda_11.Text = "0" + Most_Weight;
                else
                    tb_celda_11.Text = Most_Weight;

                tb_celda_12.Text = Less_Weight;
            }
            else
            {
                tb_celda_11.Text = "00";

                if (str_Num.Length == 1)
                    tb_celda_12.Text = "0" + str_Num;
                else
                    tb_celda_12.Text = str_Num;
            }
        }

        public Boolean Modificar_Valores()
        {
            Boolean continuar = false;
            primerBit = Convert.ToInt32(tb_primeraSalida.Text) - 1;

            switch (mode)
            {
                case "Salidas_Digitales":
                    if (0 <= primerBit && primerBit < 320)
                        if (primerBit + numeroBits <= 320)
                        {
                            continuar = true;
                        }
                        else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + numeroBits + 1) + " ( " + primerBit + " + " + numeroBits + " ) en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                    else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + 1) + " en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                    break;
                case "Registros_Internos":
                    if (40000 <= primerBit && primerBit < 40004)
                    {
                        if (primerBit + numeroBits <= 40004)
                        {
                            continuar = true;
                            primerBit = primerBit - 40000;
                        }
                        else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + numeroBits + 1) + " ( " + primerBit + " + " + numeroBits + " ) en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                    }
                    else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + 1) + " en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                    break;
            }

            if (continuar)
            {
                if (Nfuncion == 15)
                {
                    Establecer_valores wnd = new Establecer_valores(primerBit, Convert.ToInt32(tb_NumeroSalidas.Text), Nfuncion, Convert.ToInt32(tb_Dir_nodo.Text));
                    wnd.ShowDialog();

                    tramaEnviar = wnd.tramaEnviar;
                }

                if (Nfuncion == 16)
                {
                    Establecer_valores wnd = new Establecer_valores(primerBit, Convert.ToInt32(tb_NumeroSalidas.Text), Nfuncion, Convert.ToInt32(tb_Dir_nodo.Text));
                    wnd.ShowDialog();

                    tramaEnviar = wnd.tramaEnviar;

                }
            }

            return continuar;

        }

        //Trama general para las principales funciones (1, 2, 3, 4, 5, 6). Compuesta por 6 bits
        public void OrganizarTrama_y_Enviar()
        {
            Boolean continuar = false;

            primerBit = Convert.ToInt32(tb_primeraSalida.Text) - 1;

            //////////////////////////////////////////////////////////
            switch (mode)
            {
                case "Salidas_Digitales":
                    if (0 <= primerBit && primerBit < 320)
                        if (primerBit + numeroBits <= 320)
                        {
                            continuar = true;
                        }
                        else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + numeroBits + 1) + " ( " +primerBit + "" + numeroBits + " ) en la funcion " +  comboBox_Opciones.SelectedItem.ToString(), "Error");
                    else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + 1) + " en la funcion "+ comboBox_Opciones.SelectedItem.ToString(), "Error");
                    break;
                case "Entradas_Digitales":
                    if (10000 <= primerBit && primerBit < 10032)
                    {
                        if (primerBit + numeroBits <= 10032)
                        {
                            continuar = true;
                            primerBit = primerBit - 10000;
                        }
                        else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + numeroBits + 1) + " ( " + primerBit + " + " + numeroBits + " ) en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                    }
                    else  MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + 1) + " en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                    break;
                case "Registros_Entrada":
                    if (30000 <= primerBit && primerBit < 30004)
                    {
                        if (primerBit + numeroBits <= 30004)
                        {
                            primerBit = primerBit - 30000;
                            continuar = true;
                        }
                        else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + numeroBits + 1) + " ( " + primerBit + " + " + numeroBits + " ) en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                    }
                    else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + 1) + " en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                    break;
                case "Registros_Internos":
                    if (40000 <= primerBit && primerBit < 40004)
                    {
                        if (Nfuncion != 6)
                        {
                            if (primerBit + numeroBits <= 40004)
                            {
                                continuar = true;
                                primerBit = primerBit - 40000;
                            }
                            else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + numeroBits + 1) + " ( " + primerBit + " + " + numeroBits + " ) en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                        }
                        else
                        {
                            primerBit = primerBit - 40000;
                            continuar = true;
                        }
                    }
                    else MessageBox.Show("Error al intentar procesar su solicitud. Alguno de los campos es incorrecto. \nNo existe la direccion " + (primerBit + 1) + " en la funcion " + comboBox_Opciones.SelectedItem.ToString(), "Error");
                    break;
            }
            //////////////////////////////////////////////////////////
            ///
            if (continuar)
            {
                if (Nfuncion != 5)
                    numeroBits = Convert.ToInt32(tb_NumeroSalidas.Text);


                Nodo = Convert.ToInt16(tb_Dir_nodo.Text);

                tramaEnviar = new byte[12];

                //TraMa TCP
                tramaEnviar[0] = Convert.ToByte(11);
                tramaEnviar[1] = Convert.ToByte(11);
                tramaEnviar[2] = 0x00;
                tramaEnviar[3] = 0x00;
                tramaEnviar[4] = 0x00;
                tramaEnviar[5] = Convert.ToByte(6); //Longitud de bytes
                                                    //Trama Mondbus
                tramaEnviar[6] = Convert.ToByte(Nodo); //Nodo
                tramaEnviar[7] = Convert.ToByte(Nfuncion); //Funcion

                //Conversion del primer bit y numero de bits a big endian//

                String str_Num;
                String Less_Weight;
                String Most_Weight;

                //////////////////////////////////////////////////////////////

                //NºRegistro - Primer Bits
                str_Num = primerBit.ToString("X"); //Convertir a hexadecimal
                if (str_Num.Length > 2)
                {
                    Most_Weight = str_Num.Substring(0, str_Num.Length - 2);
                    Less_Weight = str_Num.Substring(str_Num.Length - 2);

                    //Previamente se convierte el numero de hexadecimal a decimal
                    tramaEnviar[8] = Convert.ToByte(Convert.ToInt32(Most_Weight, 16));
                    tramaEnviar[9] = Convert.ToByte(Convert.ToInt32(Less_Weight, 16));
                }
                else
                {
                    tramaEnviar[8] = Convert.ToByte(0);
                    tramaEnviar[9] = Convert.ToByte(Convert.ToInt32(str_Num, 16));
                }
                //Cantidad de registros - Numero de bits
                str_Num = numeroBits.ToString("X"); //Convertir a hexadecimal
                if (str_Num.Length > 2)
                {
                    Most_Weight = str_Num.Substring(0, str_Num.Length - 2);
                    Less_Weight = str_Num.Substring(str_Num.Length - 2);

                    //Previamente se convierte el numero de hexadecimal a decimal
                    tramaEnviar[10] = Convert.ToByte(Convert.ToInt32(Most_Weight, 16));
                    tramaEnviar[11] = Convert.ToByte(Convert.ToInt32(Less_Weight, 16));
                }
                else
                {

                    if (Nfuncion == 5)
                    {
                        if (slider_On_Off.Value == 1)
                        {
                            //ON
                            Most_Weight = 255.ToString("X");
                            Less_Weight = 0.ToString("X");
                        }
                        else
                        {
                            //OFF
                            Most_Weight = 0.ToString("X");
                            Less_Weight = 0.ToString("X");
                        }
                        tramaEnviar[10] = Convert.ToByte(Convert.ToInt32(Most_Weight, 16));
                        tramaEnviar[11] = Convert.ToByte(Convert.ToInt32(Less_Weight, 16));
                    }
                    else
                    {
                        tramaEnviar[10] = Convert.ToByte(0);
                        tramaEnviar[11] = Convert.ToByte(Convert.ToInt32(str_Num, 16));
                    }
                }

                cliente.enviaDatos(tramaEnviar, tramaEnviar.Length);

                Analiza_Datos_Recibidos();
            }
            

        }

        //Trama corta para las funciones 7. Compuesta por 2 bits (nodo, nfuncion)
        public void OrganizarTramaCorta_y_Enviar()
        {
            Nodo = Convert.ToInt16(tb_Dir_nodo.Text);

            tramaEnviar = new Byte[8];

            //TraMa TCP
            tramaEnviar[0] = Convert.ToByte(11);
            tramaEnviar[1] = Convert.ToByte(11);
            tramaEnviar[2] = 0x00;
            tramaEnviar[3] = 0x00;
            tramaEnviar[4] = 0x00;
            tramaEnviar[5] = Convert.ToByte(2); //Longitud de bytes
            //Trama Mondbus
            tramaEnviar[6] = Convert.ToByte(Nodo); //Nodo
            tramaEnviar[7] = Convert.ToByte(Nfuncion); //Funcion

            cliente.enviaDatos(tramaEnviar, tramaEnviar.Length);

            Analiza_Datos_Recibidos();

        }

        //Trama corta para las funciones 15, 16
        public void OrganizarTramaLarga_y_Enviar()
        {
            cliente.enviaDatos(tramaEnviar, tramaEnviar.Length);

            mostrarPorPantalla();
        }

        //Analiza las salidas de las principales funciones (1, 2, 3, 4, 5, 6)
        public void Analiza_Datos_Recibidos()
        {
            tramaRecibir = new byte[100];

            cliente.recibeDatos(tramaRecibir, 100);

            Tam_Trama_Recibida = tramaRecibir[5];
            Nodo = tramaRecibir[6];
            Nfuncion = Convert.ToInt16(tramaRecibir[7]);

            //Si la nfuncion es 3 o 4 accede a la seccion de registros que son de 16 bits (2 Bytes) 
            if (Nfuncion == 3 || Nfuncion == 4)
                esRegistro = true;
            else esRegistro = false;
            ///////////////////////////////////////////////////////////////////////////////////////

            //Se colocan valores en sus correspondientes textBoxs
            if(Nfuncion != 7)
                tb_NumeroDatos.Text = Tam_Trama_Recibida.ToString(); 
            tb_Funcion.Text = Nfuncion.ToString();
            tb_Nodo.Text = Nodo.ToString();
            if (Nfuncion != 7)
            tb_Numero_Bytes.Text = (Tam_Trama_Recibida - 3).ToString();

            mostrarPorPantalla();

        }

        //Muestra las salidas de las funciones (1, 2, 3, 4, 5, 6, 7)
        public void mostrarPorPantalla()
        {
            char[] arrayTrama = new char[200];
            String STRtrama = "";
            int i = 0;
            int registro = 1;
            Item item = new Item();
            Byte byte_Error;
            String valueString;

            if (Nfuncion != 5 && Nfuncion != 6 && Nfuncion != 7 && Nfuncion != 15 && Nfuncion != 16)
            {
                if (cb_ASCII.IsChecked == true)
                {
                    //limpiar
                    dataGrid_Datos.Items.Clear();


                    for (i = 9; i < (Tam_Trama_Recibida + 6); i++)
                    {
                        dataGrid_Datos.Items.Add(new Item() { Byte = "Byte " + (i - 8), Value = Encoding.ASCII.GetString(tramaRecibir, i, 1).ToString() });
                    }

                }
                else if (cb_Decimal.IsChecked == true)
                {
                    //limpiar
                    dataGrid_Datos.Items.Clear();

                    for (i = 9; i < (Tam_Trama_Recibida + 6); i++)
                    {
                        if (esRegistro)
                        {
                            valueString = (Convert.ToInt32(tramaRecibir[i]) * 256 + tramaRecibir[i + 1]).ToString();

                            //Actualizo valores
                            dataGrid_Datos.Items.Add(new Item() { Byte = "Registro " + registro, Value = valueString });
                            dataGrid_Datos.Items.Refresh();

                            registro++;
                            i++;
                        }
                        else //Salidas/Entradas Discretas
                        {
                            valueString = tramaRecibir[i].ToString();

                            //Actualizo valores
                            dataGrid_Datos.Items.Add(new Item() { Byte = "Byte " + (i - 8), Value = valueString });
                            dataGrid_Datos.Items.Refresh();
                        }
                    }
                }
                else if (cb_Hexadecimal.IsChecked == true)
                {
                    //limpiar
                    dataGrid_Datos.Items.Clear();

                    //Conversion a string -> array de chars en hexadecimal
                    STRtrama = BitConverter.ToString(tramaRecibir).Replace("-", "");
                    arrayTrama = STRtrama.ToCharArray(18, (Tam_Trama_Recibida - 3) * 2);

                    for (i = 0; i < (Tam_Trama_Recibida - 3) * 2; i = i + 2)
                    {
                        if (esRegistro)
                        {
                            valueString = arrayTrama[i].ToString() + arrayTrama[i + 1].ToString() + arrayTrama[i + 2].ToString() + arrayTrama[i + 3].ToString();

                            //Actualizo valores
                            dataGrid_Datos.Items.Add(new Item() { Byte = "Registro " + registro, Value = valueString });
                            dataGrid_Datos.Items.Refresh();

                            i = i + 2;
                        }
                        else
                        {
                            valueString = arrayTrama[i].ToString() + arrayTrama[i + 1].ToString();

                            //Actualizo valores
                            dataGrid_Datos.Items.Add(new Item() { Byte = "Byte " + ((i / 2) + 1).ToString(), Value = valueString });
                            dataGrid_Datos.Items.Refresh();
                        }
                    }
                }
                else if (cb_bits.IsChecked == true)
                {
                    //limpiar
                    dataGrid_Datos.Items.Clear();

                    //convertir a bits

                    Boolean value_0;
                    Boolean value_1;
                    Boolean value_2;
                    Boolean value_3;
                    Boolean value_4;
                    Boolean value_5;
                    Boolean value_6;
                    Boolean value_7;

                    byte[] pos_0 = new byte[1];
                    byte[] pos_1 = new byte[1];
                    byte[] pos_2 = new byte[1];
                    byte[] pos_3 = new byte[1];
                    byte[] pos_4 = new byte[1];
                    byte[] pos_5 = new byte[1];
                    byte[] pos_6 = new byte[1];
                    byte[] pos_7 = new byte[1];

                    pos_0[0] = Convert.ToByte(1);   //0000 0001
                    pos_1[0] = Convert.ToByte(2);   //0000 0010
                    pos_2[0] = Convert.ToByte(4);   //0000 0100
                    pos_3[0] = Convert.ToByte(8);   //0000 1000
                    pos_4[0] = Convert.ToByte(16);  //0001 0000
                    pos_5[0] = Convert.ToByte(32);  //0010 0000
                    pos_6[0] = Convert.ToByte(64);  //0100 0000
                    pos_7[0] = Convert.ToByte(128); //1000 0000


                    for (i = 9; i < (Tam_Trama_Recibida + 6); i++)
                    {
                        value_0 = Convert.ToBoolean(tramaRecibir[i] & pos_0[0]);
                        value_1 = Convert.ToBoolean(tramaRecibir[i] & pos_1[0]);
                        value_2 = Convert.ToBoolean(tramaRecibir[i] & pos_2[0]);
                        value_3 = Convert.ToBoolean(tramaRecibir[i] & pos_3[0]);
                        value_4 = Convert.ToBoolean(tramaRecibir[i] & pos_4[0]);
                        value_5 = Convert.ToBoolean(tramaRecibir[i] & pos_5[0]);
                        value_6 = Convert.ToBoolean(tramaRecibir[i] & pos_6[0]);
                        value_7 = Convert.ToBoolean(tramaRecibir[i] & pos_7[0]);

                        item.Byte = "Byte " + (i - 8);
                        item.Bit0 = Convert.ToInt16(value_0).ToString();
                        item.Bit1 = Convert.ToInt16(value_1).ToString();
                        item.Bit2 = Convert.ToInt16(value_2).ToString();
                        item.Bit3 = Convert.ToInt16(value_3).ToString();
                        item.Bit4 = Convert.ToInt16(value_4).ToString();
                        item.Bit5 = Convert.ToInt16(value_5).ToString();
                        item.Bit6 = Convert.ToInt16(value_6).ToString();
                        item.Bit7 = Convert.ToInt16(value_7).ToString();

                        //Actualizo valores
                        dataGrid_Datos.Items.Add(new Item() { Byte = item.Byte, Bit0 = item.Bit0, Bit1 = item.Bit1, Bit2 = item.Bit2, Bit3 = item.Bit3, Bit4 = item.Bit4, Bit5 = item.Bit5, Bit6 = item.Bit6, Bit7 = item.Bit7 });
                        dataGrid_Datos.Items.Refresh();
                    }
                }
            }
            else if (Nfuncion == 5 || Nfuncion == 6) //Funcion 5 y 6 (Es Registro)
            {
                if (Nfuncion == 5)
                    MessageBox.Show("Salida " + tb_primeraSalida.Text + " puesta en estado " + Convert.ToBoolean(slider_On_Off.Value), "Información");
                else if (Nfuncion == 6)
                    MessageBox.Show("Registro " + tb_primeraSalida.Text + " con valor " + tb_NumeroSalidas.Text, "Información");
            }
            else if (Nfuncion == 7 && (cb_bits.IsChecked == true)) //Funcion 7 - ERRORES
            {
                //limpiar
                dataGrid_Datos.Items.Clear();

                if (tramaRecibir != null)
                {
                    byte_Error = tramaRecibir[8];

                    item.Byte = "Errores";
                    item.Bit0 = Convert.ToInt16(Convert.ToBoolean(byte_Error & Convert.ToByte(1))).ToString();
                    item.Bit1 = Convert.ToInt16(Convert.ToBoolean(byte_Error & Convert.ToByte(2))).ToString();
                    item.Bit2 = Convert.ToInt16(Convert.ToBoolean(byte_Error & Convert.ToByte(4))).ToString();
                    item.Bit3 = Convert.ToInt16(Convert.ToBoolean(byte_Error & Convert.ToByte(8))).ToString();
                    item.Bit4 = Convert.ToInt16(Convert.ToBoolean(byte_Error & Convert.ToByte(16))).ToString();
                    item.Bit5 = Convert.ToInt16(Convert.ToBoolean(byte_Error & Convert.ToByte(32))).ToString();
                    item.Bit6 = Convert.ToInt16(Convert.ToBoolean(byte_Error & Convert.ToByte(64))).ToString();
                    item.Bit7 = Convert.ToInt16(Convert.ToBoolean(byte_Error & Convert.ToByte(128))).ToString();

                    //Actualizo valores
                    dataGrid_Datos.Items.Add(new Item() { Byte = item.Byte, Bit0 = item.Bit0, Bit1 = item.Bit1, Bit2 = item.Bit2, Bit3 = item.Bit3, Bit4 = item.Bit4, Bit5 = item.Bit5, Bit6 = item.Bit6, Bit7 = item.Bit7 });
                    dataGrid_Datos.Items.Refresh();
                }
            }
            else if (Nfuncion == 15)
            {
                MessageBox.Show("Se han modificado " + ((Convert.ToInt32(tramaEnviar[11].ToString()) / 8) + 1) + " registros correctamente.", "Correcto");
            }
            else if (Nfuncion == 16)
            {
                MessageBox.Show("Se han modificado " + tramaEnviar[11].ToString() + " registros correctamente.", "Correcto");
            }

        }

        #endregion

    }
}
