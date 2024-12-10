using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vaja_4_hibridno_sifriranje.Network
{
    using System.Net.Sockets;
    using System.Security.Cryptography;

    public static class DiffieHellmanHelper
    {
        public const int buffSize = 1024;
        public static byte[] PerformHandshakeServer(NetworkStream ns, out byte[] aesIv)
        {
            using (ECDiffieHellmanCng serverECDH = new ECDiffieHellmanCng())
            {
                serverECDH.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                serverECDH.HashAlgorithm = CngAlgorithm.Sha256;

                byte[] clientPublicKey = new byte[buffSize];
                int bytesRead = ns.Read(clientPublicKey, 0, buffSize);

                byte[] serverPublicKey = serverECDH.PublicKey.ToByteArray();
                ns.Write(serverPublicKey, 0, serverPublicKey.Length);

                using (CngKey clientKey = CngKey.Import(clientPublicKey, CngKeyBlobFormat.EccPublicBlob))
                {
                    byte[] sharedSecret = serverECDH.DeriveKeyMaterial(clientKey);

                    aesIv = new byte[16];
                    ns.Read(aesIv, 0, 16);

                    return sharedSecret;
                }
            }
        }

        public static byte[] PerformHandshakeClient(NetworkStream ns, out byte[] aesIv)
        {
            using (ECDiffieHellmanCng clientECDH = new ECDiffieHellmanCng())
            {
                clientECDH.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                clientECDH.HashAlgorithm = CngAlgorithm.Sha256;

                byte[] clientPublicKey = clientECDH.PublicKey.ToByteArray();
                ns.Write(clientPublicKey, 0, clientPublicKey.Length);

                byte[] serverPublicKey = new byte[buffSize];
                int bytesRead = ns.Read(serverPublicKey, 0, buffSize);

                using (CngKey serverKey = CngKey.Import(serverPublicKey, CngKeyBlobFormat.EccPublicBlob))
                {
                    byte[] sharedSecret = clientECDH.DeriveKeyMaterial(serverKey);

                    aesIv = new byte[16];
                    ns.Write(aesIv, 0, 16);

                    return sharedSecret;
                }
            }
        }
    }

}
