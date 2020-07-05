using System;
using System.Collections.Generic;
using System.Text;

namespace VTB
{
    public class Transaction
    {
        public string From { get; set; }
        public string To { get; set; }
        public int Amount { get; set; }
        public string Signature { get; set; }

        public void Sign(string PrivateKey)
        {
            this.Signature = Helper.GetSignature(PrivateKey, this.From + this.To + this.Amount.ToString());
        }

        public bool IsValid()
        {
            if (this.From == string.Empty)
                return true;
            else
                return Helper.VerifySignature(this.From + this.To + this.Amount.ToString(), this.From, this.Signature);
        }
    }
}
