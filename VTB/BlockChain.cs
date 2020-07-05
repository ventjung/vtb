using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace VTB
{
    public class BlockChain
    {
        public List<Block> Chain = new List<Block>(); // ini adalah blockchainnya
        public List<Transaction> PendingTransaction = new List<Transaction>(); // transaction pending disini

        public int Difficulty { get; set; } = 0; // difficulty mulai 0, setiap sukses mining akan increment
        public int MiningReward = 5; // reward 5 coin setiap sukses mining

        // constructor dari blockchain, pas hidup dia bikin genesis block yang ga ada isi transaction datanya, di index 0
        public BlockChain()
        {
            Chain.Add(new Block(null)); // Genesis Block
        }

        // get balance dari address yang jadi from dan to nya transaction data dalam block
        public int GetBalance(string Address)
        {
            int Balance = 0;
            for (int i = 0; i < Chain.Count; i++)
            {
                if (Chain[i].Data != null)
                {
                    if (Chain[i].Data.From == Address)
                    {
                        Balance -= Chain[i].Data.Amount;
                    }
                    if (Chain[i].Data.To == Address)
                    {
                        Balance += Chain[i].Data.Amount;
                    }
                }
            }
            return Balance;
        }

        // ini untuk add transaction, dari interface blockchain, masuknya ke pending transaction
        public void AddTransaction(Transaction NewTransaction)
        {
            if(NewTransaction.From == string.Empty) // reward mining berarti
            {
                PendingTransaction.Add(NewTransaction);
            }
            else
            {
                if(NewTransaction.IsValid() == true)
                {
                    PendingTransaction.Add(NewTransaction);
                }
            }
        }

        // ini method miningnya
        public void MinePendingTransaction(string MinerAddress)
        {
            if (PendingTransaction.Count > 0)
            {
                Transaction poppedTransaction = PendingTransaction[0];
                Block transactionBlock = new Block(poppedTransaction); 
                AddBlock(transactionBlock);
                PendingTransaction.Remove(poppedTransaction);
                AddTransaction(new Transaction()
                {
                    Amount = this.MiningReward,
                    From = string.Empty,
                    To = MinerAddress
                });
            }
        }

        // ini untuk nambahin block pake pendekatan proof of work (mining)
        public void AddBlock(Block NewBlock)
        {
            NewBlock.PreviousHash = GetLatestBlock().Hash;
            NewBlock.Index = GetLatestBlock().Index + 1;
            NewBlock.MineBlock(this.Difficulty);
            Chain.Add(NewBlock);
            Difficulty++;
        }

        // ini untuk ngambil block terakhir
        public Block GetLatestBlock()
        {
            return Chain[Chain.Count - 1];
        }

        // ini routine untuk ngecek apakah si block chain ini valid
        public bool IsChainValid()
        {
            // pendekatannya dari last block, ngecek sampe genesis block, bener ga hash set nya
            var result = true;
            for (int i = Chain.Count - 1; i >= 1 ; i--)
            {
                var thisBlock = Chain[i];
                var prevBlock = Chain[i - 1];
                if (thisBlock.Hash != thisBlock.CalculateHash()) // untuk block ini
                {
                    result = false;
                    break;
                }
                else if(prevBlock.Hash != thisBlock.PreviousHash) // untuk block sebelumnya tapi dicek ke prevHash block ini
                {
                    result = false;
                    break;
                }
                else if(thisBlock.Data.IsValid() != true) // ini untuk ngecek validitas transaksinya
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
    }
}
