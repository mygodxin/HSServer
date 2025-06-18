using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public class User
    {
        [BsonId]
        public int ID;
        public int SID;
        public string Name;
        public string Avatar;
    }
}
