using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Share
{
    /// <summary>
    /// 账号数据
    /// </summary>
    public class UserAccount
    {
        [BsonId]
        public string Account;
        public string Password;
        public string Platform;
        public DateTime CreateTime;
    }
}
