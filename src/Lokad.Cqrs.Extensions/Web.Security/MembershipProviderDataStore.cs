#region Copyright (c) 2011, EventDay Inc.
// Copyright (c) 2011, EventDay Inc.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the EventDay Inc. nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL EventDay Inc. BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Provider;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;

using Lokad.Cloud.Storage;
using Lokad.Cqrs.Extensions.Storage;

using Microsoft.Practices.ServiceLocation;

using System.Linq;

namespace Lokad.Cqrs.Extensions.Web.Security
{
    class MembershipProviderDataStore
    {
        private readonly string partitionKey;
        private readonly MembershipPasswordFormat passwordFormat;
        private readonly IPasswordCodec codec;
        private readonly CloudTable<UserEntity> table;

        public MembershipProviderDataStore(string partitionKey, MembershipPasswordFormat passwordFormat, IPasswordCodec codec)
        {
            this.partitionKey = partitionKey;
            this.passwordFormat = passwordFormat;
            this.codec = codec;
            var storageProvider = ServiceLocator.Current.GetInstance<ITableStorageProvider>();
            table = new CloudTable<UserEntity>(storageProvider, "users");
        }

        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            var rowKey = GetRowKey(username).ToString();
            var entity = table.Get(partitionKey, rowKey).ValueOrDefault();
            if (entity == null) return false;

            var user = entity.Value;
            user.Password = EncodePassword(newPassword);
            user.LastPasswordChangedDate = user.LastActivityDate = DateTime.Now;

            table.Update(entity);

            return true;
        }

        public bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            var rowKey = GetRowKey(username).ToString();
            var entity = table.Get(partitionKey, rowKey).ValueOrDefault();
            if (entity == null) return false;

            var user = entity.Value;
            user.PasswordQuestion = newPasswordQuestion;
            user.PasswordAnswer = newPasswordAnswer;
            user.LastActivityDate = DateTime.Now;

            table.Update(entity);

            return true;
        }

        public void CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey)
        {
            var identity = GetRowKey(username);

            var user = new UserEntity
            {
                Identity = identity,
                Username = username,
                Password = EncodePassword(password),
                CreationDate = DateTime.Now,
                IsDeleted = false,
                IsLockedOut = false,
                IsApproved = true,
                ApplicationName = partitionKey,
                Email = email,
                PasswordQuestion = passwordQuestion,
                PasswordAnswer = passwordAnswer,
                Gender = -1,
                LastProfileUpdatedDate = DateTime.Now,
                LastActivityDate = DateTime.Now,
                LastPasswordChangedDate = DateTime.Now
            };

            table.Insert(new CloudEntity<UserEntity>
            {
                PartitionKey = partitionKey,
                RowKey = identity.ToString(),
                Value = user
            });
        }

        public bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            var rowKey = GetRowKey(username).ToString();
            var entity = table.Get(partitionKey, rowKey).ValueOrDefault();
            if (entity == null || entity.Value.IsDeleted)
                return false;

            if(deleteAllRelatedData)
            {
                table.Delete(partitionKey, rowKey);
            }
            else
            {
                var user = entity.Value;
                // Only mark the account as deleted, don't physically delete it
                user.IsDeleted = true;
                user.LastActivityDate = DateTime.Now;
                
                table.Update(entity);   
            }

            return true;
        }

        public IEnumerable<UserEntity> FindUsersByEmail(string emailToMatch)
        {
            try
            {
                return GetAllUsers().Where(u => u.Email == emailToMatch);
            }
            catch
            {
                return Enumerable.Empty<UserEntity>();
            }
        }

        public IEnumerable<UserEntity> GetAllUsers()
        {
            try
            {
                return table.Get(partitionKey).Select(e => e.Value);
            }
            catch
            {
                return Enumerable.Empty<UserEntity>();
            }
        }

        public IEnumerable<UserEntity> FindUsersByName(string usernameToMatch)
        {
            try
            {
                return GetAllUsers().Where(u => u.Username == usernameToMatch);
            }
            catch
            {
                return Enumerable.Empty<UserEntity>();
            }
        }

        public IEnumerable<UserEntity> GetOnlineUsers(DateTime compareTime)
        {
            try
            {
                return GetAllUsers().Where(u => u.LastActivityDate > compareTime);
            }
            catch
            {
                return Enumerable.Empty<UserEntity>();
            }
        }

        public UserEntity GetUserByEmail(string email)
        {
            return FindUsersByEmail(email).FirstOrDefault();
        }

        public UserEntity GetUser(string username)
        {
            return FindUsersByName(username).FirstOrDefault();
        }

        public UserEntity GetUser(Guid identity)
        {
            var rowKey = identity.ToString();

            var entity = table.Get(partitionKey, rowKey).ValueOrDefault();
            if (entity == null)
                return null;
            return entity.Value;
        }

        public void Update(UserEntity entity)
        {
            var cloudEntity = new CloudEntity<UserEntity>
            {
                PartitionKey = partitionKey,
                RowKey = GetRowKey(entity.Username).ToString(),
                Value = entity
            };

            table.Update(cloudEntity);
        }

        /// <summary>
        /// The provider key is a Guid made from the username MD5 hash 
        /// </summary>
        /// <param name="username">User name</param>
        /// <returns>Provider key</returns>
        public Guid GetRowKey(string username)
        {
            var unicodeEncoding = new UnicodeEncoding();
            var message = unicodeEncoding.GetBytes(username);

            MD5 hashString = new MD5CryptoServiceProvider();

            return new Guid(hashString.ComputeHash(message));
        }

        /// <summary>
        /// Encrypts, Hashes, or leaves the password clear based on the PasswordFormat
        /// TODO: wqeqewqe
        /// </summary>
        /// <param name="password">Password</param>
        /// <returns>Encoded password</returns>
        internal string EncodePassword(string password)
        {
            var encodedPassword = password;

            switch (passwordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    encodedPassword = codec.Encode(password);
                    break;
                case MembershipPasswordFormat.Hashed:
                    /* I don't use machineKey.ValidationKey as a hash key since it changes for each port which makes it useless in an development environment
                    , but you probably should in a production environment.*/
                    var hash = new HMACSHA1 { Key = HexToByte(ConfigurationManager.AppSettings["StaticKey"]) };
                    encodedPassword = Convert.ToBase64String(hash.ComputeHash(Encoding.Unicode.GetBytes(password)));
                    break;
                default:
                    throw new ProviderException("Unsupported password format.");
            }

            return encodedPassword;
        }

        /// <summary>
        /// Converts a hexadecimal string to a byte array. Used to convert encryption key values from the configuration
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        static byte[] HexToByte(string hexString)
        {
            var returnBytes = new byte[hexString.Length / 2];

            for (var i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);

            return returnBytes;
        }

    }
}