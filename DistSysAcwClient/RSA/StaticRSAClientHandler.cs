using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DistSysAcwClient.RSA
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;

    namespace DistSysAcwClient.RSA
    {
        public static class StaticRSAHandler
        {
            private static readonly RSACryptoServiceProvider _rsa;

            static StaticRSAHandler()
            {
                var cspParams = new CspParameters
                {
                    Flags = CspProviderFlags.UseMachineKeyStore
                };
                _rsa = new RSACryptoServiceProvider(cspParams);
            }

            public static string PublicKey()
            {
                return _rsa.ToXmlString(false);
            }

            public static byte[] Sign(string message)
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                return _rsa.SignData(data, CryptoConfig.MapNameToOID("SHA1"));
            }

            public static RSACryptoServiceProvider GetProvider()
            {
                return _rsa;
            }
        }
    }

    //public class StaticRSAClientHandler
    //{
    //    private static RSACryptoServiceProvider _clientRsaProvider;
    //    public StaticRSAClientHandler(string publicKey)
    //    {
    //        _clientRsaProvider = new RSACryptoServiceProvider();
    //        _clientRsaProvider.FromXmlString(publicKey);
    //    }
    //    // private static RSACryptoServiceProvider clientRsaProvider = new RSACryptoServiceProvider(PublicKey publicKey);

    //    public bool Verify(byte[] originalBytes, byte[] signedBytes)
    //    {
    //        return _clientRsaProvider.VerifyData(originalBytes, SHA1.Create(), signedBytes);
    //    }
    //}
}
