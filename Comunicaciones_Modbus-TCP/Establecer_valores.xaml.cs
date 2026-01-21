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
using System.Windows.Shapes;

namespace Comunicaciones_Modbus_TCP
{
    /// <summary>
    /// Lógica de interacción para Establecer_valores.xaml
    /// </summary>
    public partial class Establecer_valores : Window
    {
        Int32 primera_Salida;
        Int32 numero_Salidas;
        Int32 nFuncion;
        Int32 Nodo;
        //Funcion 15
        int num_bytes;

        public string PrimerByte { get; set; }
        public string SegundoByte { get; set; }
        public string TercerByte { get; set; }
        public string CuartoByte { get; set; }

        public Byte[] tramaEnviar { get; set; }


        public Establecer_valores(int primer_sal, int num_sal, int nfuncion, int nodo)
        {
            InitializeComponent();

            this.primera_Salida = primer_sal;
            this.numero_Salidas = num_sal;
            this.nFuncion = nfuncion;
            this.Nodo = nodo;
            this.num_bytes = (num_sal / 8) + 1;

            /////////////Escrbimos en text boxs//////////////////////////
            
            if(nfuncion == 15)
                tb_PrimeraSalida.Text = (this.primera_Salida + 1).ToString();
            else if (nfuncion == 16)
                tb_PrimeraSalida.Text = (this.primera_Salida + 40001).ToString();

            tb_NumeroSalidas.Text = this.numero_Salidas.ToString();
            tb_Nodo.Text = this.Nodo.ToString();
            tb_NumeroFuncion.Text = this.nFuncion.ToString();

            ////////////////////////////////////////////////////////////

            /////////Poner todos los text box invisibles////////////////

            foreach (TextBox tb in Grid_Establecer_valores.Children.OfType<TextBox>())
            {

                if(tb.Name != "tb_PrimeraSalida" && tb.Name != "tb_NumeroSalidas" && tb.Name != "tb_Nodo" && tb.Name != "tb_NumeroFuncion")
                tb.Visibility = Visibility.Hidden;
            }

            foreach (Button btn in Grid_Establecer_valores.Children.OfType<Button>())
            {
                if(btn.Name != "btn_Enviar_trama")
                    btn.Visibility = Visibility.Hidden;
            }

            foreach (Label lb in Grid_Establecer_valores.Children.OfType<Label>())
            {
                if(lb.Name != "label_primera_salida" && lb.Name != "label_numero_salida" && lb.Name!= "label_nodo" && lb.Name != "label_numero_funcion")
                    lb.Visibility = Visibility.Hidden;
            }
            ////////////////////////////////////////////////////////////


            cambiar_apariencia();
        }

        #region Eventos
        private void btn_up_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_down_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_Enviar_trama_Click(object sender, RoutedEventArgs e)
        {
            organizar_Trama();

            this.Close();
        }
        #endregion

        #region Metodos
        public void cambiar_apariencia()
        {
            String name_text_box;
            String name_up_btn;
            String name_down_btn;
            String name_label;

            int limite;

            if (nFuncion == 15)
                limite = num_bytes;
            else limite = numero_Salidas;

            for (int i = 1; i <= limite; i++)
            {
                name_text_box = "tb_Byte_" + i;
                name_up_btn = "btn_up_byte_" + i;
                name_down_btn = "btn_down_byte_" + i;
                name_label = "label_byte_" + i;

                foreach (TextBox tb in Grid_Establecer_valores.Children.OfType<TextBox>())
                {
                    if(tb.Name == name_text_box)
                        tb.Visibility = Visibility.Visible;
                }

                foreach (Button btn in Grid_Establecer_valores.Children.OfType<Button>())
                {
                    if (btn.Name == name_up_btn || btn.Name == name_down_btn)
                        btn.Visibility = Visibility.Visible;
                }

                foreach (Label lb in Grid_Establecer_valores.Children.OfType<Label>())
                {
                    if (lb.Name == name_label)
                    {
                        lb.Visibility = Visibility.Visible;
                        switch(nFuncion)
                        {
                            case 15:
                                lb.Content = "Byte " + (30000 + this.primera_Salida + i);
                                break;
                            case 16:
                                lb.Content = "Byte " + (40000 + this.primera_Salida + i);
                                break;
                        }
                        
                    }
                }
            }

        }

        public void organizar_Trama()
        {
            int tamaño_trama_Modbus = 0;
            int j = 1;

            switch(nFuncion)
            {
                case 15:
                    tamaño_trama_Modbus = 7 + (num_bytes);
                    tramaEnviar = new Byte[6 + tamaño_trama_Modbus];
                    break;
                case 16:
                    tamaño_trama_Modbus = 7 + (numero_Salidas * 2);
                    tramaEnviar = new Byte[6 + tamaño_trama_Modbus];
                    break;
            }

            //TraMa TCP
            tramaEnviar[0] = Convert.ToByte(11);
            tramaEnviar[1] = Convert.ToByte(11);
            tramaEnviar[2] = 0x00;
            tramaEnviar[3] = 0x00;
            tramaEnviar[4] = 0x00;
            tramaEnviar[5] = Convert.ToByte(tamaño_trama_Modbus); //Longitud de bytes
            //Trama Mondbus
            tramaEnviar[6] = Convert.ToByte(Nodo);
            tramaEnviar[7] = Convert.ToByte(nFuncion);

            //Conversion del primer bit y numero de bits a big endian//

            String str_Num;
            String Less_Weight;
            String Most_Weight;

            //////////////////////////////////////////////////////////////
            
            //Primer Bit

            str_Num = primera_Salida.ToString("X"); //Convertir a hexadecimal
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

            //Numero Bits

            str_Num = numero_Salidas.ToString("X"); //Convertir a hexadecimal
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
                tramaEnviar[10] = Convert.ToByte(0);
                tramaEnviar[11] = Convert.ToByte(Convert.ToInt32(str_Num, 16));
            }

            //Byte x (Dinamico)
            String name_text_box;

            switch (nFuncion)
            {
                case 15:
                    tramaEnviar[12] = Convert.ToByte(num_bytes); // Ocupan 1 bytes por registro

                    for (int i = 0; i < num_bytes; i++)
                    {
                        name_text_box = "tb_Byte_" + j;

                        foreach (TextBox tb in Grid_Establecer_valores.Children.OfType<TextBox>())
                        {
                            if (tb.Name == name_text_box)
                            {
                                str_Num = Convert.ToInt32(tb.Text).ToString();
                            }
                        }

                        tramaEnviar[13 + i] = Convert.ToByte(str_Num);
                        j++;
                    }

                    break;
                case 16:
                    tramaEnviar[12] = Convert.ToByte(numero_Salidas * 2); // Ocupan 2 bytes por registro

                    for (int i = 0; i < (numero_Salidas * 2); i++)
                    {
                        name_text_box = "tb_Byte_" + j;

                        foreach (TextBox tb in Grid_Establecer_valores.Children.OfType<TextBox>())
                        {
                            if (tb.Name == name_text_box)
                            {
                                str_Num = Convert.ToInt32(tb.Text).ToString("X");
                            }
                        }

                        if (str_Num.Length > 2)
                        {
                            Most_Weight = str_Num.Substring(0, str_Num.Length - 2);
                            Less_Weight = str_Num.Substring(str_Num.Length - 2);

                            //Previamente se convierte el numero de hexadecimal a decimal
                            tramaEnviar[13 + i] = Convert.ToByte(Convert.ToInt32(Most_Weight, 16));
                            tramaEnviar[14 + i] = Convert.ToByte(Convert.ToInt32(Less_Weight, 16));
                        }
                        else
                        {
                            tramaEnviar[13 + i] = Convert.ToByte(0);
                            tramaEnviar[14 + i] = Convert.ToByte(Convert.ToInt32(str_Num, 16));
                        }

                        i++;
                        j++;
                    }
                    break;
            }
  
        }
        #endregion


    }
}
