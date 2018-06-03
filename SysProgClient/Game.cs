using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Windows.Forms;

namespace SysProgClient
{
    public partial class Game : Form
    {
        bool drawing = false;
        bool drawer = true;
        Bitmap drawingSurface = new Bitmap(500, 500);
        SolidBrush brushColour = new SolidBrush(Color.Black);
        String word = "";
        Socket m_Socket;
        int x;
        int y;
        int size;
        private System.Timers.Timer aTimer;
        string name;
        Lobby lobby;

        //constructor takes socket from form2 when user connects to server
        //sets the name of the player aswell, from form 2
        public Game(Socket m_Socket, String name, Lobby lobby)
        {
            this.m_Socket = m_Socket;
            this.name = name;
            this.lobby = lobby;
            InitializeComponent();
        }

        //on form load, sets default values
        private void Form1_Load(object sender, EventArgs e)
        {
            btnColour.BackColor = brushColour.Color;
            guesserSet();
            size = tbSize.Value + 2;
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(oneMS);
            aTimer.Interval = 1;
            aTimer.Enabled = true;
            aTimer.SynchronizingObject = this;
        }

        //starting drawing
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            drawing = true;
            if (drawer)
            {
                x = e.X;
                y = e.Y;
                drawGraphics(x, y);

                sendData("d," + x.ToString() + "," + y.ToString());
            }
        }

        //stopping drawing
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            drawing = false;
        }

        //brush size changed
        private void tbSize_Scroll(object sender, EventArgs e)
        {
            size = tbSize.Value + 2;
            sendData("s," + size + ",");
        }

        //colour size changing using dialog
        private void btnColour_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                btnColour.BackColor = cd.Color;
                brushColour.Color = cd.Color;
                sendData("b," + brushColour.Color.ToArgb() + ",");
            }
        }

        //drawing while mouse down
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {

            if (drawing && drawer)
            {
                x = e.X;
                y = e.Y;
                drawGraphics(x, y);
                sendData("d," + x.ToString() + "," + y.ToString() + ",");
            }
        }

        //draw on the picture box with appropriate colour/size
        private void drawGraphics(int x, int y)
        {
            Graphics GFX = Graphics.FromImage(drawingSurface);
            GFX.FillEllipse(brushColour, x - (size / 2), y - (size / 2), size, size);
            pictureBox1.Image = drawingSurface;
            GFX.Dispose();
        }

        //clear the image / picture box
        private void btnClear_Click(object sender, EventArgs e)
        {
            clearImg();
            sendData("c,");
        }

        //set client as drawer, showing certain elements
        private void drawerSet()
        {
            drawer = true;
            lblWord.Text = "";
            lblSize.Show();
            tbSize.Show();
            lblColour.Show();
            btnColour.Show();
            btnClear.Show();
            btnSkip.Show();
        }

        //set client as guesser, hiding certain elements
        private void guesserSet()
        {
            drawer = false;
            lblWord.Text = "";
            lblSize.Hide();
            tbSize.Hide();
            lblColour.Hide();
            btnColour.Hide();
            btnClear.Hide();
            btnSkip.Hide();
        }

        //set the work to _ of the given word's length
        private void wordSet(int length)
        {
            for (int i = 0; i < length; i++)
            {
                lblWord.Text += "_ ";
            }
        }

        //on pressing enter in the chat input box, send to server
        private void txtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            String input = txtChatInput.Text;
            input = input.ToLower();
            if (e.KeyCode == Keys.Enter && !input.Equals(word) && !String.IsNullOrWhiteSpace(input))
            {
                txtChatInput.Clear();
                sendData("ch," + input + "," + name + ",");
            }
        }

        //every 1 ms try to receive a message from the server
        private void oneMS(object sender, ElapsedEventArgs e)
        {
            aTimer.Enabled = false;
            try
            {
                byte[] ReceiveBuffer = new byte[1024];
                int iReceiveByteCount;
                iReceiveByteCount =
                m_Socket.Receive(ReceiveBuffer, SocketFlags.None);

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
                            case "b": //brush
                                brushColour.Color = Color.FromArgb(Int32.Parse(msgs[i + 1]));
                                if (!drawer)
                                    btnColour.BackColor = brushColour.Color;
                                i++;
                                break;
                            case "c": //clear
                                clearImg();
                                break;
                            case "s": //size
                                size = Int32.Parse(msgs[i + 1]);
                                if (!drawer)
                                    tbSize.Value = size - 2;
                                i++;
                                break;
                            case "d": //draw
                                drawGraphics(Int32.Parse(msgs[i + 1]), Int32.Parse(msgs[i + 2]));
                                i++;
                                i++;
                                break;
                            case "ch": //chat
                                txtChat.AppendText(msgs[i + 2] + ": " + msgs[i + 1] + "\r\n");
                                i++;
                                i++;
                                break;
                            case "w": //word
                                if (drawer)
                                {
                                    lblWord.Text = msgs[i + 1];
                                }
                                else
                                {
                                    lblWord.Text = "";
                                    wordSet(msgs[i + 1].Length);
                                }
                                i++;
                                break;
                            case "wr": //winner
                                MessageBox.Show(msgs[i + 1] + " wins with " + msgs[i + 2] + " points!", "Winner");
                                List<string> names = new List<string>();
                                foreach (string s in lbPlayers.Items)
                                {
                                    names.Add(s.Split('|')[0] + "| 0");
                                }
                                lbPlayers.Items.Clear();
                                foreach (string s in names)
                                {
                                    lbPlayers.Items.Add(s);
                                }
                                i++;
                                i++;
                                break;
                            case "np": //newplayer
                                lbPlayers.Items.Add("" + msgs[i + 1] + " | " + msgs[i + 2]);
                                i++;
                                i++;
                                break;
                            case "t": //time
                                lblTime.Text = msgs[i + 1];
                                i++;
                                break;
                            case "te": //test
                                txtChat.AppendText("Receiving\r\n");
                                break;
                            case "dr": //drawer
                                drawerSet();
                                drawer = true;
                                break;
                            case "dn": //drawing now
                                txtChat.AppendText("*****" + msgs[i + 1] + " is drawing*****\r\n");
                                i++;
                                break;
                            case "gr": //gusser
                                drawer = false;
                                guesserSet();
                                break;
                            case "co": //correct
                                foreach (string s in lbPlayers.Items)
                                {
                                    if (s.Split('|')[0].Equals(msgs[i + 2] + " "))
                                    {
                                        int index = lbPlayers.Items.IndexOf(s);
                                        lbPlayers.Items.RemoveAt(index);
                                        lbPlayers.Items.Insert(index, msgs[i + 2] + " | " + (Int32.Parse(s.Split('|')[1]) + Int32.Parse(msgs[i + 1])));
                                    }
                                }
                                i++;
                                i++;
                                break;
                            case "rd": //room dead
                                Form1_FormClosing(null, null);
                                Close();
                                MessageBox.Show("Room had no players for too long.", "Error");
                                break;
                            case "ns": //nospace
                                lblSpec.Visible = true;
                                txtChatInput.Enabled = false;
                                break;
                            case "rp": //removeplayer
                                foreach (string s in lbPlayers.Items)
                                {
                                    if (s.Split('|')[0].Equals(msgs[i + 1]))
                                    {
                                        lbPlayers.Items.Remove(s);
                                    }
                                }
                                i++;
                                break;
                            case "r": //round
                                if (Int32.Parse(msgs[i + 1]) == 0)
                                {
                                    lbRound.Text = "Set-Up";
                                    txtChat.AppendText("*****Setting Up Game*****\r\n");
                                }
                                else
                                {
                                    lbRound.Text = "Round: " + msgs[i + 1];
                                    txtChat.AppendText("*****Round " + msgs[i + 1] + " of 3*****\r\n");
                                }
                                btnColour.BackColor = brushColour.Color;
                                tbSize.Value = size - 2;
                                i++;
                                break;
                            case "k": //kill
                                Form1_FormClosing(null, null);
                                Close();
                                lobby.Form2_FormClosing(null, null);
                                MessageBox.Show("Server closed, please restart and reconnect.", "Error");
                                lobby.Close();
                                break;
                            case "ki": //kill idle
                                Form1_FormClosing(null, null);
                                Close();
                                lobby.Form2_FormClosing(null, null);
                                MessageBox.Show("Client idle for too long, please restart and reconnect.", "Error");
                                lobby.Close();
                                break;
                            default:
                                //txtChat.AppendText("\r\ndefault case reached");
                                break;
                        }
                    }
                }
            }
            catch
            {
            }
            try
            {
                aTimer.Enabled = true;
            }
            catch
            {

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

        //on closing of the game form, send a message to leave the room to the server
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            sendData("lr,");//send a message to leave the room
            aTimer.Dispose();
            lobby.bTimer.Enabled = true;
            lobby.Show();
        }

        //clear the picture box / image
        private void clearImg()
        {
            Graphics GFX = Graphics.FromImage(drawingSurface);
            GFX.FillRectangle(Brushes.White, 0, 0, 500, 500);
            pictureBox1.Image = drawingSurface;
        }

        private void btnSkip_Click(object sender, EventArgs e)
        {
            sendData("sk,");
        }
    }
}
