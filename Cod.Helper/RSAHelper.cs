using System.Security.Cryptography;
using System.Text;

namespace Cod
{
    /// <summary>
    /// openssl genrsa -out private.pem 2048 
    /// openssl rsa -in private.pem -outform PEM -pubout -out public.pem
    /// </summary>
    public class RSAHelper : IDisposable
    {
        private RSA privateKeyRsaProvider;
        private RSA publicKeyRsaProvider;
        private readonly HashAlgorithmName hashAlgorithmName;
        private readonly Encoding encoding;

        public RSAHelper(RSAType rsaType, bool isOpenSSL, string privateKey = null, string publicKey = null, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            this.encoding = encoding;
            if (!string.IsNullOrEmpty(privateKey))
            {
                if (isOpenSSL)
                {
                    privateKeyRsaProvider = CreateRsaProviderFromPrivateKey(privateKey);
                }
                else
                {
                    privateKeyRsaProvider = RSA.Create();
                    privateKeyRsaProvider.FromXmlString(privateKey, null);
                }
            }
            else if (!string.IsNullOrEmpty(publicKey))
            {
                if (isOpenSSL)
                {
                    publicKeyRsaProvider = CreateRsaProviderFromPublicKey(publicKey);
                }
                else
                {
                    publicKeyRsaProvider = RSA.Create();
                    publicKeyRsaProvider.FromXmlString(publicKey, null);
                }
            }

            hashAlgorithmName = rsaType == RSAType.RSA ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
        }

        public void Dispose()
        {
            if (privateKeyRsaProvider != null)
            {
                privateKeyRsaProvider.Dispose();
                privateKeyRsaProvider = null;
            }
            if (publicKeyRsaProvider != null)
            {
                publicKeyRsaProvider.Dispose();
                publicKeyRsaProvider = null;
            }
        }

        public string Sign(string data)
        {
            byte[] dataBytes = encoding.GetBytes(data);
            byte[] signatureBytes = privateKeyRsaProvider.SignData(dataBytes, hashAlgorithmName, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signatureBytes);
        }

        public bool Verify(string data, string signature)
        {
            byte[] dataBytes = encoding.GetBytes(data);
            byte[] signBytes = Convert.FromBase64String(signature);
            return publicKeyRsaProvider.VerifyData(dataBytes, signBytes, hashAlgorithmName, RSASignaturePadding.Pkcs1);
        }

        public string Decrypt(string cipherText)
        {
            return privateKeyRsaProvider == null
                ? throw new Exception("Private key is required to decrypt data.")
                : Encoding.UTF8.GetString(privateKeyRsaProvider.Decrypt(Convert.FromBase64String(cipherText), RSAEncryptionPadding.Pkcs1));
        }

        public string Encrypt(string text)
        {
            return publicKeyRsaProvider == null
                ? throw new Exception("Public key is required to decrypt data")
                : Convert.ToBase64String(publicKeyRsaProvider.Encrypt(Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.Pkcs1));
        }

        private RSA CreateRsaProviderFromPrivateKey(string privateKey)
        {
            byte[] privateKeyBits = Convert.FromBase64String(privateKey);

            RSA rsa = RSA.Create();
            RSAParameters rsaParameters = new();

            using (BinaryReader binr = new(new MemoryStream(privateKeyBits)))
            {
                byte bt = 0;
                ushort twobytes = 0;
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)
                {
                    binr.ReadByte();
                }
                else if (twobytes == 0x8230)
                {
                    binr.ReadInt16();
                }
                else
                {
                    throw new Exception("Unexpected value read binr.ReadUInt16()");
                }

                twobytes = binr.ReadUInt16();
                if (twobytes != 0x0102)
                {
                    throw new Exception("Unexpected version");
                }

                bt = binr.ReadByte();
                if (bt != 0x00)
                {
                    throw new Exception("Unexpected value read binr.ReadByte()");
                }

                rsaParameters.Modulus = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.Exponent = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.D = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.P = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.Q = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.DP = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.DQ = binr.ReadBytes(GetIntegerSize(binr));
                rsaParameters.InverseQ = binr.ReadBytes(GetIntegerSize(binr));
            }

            rsa.ImportParameters(rsaParameters);
            return rsa;
        }

        private RSA CreateRsaProviderFromPublicKey(string publicKeyString)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };

            byte[] x509Key = Convert.FromBase64String(publicKeyString);

            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
            using MemoryStream mem = new(x509Key);
            using BinaryReader binr = new(mem);  //wrap Memory Stream with BinaryReader for easy reading
            byte bt = 0;
            ushort twobytes = 0;

            twobytes = binr.ReadUInt16();
            if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
            {
                binr.ReadByte();    //advance 1 byte
            }
            else if (twobytes == 0x8230)
            {
                binr.ReadInt16();   //advance 2 bytes
            }
            else
            {
                return null;
            }

            byte[] seq = binr.ReadBytes(15);
            if (!CompareBytearrays(seq, seqOid))    //make sure Sequence for OID is correct
            {
                return null;
            }

            twobytes = binr.ReadUInt16();
            if (twobytes == 0x8103) //data read as little endian order (actual data order for Bit String is 03 81)
            {
                binr.ReadByte();    //advance 1 byte
            }
            else if (twobytes == 0x8203)
            {
                binr.ReadInt16();   //advance 2 bytes
            }
            else
            {
                return null;
            }

            bt = binr.ReadByte();
            if (bt != 0x00)     //expect null byte next
            {
                return null;
            }

            twobytes = binr.ReadUInt16();
            if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
            {
                binr.ReadByte();    //advance 1 byte
            }
            else if (twobytes == 0x8230)
            {
                binr.ReadInt16();   //advance 2 bytes
            }
            else
            {
                return null;
            }

            twobytes = binr.ReadUInt16();
            byte lowbyte = 0x00;
            byte highbyte = 0x00;

            if (twobytes == 0x8102) //data read as little endian order (actual data order for Integer is 02 81)
            {
                lowbyte = binr.ReadByte();  // read next bytes which is bytes in modulus
            }
            else if (twobytes == 0x8202)
            {
                highbyte = binr.ReadByte(); //advance 2 bytes
                lowbyte = binr.ReadByte();
            }
            else
            {
                return null;
            }

            byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
            int modsize = BitConverter.ToInt32(modint, 0);

            int firstbyte = binr.PeekChar();
            if (firstbyte == 0x00)
            {   //if first byte (highest order) of modulus is zero, don't include it
                binr.ReadByte();    //skip this null byte
                modsize -= 1;   //reduce modulus buffer size by 1
            }

            byte[] modulus = binr.ReadBytes(modsize);   //read the modulus bytes

            if (binr.ReadByte() != 0x02)            //expect an Integer for the exponent data
            {
                return null;
            }

            int expbytes = binr.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
            byte[] exponent = binr.ReadBytes(expbytes);

            // ------- create RSACryptoServiceProvider instance and initialize with public key -----
            RSA rsa = RSA.Create();
            RSAParameters rsaKeyInfo = new()
            {
                Modulus = modulus,
                Exponent = exponent
            };
            rsa.ImportParameters(rsaKeyInfo);

            return rsa;
        }

        private int GetIntegerSize(BinaryReader binr)
        {
            byte bt = binr.ReadByte();
            if (bt != 0x02)
            {
                return 0;
            }

            bt = binr.ReadByte();

            int count;
            if (bt == 0x81)
            {
                count = binr.ReadByte();
            }
            else
            if (bt == 0x82)
            {
                byte highbyte = binr.ReadByte();
                byte lowbyte = binr.ReadByte();
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                count = BitConverter.ToInt32(modint, 0);
            }
            else
            {
                count = bt;
            }

            while (binr.ReadByte() == 0x00)
            {
                count -= 1;
            }
            binr.BaseStream.Seek(-1, SeekOrigin.Current);
            return count;
        }

        private bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                {
                    return false;
                }

                i++;
            }
            return true;
        }
    }

    public enum RSAType
    {
        RSA,

        RSA2
    }
}