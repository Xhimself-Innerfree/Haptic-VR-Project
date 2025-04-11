using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.Windows;

// the Client of TCP Communication by JL
public class TCP_Client_X : MonoBehaviour
{
    private string staInfo = "NULL";        //State Info
    private string inputIP = "127.0.0.1";   //IP of the Server
    private string inputPort = "8080";      //Port of the Server
    public string inputMes = "NULL";        //Message to send
    private int recTimes = 0;               //Receive times
    private string recMes = "NULL";         //Received message
    private Socket Client_Socket;           //Client Socket, connect to the server
    public bool ConnectFlag = false;        //Connect Flag, to connect to the server
    public bool SendFlag = false;           //Send Flag, to send the message
    public bool DisAbleFlag = false;        //Disable Flag, to disable the Client

    //Start is called before the first frame update
    void Start()
    {
        
        
    }

    
    //Update is called once per frame
    void Update()
    {
        if(ConnectFlag) ConnetToServer();               //Connect to the server
        SendMsg();                                      //Send the message
        if (DisAbleFlag)DisconnectOfClient();           //shut down the Client
    }

    private void ConnetToServer()
    {
        ConnectFlag = false;                            //Reset the connect flag
        try
        {
            int _port = Convert.ToInt32(inputPort);      //Port of the Server
            string _ip = inputIP;                        //IP of the Server
            Client_Socket = new Socket(AddressFamily.InterNetwork, 
                SocketType.Stream, ProtocolType.Tcp);    //Create a new socket
            IPAddress ip = IPAddress.Parse(_ip);         //Parse the IP address
            IPEndPoint point = new IPEndPoint(ip, _port); //Create a new endpoint

            Client_Socket.Connect(point);                //Connect to the server
            staInfo = ip + ":" + _port + " Connected";   //Update the state info
            Debug.Log(staInfo);
        }
        catch (Exception) {
            staInfo = "Error with the IP or Port";
            Debug.Log("Error: " + staInfo);
        }
    }

    public void SendMsg()
    {
        try
        {
            /*while (true)
            {
            }*/
            if (SendFlag && Client_Socket.Connected)
            {
                byte[] data = Encoding.UTF8.GetBytes(inputMes); //Convert the message to bytes
                Client_Socket.Send(data);                       //Send the message
                SendFlag = false;                               //Reset the send flag
                Debug.Log("Send: " + inputMes);
            }

        }
        catch { }
    }
    
    //Disconnet the Server
    public void DisconnectOfClient()
    {
        DisAbleFlag = false; //Reset the disable flag
        Debug.Log("begin OnDisable()");

        if (Client_Socket.Connected)
        {
            try
            {
                Client_Socket.Shutdown(SocketShutdown.Both);
                Client_Socket.Close();                          
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }

        Debug.Log("end OnDisable()");

    }

    void OnGUI()
    {
        GUI.color = Color.black;

        GUI.Label(new Rect(65, 10, 60, 20), "StatInfo");

        GUI.Label(new Rect(135, 10, 80, 60), staInfo);

        GUI.Label(new Rect(65, 70, 50, 20), "Server_IP");
        inputIP = GUI.TextField(new Rect(125, 70, 100, 20), inputIP, 20);

        GUI.Label(new Rect(65, 110, 50, 20), "Server_Port");

        inputPort = GUI.TextField(new Rect(125, 110, 100, 20), inputPort, 20);

        GUI.Label(new Rect(65, 150, 80, 20), "Msg_Rec: ");

        GUI.Label(new Rect(155, 150, 80, 20), recMes);

        GUI.Label(new Rect(65, 190, 80, 20), "Msg_Send: ");

        inputMes = GUI.TextField(new Rect(155, 190, 100, 20), inputMes, 20);

        if (GUI.Button(new Rect(65, 230, 60, 20), "Connect"))
        {
            ConnectFlag = true;
        }

        if (GUI.Button(new Rect(65, 270, 60, 20), "Send"))
        {
            SendFlag = true;
        }
    }


}
