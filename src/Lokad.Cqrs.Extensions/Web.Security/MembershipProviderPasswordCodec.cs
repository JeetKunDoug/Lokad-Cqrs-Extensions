using System;

namespace Lokad.Cqrs.Extensions.Web.Security
{
    class MembershipProviderPasswordCodec : IPasswordCodec
    {
        private readonly Func<string, string> encrypt;
        private readonly Func<string, string> decrypt;

        public MembershipProviderPasswordCodec(Func<string, string> encrypt, Func<string, string> decrypt)
        {
            this.encrypt = encrypt;
            this.decrypt = decrypt;
        }

        #region Implementation of IPasswordCodec

        public string Encode(string password)
        {
            return encrypt(password);
        }

        public string Decode(string password)
        {
            return decrypt(password);
        }

        #endregion
    }
}