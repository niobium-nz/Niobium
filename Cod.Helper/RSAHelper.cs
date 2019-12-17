using System;
using System.IO;
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
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            this.encoding = encoding;
            if (!String.IsNullOrEmpty(privateKey))
            {
                if (isOpenSSL)
                {
                    this.privateKeyRsaProvider = this.CreateRsaProviderFromPrivateKey(privateKey);
                }
                else
                {
                    this.privateKeyRsaProvider = RSA.Create();
                    this.privateKeyRsaProvider.FromXmlString(privateKey, null);
                }
            }
            else if (!String.IsNullOrEmpty(publicKey))
            {
                if (isOpenSSL)
                {
                    this.publicKeyRsaProvider = this.CreateRsaProviderFromPublicKey(publicKey);
                }
                else
                {
                    this.publicKeyRsaProvider = RSA.Create();
                    this.publicKeyRsaProvider.FromXmlString(publicKey, null);
                }
            }

            this.hashAlgorithmName = rsaType == RSAType.RSA ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
        }

        public void Dispose()
        {
            if (this.privateKeyRsaProvider != null)
            {
                this.privateKeyRsaProvider.Dispose();
                this.privateKeyRsaProvider = null;
            }
            if (this.publicKeyRsaProvider != null)
            {
                this.publicKeyRsaProvider.Dispose();
                this.publicKeyRsaProvider = null;
            }
        }

        public string Sign(string data)
        {
            var dataBytes = this.encoding.GetBytes(data);
            var signatureBytes = this.privateKeyRsaProvider.SignData(dataBytes, this.hashAlgorithmName, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signatureBytes);
        }

        public bool Verify(string data, string signature)
        {
            var dataBytes = this.encoding.GetBytes(data);
            var signBytes = Convert.FromBase64String(signature);
            return this.publicKeyRsaProvider.VerifyData(dataBytes, signBytes, this.hashAlgorithmName, RSASignaturePadding.Pkcs1);
        }

        public string Decrypt(string cipherText)
        {
            if (this.privateKeyRsaProvider == null)
            {
                throw new Exception("Private key is required to decrypt data.");
            }
            return Encoding.UTF8.GetString(this.privateKeyRsaProvider.Decrypt(Convert.FromBase64String(cipherText), RSAEncryptionPadding.Pkcs1));
        }

        public string Encrypt(string text)
        {
            if (this.publicKeyRsaProvider == null)
            {
                throw new Exception("Public key is required to decrypt data");
            }
            return Convert.ToBase64String(this.publicKeyRsaProvider.Encrypt(Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.Pkcs1));
        }

        private RSA CreateRsaProviderFromPrivateKey(string privateKey)
        {
            var privateKeyBits = Convert.FromBase64String(privateKey);

            var rsa = RSA.Create();
            var rsaParameters = new RSAParameters();

            using (var binr = new BinaryReader(new MemoryStream(privateKeyBits)))
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

                rsaParameters.Modulus = binr.ReadBytes(this.GetIntegerSize(binr));
                rsaParameters.Exponent = binr.ReadBytes(this.GetIntegerSize(binr));
                rsaParameters.D = binr.ReadBytes(this.GetIntegerSize(binr));
                rsaParameters.P = binr.ReadBytes(this.GetIntegerSize(binr));
                rsaParameters.Q = binr.ReadBytes(this.GetIntegerSize(binr));
                rsaParameters.DP = binr.ReadBytes(this.GetIntegerSize(binr));
                rsaParameters.DQ = binr.ReadBytes(this.GetIntegerSize(binr));
                rsaParameters.InverseQ = binr.ReadBytes(this.GetIntegerSize(binr));
            }

            rsa.ImportParameters(rsaParameters);
            return rsa;
        }

        private RSA CreateRsaProviderFromPublicKey(string publicKeyString)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };

            var x509Key = Convert.FromBase64String(publicKeyString);

            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
            using (var mem = new MemoryStream(x509Key))
            {
                using (var binr = new BinaryReader(mem))  //wrap Memory Stream with BinaryReader for easy reading
                {
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

                    var seq = binr.ReadBytes(15);
                    if (!this.CompareBytearrays(seq, seqOid))    //make sure Sequence for OID is correct
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
                    var modsize = BitConverter.ToInt32(modint, 0);

                    var firstbyte = binr.PeekChar();
                    if (firstbyte == 0x00)
                    {   //if first byte (highest order) of modulus is zero, don't include it
                        binr.ReadByte();    //skip this null byte
                        modsize -= 1;   //reduce modulus buffer size by 1
                    }

                    var modulus = binr.ReadBytes(modsize);   //read the modulus bytes

                    if (binr.ReadByte() != 0x02)            //expect an Integer for the exponent data
                    {
                        return null;
                    }

                    var expbytes = (int)binr.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
                    var exponent = binr.ReadBytes(expbytes);

                    // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                    var rsa = RSA.Create();
                    var rsaKeyInfo = new RSAParameters
                    {
                        Modulus = modulus,
                        Exponent = exponent
                    };
                    rsa.ImportParameters(rsaKeyInfo);

                    return rsa;
                }

            }
        }

        private int GetIntegerSize(BinaryReader binr)
        {
            var bt = binr.ReadByte();
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
                var highbyte = binr.ReadByte();
                var lowbyte = binr.ReadByte();
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

            var i = 0;
            foreach (var c in a)
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