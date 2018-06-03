using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Windows.Forms;

namespace SysProgClient
{
    public partial class Lobby : Form
    {
        public Socket m_Socket;    //socket used to connect to server
        private IPEndPoint m_remoteEndPoint; //endpoint used while connecting
        public string name; //users name, only assigned when connected to server
        public System.Timers.Timer bTimer;
        int maxRooms = 10;

        //constructor
        public Lobby()
        {
            InitializeComponent();
        }

        //on form load
        private void Form2_Load(object sender, EventArgs e)
        {

        }

        //join a server as an active TCP
        private void btnJoin_Click(object sender, EventArgs e)
        {
            if (m_Socket != null && m_Socket.Connected && !txtName.Text.Equals(""))
            {
                sendData("n," + txtName.Text + ",");
            }
            else if (!(txtName.Text.Equals("")||txtIP.Equals("")))
            {
                try
                {
                    // Create the socket, for TCP use
                    m_Socket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);
                }

                catch (SocketException)
                {
                    // If an exception occurs, display an error message                	
                    Close_Socket_and_Exit();
                }

                // Get the IP address from the appropriate text box
                String ip = txtIP.Text;
                try
                {
                    System.Net.IPAddress DestinationIPAddress =
                                   System.Net.IPAddress.Parse(ip);
                    if (checkInvalidIP(ip)){
                        throw new Exception();
                    }
                // Get the Port number from the appropriate text box
                int iPort = 8009;
                // Combine Address and Port to create an Endpoint
                m_remoteEndPoint =
                       new System.Net.IPEndPoint(DestinationIPAddress, iPort);
                }
                catch
                {
                    MessageBox.Show("Please enter a valid IP address (E.G. \"127.0.0.1\").", "Error");
                    return;
                }
                try
                {

                    m_Socket.Connect(m_remoteEndPoint);
                }
                catch (SocketException)
                {
                    MessageBox.Show("Couldn't connect. Please ensure a server exists on the supplied IP.", "Error");
                }
                /*
                 * Enables ui elements once user is connected to server in order to
                 * allow a user to join a room
                 */
                if (m_Socket.Connected)
                {
                    txtIP.Enabled = false;
                    name = txtName.Text;
                    sendData("n," + name + ",");
                    m_Socket.Blocking = false;
                    bTimer = new System.Timers.Timer();
                    bTimer.Elapsed += new ElapsedEventHandler(oneS);
                    bTimer.Interval = 1000;
                    bTimer.Enabled = true;
                    bTimer.SynchronizingObject = this;
                }
            }
            else
            {
                MessageBox.Show("Enter a username and IP.", "Error");
            }
        }

        public bool checkInvalidIP(string ip)
        {
            string[] ipCheck = ip.Split('.');
            if (ipCheck.Length != 4)
            {
                return true;
            }
            return false;
        }

        //every one second receive messages from the server
        private void oneS(object sender, ElapsedEventArgs e)
        {
            bTimer.Enabled = false;
            try
            {
                byte[] ReceiveBuffer = new byte[1024];
                int iReceiveByteCount;
                iReceiveByteCount =
                m_Socket.Receive(ReceiveBuffer, SocketFlags.None);
                Console.Write("");
                if (0 < iReceiveByteCount) //receives multiple msgs into same buffer / string.
                                           //change method to handle this (delete / shift array based on the command
                {
                    String msg = Encoding.ASCII.GetString(ReceiveBuffer, 0,
                                        iReceiveByteCount);
                    List<String> msgs = msg.Split(',').ToList<String>();
                    for (int i = 0; i < msgs.Count; i++)
                    {
                        switch (msgs[i])
                        {
                            case "ro": //rooms  
                                string tempSelected="";
                                if (lbRooms.SelectedIndex != -1)
                                {
                                    tempSelected = lbRooms.SelectedItem.ToString();
                                }
                                lbRooms.Items.Clear();
                                for (int k = 1; k < maxRooms+1; k++)
                                {
                                    if (!msgs[i + k].Equals(""))
                                    {
                                        lbRooms.Items.Add(msgs[i + k]);
                                    }
                                }
                                lbRooms.SelectedItem = tempSelected;
                                i++;
                                i++;
                                break;
                            case "te": //test
                                Random rnd = new Random();
                                txtIP.AppendText("Receiving" + rnd.Next(10));
                                break;
                            case "mr": //max rooms
                                maxRooms = Int32.Parse(msgs[i+1]);
                                i++;
                                break;
                            case "nw": //namewrong
                                MessageBox.Show("Please choose another name.", "Error");
                                break;
                            case "k": //kill
                                Form2_FormClosing(null, null);
                                MessageBox.Show("Server closed, please restart and reconnect.", "Error");
                                Close();
                                break;
                            case "ki": //kill idle
                                Form2_FormClosing(null, null);
                                MessageBox.Show("Client idle for too long, please restart and reconnect.", "Error");
                                Close();
                                break;
                            case "rf": //room full
                                MessageBox.Show("Room couldn't be made.", "Error");
                                break;
                            case "sf": //server full
                                MessageBox.Show("Server is full. Please try again later or connect to a different server.", "Error");
                                txtIP.Enabled = true;
                                break;
                            case "nr": //nameright
                                btnJoinServer.Enabled = false;
                                lbRooms.Enabled = true;
                                txtRoomNum.Enabled = true;
                                btnJoinRoom.Enabled = true;
                                btnCreateRoom.Enabled = true;
                                txtName.Enabled = false;
                                break;
                        }
                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString());
            }
            bTimer.Enabled = true;
        }

        //close the socket
        private void Close_Socket_and_Exit()
        {
            try
            {
                m_Socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {

            }
        }

        //check for selected listbox item and connect to a room if you can
        private void btnJoinRoom_Click(object sender, EventArgs e)
        {
            if (lbRooms.SelectedItem == null)
            {
                MessageBox.Show("Please select/create a room to join.", "Error");
            }
            else
            {
                Game game = new Game(m_Socket, name, this);
                bTimer.Enabled = false;
                game.Show();
                Hide();
                sendData("j," + lbRooms.SelectedItem.ToString() + ",");
            }
        }

        //check room name value and try to create a new room if valid
        private void btnCreateRoom_Click(object sender, EventArgs e)
        {
            if (!lbRooms.Items.Contains(txtRoomNum.Text))
            {
                {
                    if (!txtRoomNum.Text.Equals(""))
                    {
                            sendData("cr," + txtRoomNum.Text + ",");
                            txtRoomNum.Text = "";

                    }
                    else
                    {
                        MessageBox.Show("Please enter a room name.", "Error");
                    }
                }
            }
            else
            {
                MessageBox.Show("Room already exists.", "Error");
            }
        }

        //send data to the server
        private void sendData(String data)
        {
            try
            {
                String szData = data;
                int iBytesSent;
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(szData);
                iBytesSent = m_Socket.Send(byData, SocketFlags.None);
            }
            catch
            {

            }
        }

        //on closing the lobby, tell the server you are leaving and then close the socket
        public void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            sendData("ls,");
            Close_Socket_and_Exit();
        }
    }
}
