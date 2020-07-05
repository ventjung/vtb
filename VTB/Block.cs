using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace VTB
{
    public class Block
    {
        public long Index { get; set; } = 0;
        public DateTime Timestamp { get; set; }

        public Transaction Data { get; set; }
        public string Hash { get; set; }
        public string PreviousHash { get; set; } = string.Empty;
        public long Nonce { get; set; }

        public Block(Transaction data)
        {
            this.Data = data;
            this.Nonce = 0;
            this.Timestamp = DateTime.Now;
            this.Hash = CalculateHash();
        }

        public string CalculateHash()
        {
            SHA256 Hasher = SHA256.Create();

            // object to json
            string jsonObject = JsonConvert.SerializeObject(new
            {
                Index = this.Index,
                Timestamp = this.Timestamp,
                Data = this.Data,
                PreviousHash = this.PreviousHash,
                Nonce = this.Nonce
            });

            // json to byte[]
            byte[] byteObject = Encoding.UTF8.GetBytes(jsonObject);

            // compute hash
            byte[] byteHash = Hasher.ComputeHash(byteObject);

            // return hashed string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < byteHash.Length; i++)
            {
                builder.Append(byteHash[i].ToString("X2"));
            }
            return builder.ToString();
        }

        public void MineBlock(int difficulty) // difficulty diset sama si chain
        {
            var zeroesOnHashed = this.Hash.Substring(0, difficulty);
            var zeroesRequired = string.Empty;
            for (int i = 0; i < difficulty; i++)
            {
                zeroesRequired += "0";
            }

            while (zeroesOnHashed != zeroesRequired)
            {
                this.Nonce++;
                this.Hash = this.CalculateHash();
                zeroesOnHashed = this.Hash.Substring(0, difficulty);
            }
        }
    }
}
