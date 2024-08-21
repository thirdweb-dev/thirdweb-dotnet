using System.Runtime.Serialization;

namespace Thirdweb.EWS;

internal partial class LocalStorage : LocalStorageBase
{
    [DataContract]
    internal class DataStorage
    {
        internal string AuthToken => this._authToken;
        internal string DeviceShare => this._deviceShare;
        internal string EmailAddress => this._emailAddress;
        internal string PhoneNumber => this._phoneNumber;
        internal string WalletUserId => this._walletUserId;
        internal string AuthProvider => this._authProvider;

        [DataMember]
        private string _authToken;

        [DataMember]
        private string _deviceShare;

        [DataMember]
        private string _emailAddress;

        [DataMember]
        private string _phoneNumber;

        [DataMember]
        private string _walletUserId;

        [DataMember]
        private string _authProvider;

        internal DataStorage(string authToken, string deviceShare, string emailAddress, string phoneNumber, string walletUserId, string authProvider)
        {
            this._authToken = authToken;
            this._deviceShare = deviceShare;
            this._emailAddress = emailAddress;
            this._phoneNumber = phoneNumber;
            this._walletUserId = walletUserId;
            this._authProvider = authProvider;
        }

        internal void ClearAuthToken()
        {
            this._authToken = null;
        }
    }

    [DataContract]
    private class Storage
    {
        [DataMember]
        internal DataStorage Data { get; set; }
    }
}
