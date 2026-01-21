using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Comunicaciones_Modbus_TCP
{
    class Modbus
    {
        #region Propiedades
        public Int16 Primera_Salida_A_Leer { get; private set; }
        public Int16 Numero_De_Salidas_A_Leer { get; private set; }
        public Int16 Tamaño_Trama { get; private set; }
        public Int16 Num_Funcion{ get; private set; }
        #endregion

        #region Constructor
        public Modbus(Int16 PrimeraSalidaALeer, Int16 NumeroSalidasALeer, Int16 Nfuncion)
        {
            Primera_Salida_A_Leer = PrimeraSalidaALeer;
            Numero_De_Salidas_A_Leer = NumeroSalidasALeer;
            Num_Funcion = Nfuncion;

            calcularTamañoTrama();
        }
        #endregion

        #region Metodos
        public void enviarDatos()
        {
        }
        public void calcularTamañoTrama()
        {
            switch (Num_Funcion)
            {
                case 1: Tamaño_Trama = 12;
                        break;
                case 2: Tamaño_Trama = 13;
                        break;


            }

        }
        #endregion

    }
}
