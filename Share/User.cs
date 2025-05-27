using MessagePack;

namespace Share
{
    [MessagePackObject(true)]
    public class User
    {
        /// <summary>
        /// 名字
        /// </summary>
        public string Name;
        /// <summary>
        /// ID
        /// </summary>
        public int ID;
        /// <summary>
        /// 等级
        /// </summary>
        public int Level;
        /// <summary>
        /// 头像
        /// </summary>
        public string Avatar;
    }
}
