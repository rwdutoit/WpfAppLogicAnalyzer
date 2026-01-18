using ScottPlot;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfAppLogicAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Connect to a Remote server  
        // Get Host IP Address that is used to establish a connection  
        // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
        // If a host has multiple addresses, you will get a list of addresses  
        IPAddress ipAddress;
        IPEndPoint remoteEP;
        Socket sender;
        public MainWindow()
        {
            InitializeComponent();
            // Connect to a Remote server  
            // Get Host IP Address that is used to establish a connection  
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
            // If a host has multiple addresses, you will get a list of addresses  
            ipAddress = IPAddress.Parse(ipAddressText.Text); // host.AddressList[0];
            remoteEP = new IPEndPoint(ipAddress, 8888);
        }

        public void StartClient()
        {
            try
            {
                // Create a TCP/IP  socket.    
                sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    // Connect to Remote EndPoint  
                    sender.Connect(remoteEP);

                    //mtextshow.Text = "Socket connected to " + sender.RemoteEndPoint.ToString();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void stopClient()
        {
            //// Release the socket.    
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }

        public void startCapturing()
        {
            try
            {


                byte[] bytes = new byte[1024];

                // Encode the data string into a byte array.    
                byte[] msg = Encoding.ASCII.GetBytes("s"); //Start capturing

                // Send the data through the socket.    
                int bytesSent = sender.Send(msg);
                short sampleSize = Convert.ToInt16(sampleAmount.Text); // 100;
                int bytesSent2 = sender.Send(BitConverter.GetBytes(sampleSize));

                // Receive the response from the remote device.    
                int bytesRec = sender.Receive(bytes);
                //var results = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                //mtextshow.Show = "Start capturing results = " + results;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.ToString());
            }

        }

        public byte[] startReporting()
        {

            int size = 1024 * 1024;
            byte[] bytesBuffer = new byte[size];

            // Encode the data string into a byte array.    
            byte[] msg = Encoding.ASCII.GetBytes("r"); //Start capturing

            // Send the data through the socket.    
            int bytesSent = sender.Send(msg);

            // Receive the response from the remote device.    
            int bytesRec = 0;
            int offset = 0;
            do
            {

                bytesRec = sender.Receive(bytesBuffer, offset, size - offset, SocketFlags.None);
                offset += bytesRec;
            } while (bytesBuffer[offset - 1] != 'e'); //(sender.Available > 0);
                                                      //var results = Encoding.ASCII.GetString(bytesBuffer, 0, offset);
                                                      //mtextshow.Show = "Capture report results = " + results;

            return bytesBuffer;

        }

        private void ButtonFetch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WpfPlot1.Plot.Clear();
                StartClient();
                startCapturing();
                var bytesBuffer = startReporting();
                var myData = process(bytesBuffer);
                stopClient();


                var scatter0 = WpfPlot1.Plot.Add.Scatter(myData.Time, myData.Values[0]);
                scatter0.ConnectStyle = ScottPlot.ConnectStyle.StepHorizontal;
                var scatter1 = WpfPlot1.Plot.Add.Scatter(myData.Time, myData.Values[1]);
                scatter1.ConnectStyle = ScottPlot.ConnectStyle.StepHorizontal;
                var scatter2 = WpfPlot1.Plot.Add.Scatter(myData.Time, myData.Values[2]);
                scatter2.ConnectStyle = ScottPlot.ConnectStyle.StepHorizontal;
                var scatter3 = WpfPlot1.Plot.Add.Scatter(myData.Time, myData.Values[3]);
                scatter3.ConnectStyle = ScottPlot.ConnectStyle.StepHorizontal;

                WpfPlot1.Plot.Axes.AutoScale();
                WpfPlot1.Refresh();

                // Display data as text
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("Time\tCh0\tCh1\tCh2\tCh3");
                for (int i = 0; i < myData.Time.Length; i++)
                {
                    sb.AppendLine($"{myData.Time[i]}\t{myData.Values[0][i] - 0}\t{myData.Values[1][i] - 1}\t{myData.Values[2][i] - 2}\t{myData.Values[3][i] - 3}");
                }
                dataText.Text = sb.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.ToString());
            }
        }

        // reverse byte order (64-bit)
        public static UInt64 ReverseBytes(UInt64 value)
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                   (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                   (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                   (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }

        private MyData process(byte[] byteBuffer)
        {
            var results = Encoding.ASCII.GetString(byteBuffer, 0, byteBuffer.Length);
            var myData = new MyData();
            int offset = 0;
            if (byteBuffer.Length != 0 && byteBuffer[offset] == 'S')
            {
                offset++;
                uint samples = BitConverter.ToUInt32(byteBuffer, offset) - 1;
                offset += 4;

                myData.Values[0] = new double[samples];
                myData.Values[1] = new double[samples];
                myData.Values[2] = new double[samples];
                myData.Values[3] = new double[samples];
                myData.Time = new double[samples];
                offset += 9;
                for (int index = 0; index < samples; index++)
                {
                    ulong time = BitConverter.ToUInt64(byteBuffer, offset);
                    myData.Time[index] = Convert.ToDouble(time);
                    offset += 8;
                    var values = byteBuffer[offset];
                    myData.Values[0][index] = 0 + Convert.ToDouble((values & 1) >> 0);
                    myData.Values[1][index] = 1 + Convert.ToDouble((values & 2) >> 1);
                    myData.Values[2][index] = 2 + Convert.ToDouble((values & 4) >> 2);
                    myData.Values[3][index] = 3 + Convert.ToDouble((values & 8) >> 3);
                    offset++;
                }
            }
            return myData;
        }
    }

    class MyData
    {
        private double[] time;
        private double[][] values = new double[4][];

        public double[] Time { get => time; set => time = value; }
        public double[][] Values { get => values; set => values = value; }
    }
}

