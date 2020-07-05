using System;

namespace VTB
{
    class Program
    {
        static void Main(string[] args)
        {
            // Public Private Key dari si Node ini
            string privateKey = "123456789"; // sesuatu
            string publicKey = Helper.GetPublicKeyFromPrivateKeyEx(privateKey); // ini jadi wallet address kita

            // Init Blockchain (disederhanakan)
            BlockChain blockChain = new BlockChain();

            // operasi transaksi (masuk uang ke si wallet ini)
            Transaction t1 = new Transaction()
            {
                From = "", // (disederhanakan) diisi empty soalnya biar pass verifikasi yang reward mining
                To = publicKey,
                Amount = 50
            };
            blockChain.AddTransaction(t1);

            // operasi transaksi (keluar uang dari si wallet ini)
            Transaction t2 = new Transaction()
            {
                From = publicKey,
                To = "(orang lain)", // diisi address orang lain
                Amount = 10
            };
            t2.Sign(privateKey);
            blockChain.AddTransaction(t2);

            // operasi mining
            blockChain.MinePendingTransaction(publicKey); // masuk coin (50) - m1
            blockChain.MinePendingTransaction(publicKey); // keluar coin (10) - m2
            blockChain.MinePendingTransaction(publicKey); // masuk coin hasil mining (1) (m1) -m3
            blockChain.MinePendingTransaction(publicKey); // masuk coin hasil mining (1) (m2) -m4

            // console log balance dan validasi blockchain
            Console.WriteLine(blockChain.GetBalance(publicKey));
            Console.WriteLine(blockChain.GetBalance("(orang lain)"));
            Console.WriteLine(blockChain.IsChainValid());
        }
    }
}
