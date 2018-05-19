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

namespace Chat
{
    public partial class Client : Form
    {
        IPEndPoint ipe;
        Socket client;
        public Client()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Connect();

        }
        string strkey;
        //Tạo kết nối
        void Connect()
        {
            ipe = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try
            {
                client.Connect(ipe);
            }
            catch
            {
                MessageBox.Show("Không thể kết nối!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            Thread listen = new Thread(Receive);
            listen.IsBackground = true;
            listen.Start();

        }
        //Đóng kết nối
        void Close()
        {
            client.Close();
        }

        void Send(string text)
        {
            if (txtMessage.Text != string.Empty)
                client.Send(Encoding.ASCII.GetBytes(txtKey.Text + text));
        }

        void Receive()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);
                    // Ép kiểu thành string
                    string message = Encoding.ASCII.GetString(data); 
                    string catchkey = message.Substring(0, 31);
                    txtKey.Text = catchkey;
                    message = message.Substring(32);
                    //byte[] keybyte = Encoding.ASCII.GetBytes(txtKey.Text);
                    //string decrypted = DecryptStringFromBytes_Aes(Encoding.ASCII.GetBytes(message), keybyte);
                    //Split(decrypted);
                    Split(message);
                    CheckMD5(Str);
                    AddMessage(Str);

                }
            }
            catch {
                Close();
            }
        }
        
        string str1;
        string str2;
        string Str;
        string strmd5;


        void Split(string text)
        {
            // message = str1;str2
            string[] arrListStr1 = (text).Split(new char[] { ';' });         
            str1 = arrListStr1[0].ToString().Trim(); // textpadding text
            str2 = arrListStr1[1].ToString().Trim();//padding length+ md5
            string[] arrListStr2 = (str2).Split(new char[] { ',' });
            int padlength = int.Parse(arrListStr2[0].ToString().Trim());//padding length
            strmd5  =  arrListStr2[1].ToString().Trim();// md5

            // str1 = text+padding
            Str = str1.Substring(0, padlength).Trim();

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
        //Padding dữ liệu bằng hàm timestamp
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
        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        //Phát hiện thay đổi gói tin trên đường truyền
        void CheckMD5(string text)
        {
            if (String.Compare(Hash(text), strmd5, false) != 0)
            {
                DialogResult dlr = MessageBox.Show("Đã thay đổi tin nhắn. Bạn muốn dừng ứng dụng?",
                    "Thông báo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dlr == DialogResult.Yes) Application.Exit();
            }
        }

        // Thêm tin nhắn vào khung chat
        void AddMessage(string s)
        {
            lsvMessage.Items.Add(new ListViewItem() { Text = s });
            txtMessage.Clear();
        } 

       
        private void Form1_Load(object sender, EventArgs e)
        {
            txtKey.Text = strkey;
            timer1.Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Đóng kết nối khi đóng form
            Close();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            Send(txtMessage.Text);
            AddMessage(PaddingText(txtMessage.Text));
        }

        //Send noise
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

        // diff

        private void btnKey_Click(object sender, EventArgs e)
        {
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

        //Mã Hóa tin nhắn đi với AES
        private const int Keysize = 256;
        private const int DerivationIterations = 1000;
        public static string Encrypt(string plainText, string passPhrase)
        {
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
        // Giải mã tin nhắn nhận được với AES
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
    }
}
