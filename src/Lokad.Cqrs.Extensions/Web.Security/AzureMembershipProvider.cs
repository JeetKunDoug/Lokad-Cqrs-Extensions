using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Text;
using System.Web.Configuration;
using System.Web.Security;
using System.Linq;

namespace Lokad.Cqrs.Extensions.Web.Security
{
    /// <summary>
    /// Custom ASP.NET membership provider that uses the Azure Table Store to store membership data in a cloud.
    /// The code is based on a MSDN code example that used ODBC, see http://bit.ly/hiLtaw the ODBC sample.
    /// Sample was converted by Inge Eivind Henriksen; inge [AT] meronymy [DOT] com 
    /// Use at your own risk, the code was not made for a production environment but for shareing.
    /// Sample has same license as the MSDN sample has.
    /// </summary>
    public class AzureMembershipProvider : MembershipProvider
    {
        MachineKeySection machineKey;

        // Minimun password length
        int minRequiredPasswordLength = 6;

        // Minium non-alphanumeric char required
        int minRequiredNonAlphanumericCharacters;
        
        // Enable - disable password retrieval
        bool enablePasswordRetrieval;
        
        // Enable - disable password reseting
        bool enablePasswordReset;
        
        /// Require security question and answer (this, for instance, is a functionality which not many people use)
        bool requiresQuestionAndAnswer;
        
        /// Application name
        string applicationName;
        
        // Max number of failed password attempts before the account is blocked, and time to reset that counter
        int maxInvalidPasswordAttempts;
        int passwordAttemptWindow;
        
        // Require email to be unique 
        bool requiresUniqueEmail;
        
        // Regular expression the password should match (empty for none)
        string passwordStrengthRegularExpression;

        MembershipPasswordFormat passwordFormat;
        private MembershipProviderDataStore dataStore;
        private MembershipProviderPasswordCodec passwordCodec;

        /// <summary>
        /// A helper function to retrieve config values from the configuration file
        /// </summary>
        /// <param name="configValue"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        static string GetConfigValue(string configValue, string defaultValue)
        {
            return String.IsNullOrEmpty(configValue) ? defaultValue : configValue;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            // Initialize values from web.config.
            if (config == null) throw new ArgumentNullException("config");

            if (String.IsNullOrEmpty(name)) name = "CustomMembershipProvider";

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Custom Membership provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);

            #region Set membership settings
            applicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            maxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            passwordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            minRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "1"));
            minRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
            passwordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], ""));
            enablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            enablePasswordRetrieval = Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "true"));
            requiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
            requiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
            #endregion

            #region Determine password format settings
            var tempFormat = config["passwordFormat"] ?? "Hashed";
            switch (tempFormat)
            {
                case "Hashed":
                    passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
                case "Encrypted":
                    passwordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Clear":
                    passwordFormat = MembershipPasswordFormat.Clear;
                    break;
                default:
                    throw new ProviderException("Password format not supported.");
            }
            #endregion

            // Get encryption and decryption key information from the configuration.
            var cfg = WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            machineKey = (MachineKeySection)cfg.GetSection("system.web/machineKey");

            if (machineKey.ValidationKey.Contains("AutoGenerate"))
                if (PasswordFormat != MembershipPasswordFormat.Clear)
                    throw new ProviderException("Hashed or Encrypted passwords are not supported with auto-generated keys.");
            
            passwordCodec = new MembershipProviderPasswordCodec(
                pwd => Convert.ToBase64String(EncryptPassword(Encoding.Unicode.GetBytes(pwd))),
                pwd => Encoding.Unicode.GetString(DecryptPassword(Convert.FromBase64String(pwd))));

            dataStore = new MembershipProviderDataStore(applicationName, passwordFormat, passwordCodec);
        }

        public override string ApplicationName
        {
            get { return applicationName; }
            set { applicationName = value; }
        }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            if (!ValidateUser(username, oldPassword)) return false;

            var args = new ValidatePasswordEventArgs(username, newPassword, true);

            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Change password canceled due to new password validation failure.");

            return dataStore.ChangePassword(username, oldPassword, newPassword);
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            if (!ValidateUser(username, password)) return false;

            return dataStore.ChangePasswordQuestionAndAnswer(username, password, newPasswordQuestion, newPasswordAnswer);
        }



        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            #region Validate password
            
            var args = new ValidatePasswordEventArgs(username, password, true);

            OnValidatingPassword(args);
            
            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (RequiresUniqueEmail && GetUserNameByEmail(email) != String.Empty)
            {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }
            
            #endregion

            if (GetUser(username, false) != null)
            {
                status = MembershipCreateStatus.DuplicateUserName;
                return null;
            }

            dataStore.CreateUser(username, password, email, passwordQuestion, passwordAnswer, isApproved, providerUserKey);

            // Assert that the user has been added to the store
            var newUser = GetUser(username, false);
            status = newUser == null ? MembershipCreateStatus.UserRejected : MembershipCreateStatus.Success;

            return newUser;
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            return dataStore.DeleteUser(username, deleteAllRelatedData);
        }

        public override bool EnablePasswordReset
        {
            get { return enablePasswordReset; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return enablePasswordRetrieval; }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            var users = new MembershipUserCollection();

            var userEntities = dataStore.FindUsersByEmail(emailToMatch).ToArray();

            totalRecords = userEntities.Length;

            if (totalRecords == 0) return users;

            var counter = 0;
            var startIndex = pageSize * pageIndex;
            var endIndex = startIndex + pageSize - 1;

            foreach (var userEntity in userEntities)
            {
                if (counter >= startIndex) users.Add(GetUserFromEntity(userEntity));

                if (counter >= endIndex) break;

                counter++;
            }

            return users;
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            var users = new MembershipUserCollection();

            var userEntities = dataStore.FindUsersByEmail(usernameToMatch).ToArray();
            totalRecords = userEntities.Length;

            if (totalRecords == 0) return users;

            var counter = 0;
            var startIndex = pageSize * pageIndex;
            var endIndex = startIndex + pageSize - 1;

            foreach (var userEntity in userEntities)
            {
                if (counter >= startIndex) users.Add(GetUserFromEntity(userEntity));

                if (counter >= endIndex) break;

                counter++;
            }

            return users;
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            var users = new MembershipUserCollection();

            var userEntities = dataStore.GetAllUsers().ToArray();
            totalRecords = userEntities.Length;

            if (totalRecords == 0) return users;

            var counter = 0;
            var startIndex = pageSize * pageIndex;
            var endIndex = startIndex + pageSize - 1;

            foreach (var userEntity in userEntities)
            {
                if (counter >= startIndex) users.Add(GetUserFromEntity(userEntity));

                if (counter >= endIndex) break;

                counter++;
            }

            return users;
        }

        public override int GetNumberOfUsersOnline()
        {
            var onlineSpan = new TimeSpan(0, Membership.UserIsOnlineTimeWindow, 0);
            var compareTime = DateTime.Now.Subtract(onlineSpan);

            return dataStore.GetOnlineUsers(compareTime).Count();
        }

        /// <summary>
        /// Compares password values based on the MembershipPasswordFormat
        /// </summary>
        /// <param name="password">Cleartext password</param>
        /// <param name="dbpassword">Encoded or hashed password</param>
        /// <returns>True if same values</returns>
        bool CheckPassword(string password, string dbpassword)
        {
            var pass1 = password;
            var pass2 = dbpassword;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Encrypted:
                    pass2 = passwordCodec.Decode(dbpassword);
                    break;
                case MembershipPasswordFormat.Hashed:
                    pass1 = dataStore.EncodePassword(password);
                    break;
            }

            return pass1 == pass2;
        }

        //TODO: Get this the hell out of here!
        void SendAccountLockedEmail(Guid providerKey, string userName, string emailAddress)
        {
            Console.Out.WriteLine("");
//            // Now lets create an email message
//            var emailMessage = new StringBuilder();
//            var header = Resources.Email.EmailHeader.Replace("{0}", "Your MyApp account has been locked out!");
//            emailMessage.Append(header);
//            emailMessage.Append(string.Format("Hello, {0}.<br />Someone, possibly you, have {1} bad sign in attempts to your MyApp account. Because of this your account was automatically locked out to prevent a possible account password hacking attempt. ", userName, MaxInvalidPasswordAttempts));
//            emailMessage.Append("You can't sign in to MyApp until your account is unlocked. To unlock your MyApp account again please click the link below:<br /><br />");
//            var url = string.Format("http://www.MyApp.com/Account/UnlockAccount.aspx?Id={0}", providerKey);
//            emailMessage.Append(string.Format("<a href='{0}'>{0}</a><br /><br />", url));
//            emailMessage.Append("If your e-mail client does not allow links then copy & paste the above url into your browser address bar.");
//            var footer = String.Format(Resources.Email.EmailFooter, "This e-mail was sent your user account at <a href=\"http://www.MyApp.com\">www.MyApp.com</a> was locked out because of too many bad sign attempt.");
//            emailMessage.Append(footer);
//            var fromEmail = ConfigurationManager.AppSettings["fromEmail"];
//
//            // Send the email
//            var queueDataSource = new AzureQueueDataSource();
//            queueDataSource.SendEmail(fromEmail, emailAddress, "Unlock your MyApp account", emailMessage.ToString());
        }

        /// <summary>
        /// A helper method that performs the checks and updates associated with password failure tracking
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="failureType">Failure type; "password" or "passwordAnswer" accepted</param>
        void UpdateFailureCount(string username, string failureType)
        {
            var userEntity = dataStore.GetUser(username);

            if (userEntity == null || userEntity.IsDeleted) return;

            var windowStart = new DateTime();
            var failureCount = 0;

            switch (failureType)
            {
                case "password":
                    failureCount = userEntity.FailedPasswordAttemptCount;
                    windowStart = userEntity.FailedPasswordAttemptWindowStart;
                    break;
                case "passwordAnswer":
                    failureCount = userEntity.FailedPasswordAnswerAttemptCount;
                    windowStart = userEntity.FailedPasswordAnswerAttemptWindowStart;
                    break;
            }

            var windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

            if (failureCount == 0 || DateTime.Now > windowEnd)
            {
                // First password failure or outside of PasswordAttemptWindow. 
                // Start a new password failure count from 1 and a new window starting now.
                switch (failureType)
                {
                    case "password":
                        userEntity.FailedPasswordAttemptCount = 1;
                        userEntity.FailedPasswordAttemptWindowStart = DateTime.Now;
                        break;
                    case "passwordAnswer":
                        userEntity.FailedPasswordAnswerAttemptCount = 1;
                        userEntity.FailedPasswordAnswerAttemptWindowStart = DateTime.Now;
                        break;
                }
            }
            else
            {
                if (failureCount++ >= MaxInvalidPasswordAttempts)
                {
                    // Password attempts have exceeded the failure threshold. Lock out the user.
                    userEntity.IsLockedOut = true;
                    userEntity.LastLockedOutDate = DateTime.Now;

                    // Notify that the user has been locked out by e-mail
                    SendAccountLockedEmail(userEntity.Identity, userEntity.Username, userEntity.Email);
                }
                else
                {
                    // Password attempts have not exceeded the failure threshold. Update
                    // the failure counts. Leave the window the same.
                    switch (failureType)
                    {
                        case "password":
                            userEntity.FailedPasswordAttemptCount = failureCount;
                            break;
                        case "passwordAnswer":
                            userEntity.FailedPasswordAnswerAttemptCount = failureCount;
                            break;
                    }
                }
            }

            dataStore.Update(userEntity);
        }

        public override string GetPassword(string username, string answer)
        {
            if (!EnablePasswordRetrieval) throw new ProviderException("Password Retrieval Not Enabled.");

            if (PasswordFormat == MembershipPasswordFormat.Hashed) throw new ProviderException("Cannot retrieve Hashed passwords.");

            var userEntity = dataStore.GetUser(username);
            if (userEntity == null) throw new MembershipPasswordException("The supplied user name is not found.");
            
            if (userEntity.IsLockedOut)
                throw new MembershipPasswordException("The supplied user is locked out.");

            if (userEntity.IsDeleted)
                throw new MembershipPasswordException("The supplied user does not exist.");

            if (RequiresQuestionAndAnswer && !CheckPassword(answer, userEntity.PasswordAnswer))
            {
                UpdateFailureCount(username, "passwordAnswer");

                throw new MembershipPasswordException("Incorrect password answer.");
            }

            string password;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Encrypted:
                    password = passwordCodec.Decode(userEntity.Password);
                    break;
                case MembershipPasswordFormat.Clear:
                    password = userEntity.Password;
                    break;
                default:
                    throw new MembershipPasswordException("Only encrypted or plaintext passwords can be retrieved.");
            }

            userEntity.FailedPasswordAnswerAttemptCount = 0;
            userEntity.LastActivityDate = DateTime.Now;
            
            dataStore.Update(userEntity);

            return password;
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            return GetUser(dataStore.GetRowKey(username), userIsOnline);
        }

        MembershipUser GetUserFromEntity(UserEntity userEntity)
        {
            return new MembershipUser(
                    Name,
                    userEntity.Username,
                    userEntity.Identity,
                    userEntity.Email,
                    userEntity.PasswordQuestion,
                    userEntity.Comment,
                    userEntity.IsApproved,
                    userEntity.IsLockedOut,
                    userEntity.CreationDate,
                    userEntity.LastLoginDate,
                    userEntity.LastActivityDate,
                    userEntity.LastPasswordChangedDate,
                    userEntity.LastLockedOutDate
                );
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {

            UserEntity userEntity;

            try
            {
                userEntity = dataStore.GetUser((Guid)providerUserKey);
            }
            catch
            {
                // If the table is empty a DataServiceQueryException is thrown
                userEntity = null;
            }

            return userEntity == null ? null : GetUserFromEntity(userEntity);
        }

        public override string GetUserNameByEmail(string email)
        {
            var userEntity = dataStore.GetUserByEmail(email);

            return userEntity == null ? null : userEntity.Username;
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return maxInvalidPasswordAttempts; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return minRequiredNonAlphanumericCharacters; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return minRequiredPasswordLength; }
        }

        public override int PasswordAttemptWindow
        {
            get { return passwordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return passwordFormat; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return passwordStrengthRegularExpression; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return requiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return requiresUniqueEmail; }
        }

        public override string ResetPassword(string username, string answer)
        {
            if (!EnablePasswordReset)
                throw new NotSupportedException("Password reset is not enabled.");

            if (answer == null && RequiresQuestionAndAnswer)
            {
                UpdateFailureCount(username, "passwordAnswer");

                throw new ProviderException("Password answer required for password reset.");
            }

            const int NEW_PASSWORD_LENGTH = 8;
            var newPassword = Membership.GeneratePassword(NEW_PASSWORD_LENGTH, MinRequiredNonAlphanumericCharacters);

            var args = new ValidatePasswordEventArgs(username, newPassword, true);

            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Reset password canceled due to password validation failure.");
            
            var userEntity = dataStore.GetUser(username);
            if (userEntity == null || userEntity.IsDeleted)
                throw new MembershipPasswordException("The supplied user name is not found.");

            if (userEntity.IsLockedOut)
                throw new MembershipPasswordException("The supplied user is locked out.");

            var passwordAnswer = userEntity.PasswordAnswer;

            if (RequiresQuestionAndAnswer && !answer.Trim().Equals(passwordAnswer.Trim(), StringComparison.InvariantCultureIgnoreCase))
            {
                UpdateFailureCount(username, "passwordAnswer");

                throw new MembershipPasswordException("Incorrect password answer.");
            }

            userEntity.Password = passwordCodec.Encode(newPassword);
            userEntity.LastPasswordChangedDate = userEntity.LastActivityDate = DateTime.Now;
            dataStore.Update(userEntity);

            return newPassword;
        }

        public override bool UnlockUser(string username)
        {
            var userEntity = dataStore.GetUser(username);
            if (userEntity == null || userEntity.IsDeleted) return false;

            userEntity.IsLockedOut = false;
            userEntity.LastActivityDate = DateTime.Now;
            dataStore.Update(userEntity);

            return true;
        }

        public override void UpdateUser(MembershipUser user)
        {
            var userEntity = dataStore.GetUser(user.UserName);

            if (userEntity == null || userEntity.IsDeleted)
                throw new MembershipPasswordException("The supplied user name is not found.");

            if (userEntity.IsLockedOut)
                throw new MembershipPasswordException("The supplied user is locked out.");

            userEntity.Email = user.Email;
            userEntity.Comment = user.Comment;
            userEntity.IsApproved = user.IsApproved;
            dataStore.Update(userEntity);
        }

        public override bool ValidateUser(string username, string password)
        {
            var userEntity = dataStore.GetUser(username);

            if (userEntity == null || userEntity.IsDeleted || userEntity.IsLockedOut) return false;

            var isValid = false;
            var isApproved = userEntity.IsApproved;
            var pwd = userEntity.Password;

            if (CheckPassword(password, pwd))
            {
                userEntity.LastActivityDate = DateTime.Now;

                if (isApproved)
                {
                    isValid = true;

                    userEntity.LastLoginDate = DateTime.Now;
                    userEntity.FailedPasswordAttemptCount = 0;
                }

                dataStore.Update(userEntity);
            }
            else UpdateFailureCount(username, "password");

            return isValid;
        }
    }
}