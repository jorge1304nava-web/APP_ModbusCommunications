using System;
using System.Windows;
using System.Net;
using System.Net.Sockets;

namespace Comunicaciones_Modbus_TCP
{
    class Cliente
    {

        private Socket clienteTCP = null;

        #region Constructor
        public Cliente(String dirIP, Int32 puerto)
        {
            try
            {
                EsClienteValido = false;

                //Primera interfaz IP que encuentra en el equipo
                IPAddress direccion = IPAddress.Parse(dirIP);

                //Creo un objeto con la direccion y el puerto
                IPEndPoint ipep = new IPEndPoint(direccion, puerto);

                clienteTCP = new Socket(direccion.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                clienteTCP.Connect(ipep);
            }
            //Conexiones rechazadas
            catch (SocketException ex_socket) //Excepcion Socket
            {
                if (ex_socket.SocketErrorCode == SocketError.ConnectionRefused)
                    MessageBox.Show("Error al conectarse al servidor:\nAntes de poner en marcha el cliente se debe poner en funcionamiento el servidor en el nodo cuya IP es: " + dirIP + " , y el puerto: " + puerto.ToString() + " debe poder ser accedido (firewall)", "Error Cliente TCP");
                else
                {
                    MessageBox.Show("Error al enviar/recibir por el servidor:\n" + ex_socket.ToString(), "Error Cliente TCP");
                    throw;
                }
            }
            catch (Exception ex)  //Exceptcion General
            {
                MessageBox.Show("Error al conectarse con el servidor:\n" + ex.ToString(), "Error Servidor TCP");
                throw;
            }
            finally
            {
                //Si no hay conexion, elimino el Socket clienteTCP para que se vuelva a intentar.
                if (clienteTCP != null && clienteTCP.Connected)
                    EsClienteValido = true;
                else
                    clienteTCP = null;
            }
        }
        #endregion

        #region Metodos
        //Variable boolena accesible desde cualquier parte, para conocer el estado del cliente
        public bool EsClienteValido{ get; private set; }

        //Para cuando se desee finalizar la conexion
        public void cierraCliente()
        {
            if (clienteTCP != null)
            {
                clienteTCP.Close();
            }
            clienteTCP = null;
            EsClienteValido = false;
        }

        //Envio de array de bytes
        public int enviaDatos(byte[] datos, int dim)
        {
            try
            {
                if (clienteTCP != null)
                {
                    int res = clienteTCP.Send(datos, dim, SocketFlags.None);

                    if (res == dim)
                        return res;
                    else if (res == 0)  //Si es 0, es que el otro nodo a cerrado la conexion => No existe conexion
                        return (-1);
                    else
                        return (-2);  //Error desconocido
                }
                else  //Cliente TCP = Null => No existe conexion
                    return (-1);
            }
            //Excepciones
            catch (SocketException ex_socket)  //Excepcion Socket
            {
                //Conexion rechazada
                if (ex_socket.SocketErrorCode == SocketError.ConnectionReset)
                    return (-1);  //No existe conexion
                else
                    return (-2);  //Error desconocido
            }
            catch (Exception ex)  //Excepcion General
            {
                return (-2);  //Error desconocido
            }
        }

        //Recibo de array de datos
        public int recibeDatos(byte[] datos, int dimMax)
        {
            try
            {
                if (clienteTCP != null)
                {
                    int res = clienteTCP.Receive(datos, dimMax, SocketFlags.None);

                    if (res > 0)
                        return res;
                    else if (res == 0)  //Si es 0, es que el otro nodo a cerrado la conexion => No existe conexion
                        return (-1);
                    else
                        return (-2);   //Error desconocido
                }
                else
                    return (-1);  //Cliente TCP = Null => No existe conexion
            }
            //Excepciones
            catch (SocketException ex_socket)  //Excepcion Socket
            {
                //Conexion rechazada
                if (ex_socket.SocketErrorCode == SocketError.ConnectionReset)
                    return (-1); //No existe conexion
                else
                    return (-2);  //Error desconocido
            }
            catch (Exception ex)  //Excepcion General
            {
                return (-2);  //Error desconocido
            }
        }
        #endregion
    }
}
