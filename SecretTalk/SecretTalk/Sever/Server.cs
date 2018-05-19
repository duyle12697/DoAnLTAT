using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Security.Cryptography;
//using System.Web.Script.Serialization;
using System.Web;
using System.Collections.Generic;

namespace Sever
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Connect();
        }
        string strkey = RandomKey();
        IPEndPoint ipe;
        Socket server;
        List<Socket> clientlist;
        // Tạo kết nối
        void Connect()
        {
            clientlist = new List<Socket>();
            ipe = new IPEndPoint(IPAddress.Any, 9999);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            server.Bind(ipe);
            Thread Listen = new Thread(() => {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        clientlist.Add(client);
                        
                        
                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    ipe = new IPEndPoint(IPAddress.Any, 9999);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }

            });
            Listen.IsBackground = true;
            Listen.Start();

        }
        // Đóng kết nối
        void Close()
        {
            server.Close();
        }
        void Send(Socket client, string text, int TLNopadding)
        {
            string chuoitong = text + ";" + TLNopadding + "," + Hash(txtMessage.Text);
            //string chuoimahoa = Encrypt(chuoitong,txtKey.Text);
            //byte[] dlguidi = Encoding.ASCII.GetBytes(txtKey.Text + chuoimahoa);
            //byte[] chuoimh = Encoding.ASCII.GetBytes(chuoimahoa);
            if (client != null && txtMessage.Text != string.Empty)
              client.Send(Encoding.ASCII.GetBytes(txtKey.Text + chuoitong));
        }

        void SendNoise(Socket client, string text, int TLNopadding)
        {
            string chuoitong = text + strBuilder.ToString() + ";" + TLNopadding + "," + Hash(txtMessage.Text);
            if (client != null && txtMessage.Text != string.Empty)
                client.Send(Encoding.ASCII.GetBytes(txtKey.Text + chuoitong));
        }
        void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5];
                    client.Receive(data);
                    //Ép kiểu object thành string
                    string message = Encoding.ASCII.GetString(data);
                    string key = message.Substring(0,31);
                    txtKey.Text = key;
                    message = message.Substring(32);
                    foreach (Socket item in clientlist)
                    {
                        if (item != null && item != client)
                            item.Send(Encoding.ASCII.GetBytes(txtMessage.Text));
                    }
                    AddMessage(message);
                }
            }
            catch
            {
                clientlist.Remove(client);
                client.Close();
            }
        }
        // Add tin nhắn vào khung chat
        void AddMessage(string s)
        {
            lsvMessage.Items.Add(new ListViewItem() { Text = s });
        } 
        //Dừng kết nối khi đóng form
        private void Sever_FormClosed(object sender, FormClosedEventArgs e)
        {
            Close();
        }

        //Gửi tin nhắn vào tất cả client
        private void btnSend_Click(object sender, EventArgs e)
        {
            //AddMessage(Encrypt("abc", txtKey.Text));
            //AddMessage(Decrypt(Encrypt("abc", txtKey.Text), txtKey.Text));
            if (txtMessage.Text != string.Empty)
            {
                string tmp = PaddingText(txtMessage.Text);
                foreach (Socket item in clientlist)
                {
                    Send(item, tmp, txtMessage.Text.Length);
                }
                AddMessage(PaddingText(txtMessage.Text));
                txtMessage.Clear();
            }

        }
        public byte[] EncryptData(string data)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] hashedBytes;
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            hashedBytes = md5Hasher.ComputeHash(encoder.GetBytes(data));
            return hashedBytes;
        }
        public string maHoaMd5(string data)
        {
            return BitConverter.ToString(EncryptData(data)).Replace("-", "").ToLower();
        }
        //time stmp
        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        //Padiing text (không dùng kiểu chuẩn)
        public string PaddingText(string text)
        {
            int blockmissing = 16 - (16 % text.Length);
            string Ptext;
            if (blockmissing != 0)
            {
                String timeStamp = maHoaMd5(GetTimestamp(DateTime.Now));
                Ptext = timeStamp.Substring(0, blockmissing);
                return String.Concat(text, Ptext);
            }
            else return text;
        }
        // Button Send Noise
        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        public string Hash(string text)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                return GetMd5Hash(md5Hash, text);
            }
        }
        public static char GetChar()
        {
            string chars = "$%#@!*abcdefghijklmnopqrstuvwxyz1234567890?;:ABCDEFGHIJKLMNOPQRSTUVWXYZ^&";
            Random rand = new Random();
            int num = rand.Next(0, chars.Length - 1);
            return chars[num];
        }
        public static string RandomKey()
        {
            string rdkey = null;
            int num;
            Random rand = new Random();
            string chars = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (int i = 1; i <=32; i++)
            {
                num = rand.Next(0, chars.Length - 1);
                rdkey = rdkey + chars[num];
                num = 0;
            }
            return rdkey;
        }

        System.Text.StringBuilder strBuilder;
        private void Server_Load(object sender, EventArgs e)
        {
            txtKey.Text = strkey;
            timer1.Start();
        }

        private void btnSN_Click(object sender, EventArgs e)
        {
            if (txtMessage.Text != string.Empty)
            {
                int n = txtMessage.Text.ToString().Length;
                Random rand = new Random();
                int num = rand.Next(0, n);
                strBuilder = new System.Text.StringBuilder(txtMessage.Text);
                strBuilder[num] = GetChar();
                string tmp = PaddingText(strBuilder.ToString());

                foreach (Socket item in clientlist)
                {
                    SendNoise(item, tmp, txtMessage.Text.Length);
                }
                lsvMessage.Items.Add(new ListViewItem() { Text = strBuilder.ToString() + ";" + txtMessage.Text.Length + "," + Hash(txtMessage.Text)});
                txtMessage.Clear();
            }

        }

        // Trao đổi key trước khi truyền (RSA)

        int duration = 30;

        private void timer1_Tick(object sender, EventArgs e)
        {
            duration--;
            txtTIme.Text = duration.ToString();
            timer1.Interval = 1000;
            if (duration == 0)
            {
                timer1.Stop();
                MessageBox.Show("Session Time Out!");
                txtKey.Text = RandomKey();
                duration = 15;
                timer1.Start();
            }
            timer1.Start();
        }

        private const int Keysize = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DerivationIterations = 1000;

        public static string Encrypt(string plainText, string passPhrase)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            var saltStringBytes = Generate256BitsOfRandomEntropy();
            var ivStringBytes = Generate256BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                var cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }
        //
        public static string Decrypt(string cipherText, string passPhrase)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                var plainTextBytes = new byte[cipherTextBytes.Length];
                                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }

        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }
        private void btnKey_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
